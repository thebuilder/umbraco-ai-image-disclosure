using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.Media;

internal sealed class MediaAiRescanService(
    IEntityService entityService,
    ICoreScopeProvider scopeProvider,
    IMediaService mediaService,
    ILogger<MediaAiRescanService> logger,
    IOptions<MediaAiRescanOptions> options)
{
    internal const int MaximumBatchSize = 50;

    internal RescanResult Scan(RescanCursor? cursor, int limit, int userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (cursor is not null && (cursor.LastId < 0 || cursor.MaximumId < cursor.LastId))
            throw new ArgumentOutOfRangeException(nameof(cursor));
        cursor ??= new RescanCursor(0, entityService.GetPagedDescendants(
            -1, UmbracoObjectTypes.Media, 0, 1, out _, null, Ordering.By("Id", Direction.Descending))
            .FirstOrDefault()?.Id ?? 0);
        var batchLimit = Math.Clamp(options.Value.MaximumBatchSize, 1, MaximumBatchSize);
        limit = Math.Clamp(limit, 1, batchLimit);
        var query = scopeProvider.CreateQuery<IUmbracoEntity>()
            .Where(entity => entity.Id > cursor.LastId && entity.Id <= cursor.MaximumId);
        var entities = entityService.GetPagedDescendants(
            -1, UmbracoObjectTypes.Media, 0, limit, out _, query, Ordering.By("Id")).ToArray();
        var scanned = 0;
        var skippedManual = 0;
        var failed = 0;
        foreach (var entity in entities)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var media = mediaService.GetById(entity.Id);
                if (media is null || media.Trashed
                    || !media.ContentType.Alias.Equals(Constants.DefaultImageMediaTypeAlias, StringComparison.OrdinalIgnoreCase)) continue;
                if (MediaAiMetadataProcessor.IsManualSource(media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias)))
                {
                    skippedManual++;
                    continue;
                }
                // Run detection once inside MediaSavingNotification, using the same action
                // as the editor. Applying results before Save would dirty editorial fields.
                media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.ResumeAutomaticDisclosureSourceValue);
                var save = mediaService.Save(media, userId);
                if (save.Success) scanned++;
                else
                {
                    failed++;
                    logger.LogWarning("AI disclosure scan could not save media {MediaKey}", media.Key);
                }
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException or StackOverflowException or AccessViolationException))
            {
                failed++;
                logger.LogWarning(exception, "AI disclosure scan failed for media {MediaId}", entity.Id);
            }
        }
        var nextCursor = cursor with { LastId = entities.LastOrDefault()?.Id ?? cursor.LastId };
        return new RescanResult(nextCursor, scanned, skippedManual, failed,
            entities.Length < limit || nextCursor.LastId >= cursor.MaximumId);
    }
}

internal sealed record RescanResult(RescanCursor NextCursor, int Scanned, int SkippedManual, int Failed, bool Done);
