using Microsoft.Extensions.Logging;
using TheBuilder.AIGeneratedImages.Detection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.AIGeneratedImages.Media;

internal sealed class AiGeneratedMediaSavingHandler(
    IMediaAiMetadataProcessor processor,
    ILogger<AiGeneratedMediaSavingHandler> logger) : INotificationHandler<MediaSavingNotification>
{
    public void Handle(MediaSavingNotification notification)
    {
        foreach (var media in notification.SavedEntities.Where(ShouldInspect))
        {
            if (processor.Inspect(media).Status == AiImageDetectionStatus.InvalidMetadata)
            {
                logger.LogWarning(
                    "Could not validate C2PA metadata for image media {MediaKey}; manual AI metadata was preserved",
                    media.Key);
            }
        }
    }

    internal static bool ShouldInspect(IMedia media) =>
        media.ContentType.Alias.Equals(Constants.DefaultImageMediaTypeAlias, StringComparison.OrdinalIgnoreCase)
        && media.HasProperty(Constants.AiGeneratedPropertyAlias)
        && media.HasProperty(Constants.AiGeneratorPropertyAlias)
        && media.IsPropertyDirty(Constants.SourcePropertyAlias);
}
