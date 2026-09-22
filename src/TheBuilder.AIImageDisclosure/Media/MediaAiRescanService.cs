using Microsoft.Extensions.Logging;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.Media;

internal sealed class MediaAiRescanService(
    IEntityService entityService,
    IMediaService mediaService,
    ILogger<MediaAiRescanService> logger,
    IImageWatermarkVerifier? watermarkVerifier = null)
{
    internal const int MaximumBatchSize = 50;

    internal RescanResult Scan(int pageIndex, int limit, int userId, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        var batchLimit = Math.Clamp(watermarkVerifier?.MaximumRescanBatchSize ?? MaximumBatchSize, 1, MaximumBatchSize);
        limit = Math.Clamp(limit, 1, batchLimit);
        var entities = entityService.GetPagedDescendants(
            -1, UmbracoObjectTypes.Media, pageIndex, limit, out var total, null, Ordering.By("Id")).ToArray();
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
        return new RescanResult(pageIndex + 1, scanned, skippedManual, failed, total,
            entities.Length == 0 || ((long)pageIndex + 1) * limit >= total);
    }
}

internal sealed record RescanResult(int NextPageIndex, int Scanned, int SkippedManual, int Failed, long TotalItems, bool Done);
