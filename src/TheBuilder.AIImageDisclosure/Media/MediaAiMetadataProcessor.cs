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
        var result = Read(media);
        Apply(media, result);
        return result;
    }

    public AiImageMetadata ResumeAutomaticDetection(IMedia media)
    {
        var result = Read(media);
        ApplyAutomatic(media, result);
        return result;
    }

    private AiImageMetadata Read(IMedia media)
    {
        string? mediaPath = null;
        try
        {
            if (!media.TryGetMediaPath(Constants.SourcePropertyAlias, mediaUrlGenerators, out mediaPath)
                || string.IsNullOrWhiteSpace(mediaPath))
            {
                return AiImageMetadata.Unreadable;
            }

            using var source = mediaService.GetMediaFileContentStream(mediaPath);
            return metadataReader.Read(source, MimeTypes.GetMimeType(mediaPath));
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            logger.LogWarning(exception, "Could not inspect image media {MediaKey} at {MediaPath}", media.Key, mediaPath);
            return AiImageMetadata.Unreadable;
        }
    }

    internal static void Apply(IMedia media, AiImageMetadata result)
    {
        if (HasManualMetadata(media))
        {
            media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.ManualDisclosureSourceValue);
            return;
        }

        ApplyAutomatic(media, result);
    }

    internal static void ApplyAutomatic(IMedia media, AiImageMetadata result)
    {
        switch (result.Status)
        {
            case AiImageDetectionStatus.Generated:
                media.SetValue(Constants.AiDisclosurePropertyAlias, Constants.GeneratedDisclosureValue);
                media.SetValue(Constants.AiGeneratorPropertyAlias, result.Generator ?? string.Empty);
                media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.C2paDisclosureSourceValue);
                SetReason(media, result);
                break;
            case AiImageDetectionStatus.Modified:
                media.SetValue(Constants.AiDisclosurePropertyAlias, Constants.ModifiedDisclosureValue);
                media.SetValue(Constants.AiGeneratorPropertyAlias, result.Generator ?? string.Empty);
                media.SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.C2paDisclosureSourceValue);
                SetReason(media, result);
                break;
            case AiImageDetectionStatus.NotDetected:
            case AiImageDetectionStatus.InvalidMetadata:
                ClearAutomaticMetadata(media);
                SetReason(media, result);
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
        media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias);

    private static void ClearAutomaticMetadata(IMedia media)
    {
        media.SetValue(Constants.AiDisclosurePropertyAlias, string.Empty);
        media.SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
        media.SetValue(Constants.AiDisclosureSourcePropertyAlias, string.Empty);
    }

    private static void SetReason(IMedia media, AiImageMetadata result) =>
        media.SetValue(Constants.AiDisclosureReasonPropertyAlias, result.Reason.ToStoredValue());

    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
}
