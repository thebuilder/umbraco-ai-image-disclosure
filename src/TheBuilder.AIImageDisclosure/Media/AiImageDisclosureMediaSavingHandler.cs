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
            if (media.IsPropertyDirty(Constants.SourcePropertyAlias))
            {
                if (processor.Inspect(media).Status == AiImageDetectionStatus.InvalidMetadata)
                {
                    logger.LogWarning(
                        "Could not validate C2PA metadata for image media {MediaKey}; manual AI metadata was preserved",
                        media.Key);
                }
            }
            else if (HasManualMetadataChange(media))
            {
                media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.ManualDisclosureSourceValue);
            }
        }
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
}
