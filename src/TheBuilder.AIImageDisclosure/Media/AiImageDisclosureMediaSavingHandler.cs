using Microsoft.Extensions.Logging;
using TheBuilder.AIImageDisclosure.Detection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.AIImageDisclosure.Media;

internal sealed class AiImageDisclosureMediaSavingHandler(
    IMediaAiMetadataProcessor processor,
    ILogger<AiImageDisclosureMediaSavingHandler> logger) : INotificationHandler<MediaSavingNotification>
{
    public void Handle(MediaSavingNotification notification)
    {
        foreach (var media in notification.SavedEntities.Where(IsDisclosureImage))
        {
            if (ShouldResumeAutomaticDetection(media))
            {
                LogInvalidMetadata(media, processor.ResumeAutomaticDetection(media));
            }
            else if (media.IsPropertyDirty(Constants.SourcePropertyAlias))
            {
                LogInvalidMetadata(media, processor.Inspect(media));
            }
            else if (HasManualMetadataChange(media))
            {
                if (media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias)
                    && string.IsNullOrWhiteSpace(media.GetValue<string>(Constants.AiDisclosurePropertyAlias)))
                {
                    media.SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
                }

                media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.ManualDisclosureSourceValue);
            }
        }
    }

    private void LogInvalidMetadata(IMedia media, AiImageMetadata result)
    {
        if (result.Status != AiImageDetectionStatus.InvalidMetadata) return;

        logger.LogWarning(
            "Could not validate C2PA metadata for image media {MediaKey}; no automatic AI disclosure was applied",
            media.Key);
    }

    internal static bool ShouldInspect(IMedia media) =>
        IsDisclosureImage(media) && media.IsPropertyDirty(Constants.SourcePropertyAlias);

    private static bool IsDisclosureImage(IMedia media) =>
        media.ContentType.Alias.Equals(Constants.DefaultImageMediaTypeAlias, StringComparison.OrdinalIgnoreCase)
        && media.HasProperty(Constants.AiDisclosurePropertyAlias)
        && media.HasProperty(Constants.AiGeneratorPropertyAlias)
        && media.HasProperty(Constants.AiDisclosureSourcePropertyAlias);

    private static bool HasManualMetadataChange(IMedia media) =>
        media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias)
        || media.IsPropertyDirty(Constants.AiGeneratorPropertyAlias);

    private static bool ShouldResumeAutomaticDetection(IMedia media) =>
        media.IsPropertyDirty(Constants.AiDisclosureSourcePropertyAlias)
        && !string.Equals(
            media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias),
            Constants.ManualDisclosureSourceValue,
            StringComparison.Ordinal);
}
