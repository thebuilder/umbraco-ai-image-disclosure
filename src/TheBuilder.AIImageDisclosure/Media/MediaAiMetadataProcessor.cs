using Microsoft.Extensions.Logging;
using MimeKit;
using TheBuilder.AIImageDisclosure.Detection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.Media;

internal sealed class MediaAiMetadataProcessor(
    IImageAiMetadataReader metadataReader,
    IMediaService mediaService,
    MediaUrlGeneratorCollection mediaUrlGenerators,
    ILogger<MediaAiMetadataProcessor> logger) : IMediaAiMetadataProcessor
{
    public AiImageMetadata Inspect(IMedia media)
    {
        string? mediaPath = null;
        try
        {
            if (!media.TryGetMediaPath(Constants.SourcePropertyAlias, mediaUrlGenerators, out mediaPath)
                || string.IsNullOrWhiteSpace(mediaPath))
            {
                Apply(media, AiImageMetadata.InvalidMetadata);
                return AiImageMetadata.InvalidMetadata;
            }

            using var source = mediaService.GetMediaFileContentStream(mediaPath);
            var result = metadataReader.Read(source, MimeTypes.GetMimeType(mediaPath));
            Apply(media, result);
            return result;
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            logger.LogWarning(exception, "Could not inspect image media {MediaKey} at {MediaPath}", media.Key, mediaPath);
            Apply(media, AiImageMetadata.InvalidMetadata);
            return AiImageMetadata.InvalidMetadata;
        }
    }

    internal static void Apply(IMedia media, AiImageMetadata result)
    {
        if (HasManualMetadata(media))
        {
            media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.ManualDisclosureSourceValue);
            return;
        }

        switch (result.Status)
        {
            case AiImageDetectionStatus.Generated:
                media.SetValue(Constants.AiDisclosurePropertyAlias, Constants.GeneratedDisclosureValue);
                media.SetValue(Constants.AiGeneratorPropertyAlias, result.Generator ?? string.Empty);
                media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.C2paDisclosureSourceValue);
                break;
            case AiImageDetectionStatus.Modified:
                media.SetValue(Constants.AiDisclosurePropertyAlias, Constants.ModifiedDisclosureValue);
                media.SetValue(Constants.AiGeneratorPropertyAlias, result.Generator ?? string.Empty);
                media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.C2paDisclosureSourceValue);
                break;
            case AiImageDetectionStatus.NotDetected:
            case AiImageDetectionStatus.InvalidMetadata:
                ClearAutomaticMetadata(media);
                break;
            default:
                throw new InvalidOperationException($"Unsupported AI image detection status {result.Status}.");
        }
    }

    private static bool HasManualMetadata(IMedia media) =>
        HasDirtyMetadata(media)
        || string.Equals(
            media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias),
            Constants.ManualDisclosureSourceValue,
            StringComparison.Ordinal);

    private static bool HasDirtyMetadata(IMedia media) =>
        media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias)
        || media.IsPropertyDirty(Constants.AiGeneratorPropertyAlias);

    private static void ClearAutomaticMetadata(IMedia media)
    {
        media.SetValue(Constants.AiDisclosurePropertyAlias, string.Empty);
        media.SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
        media.SetValue(Constants.AiDisclosureSourcePropertyAlias, string.Empty);
    }

    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
}
