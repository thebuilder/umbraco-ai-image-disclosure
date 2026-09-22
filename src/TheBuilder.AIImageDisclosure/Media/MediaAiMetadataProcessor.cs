using Microsoft.Extensions.Logging;
using MimeKit;
using TheBuilder.AIImageDisclosure.Watermarks;
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
    ILogger<MediaAiMetadataProcessor> logger,
    IImageWatermarkVerifier? watermarkVerifier = null) : IMediaAiMetadataProcessor
{
    public async Task<AiImageMetadata> InspectAsync(IMedia media, CancellationToken cancellationToken = default)
    {
        var manual = HasManualMetadata(media);
        var result = Read(media);
        Apply(media, result);
        if (manual) ClearWatermark(media);
        else await InspectWatermarkAsync(media, result, cancellationToken);
        return result;
    }

    public async Task<AiImageMetadata> ResumeAutomaticDetectionAsync(IMedia media, CancellationToken cancellationToken = default)
    {
        var result = Read(media);
        ApplyAutomatic(media, result);
        await InspectWatermarkAsync(media, result, cancellationToken);
        return result;
    }

    private async Task InspectWatermarkAsync(IMedia media, AiImageMetadata result, CancellationToken cancellationToken)
    {
        ClearWatermark(media);
        if (result.Reason != AiImageDetectionReason.NoContentCredentials || watermarkVerifier is null
            || !media.HasProperty(Constants.AiWatermarkPropertyAlias)) return;
        try
        {
            if (!media.TryGetMediaPath(Constants.SourcePropertyAlias, mediaUrlGenerators, out var path)
                || string.IsNullOrWhiteSpace(path)) return;
            var fileValue = media.GetValue<string>(Constants.SourcePropertyAlias);
            using var source = mediaService.GetMediaFileContentStream(path);
            var watermark = await watermarkVerifier.VerifyAsync(source, MimeTypes.GetMimeType(path), cancellationToken);
            if (media.GetValue<string>(Constants.SourcePropertyAlias) != fileValue
                || IsManualSource(media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias))) return;
            media.SetValue(Constants.AiWatermarkPropertyAlias, watermark.Status switch
            {
                ImageWatermarkStatus.Detected => Constants.OpenAiWatermarkDetected,
                ImageWatermarkStatus.NotDetected => "No OpenAI watermark detected",
                ImageWatermarkStatus.Unavailable => "OpenAI watermark check unavailable",
                ImageWatermarkStatus.Unsupported => "Image unsupported by OpenAI watermark check",
                _ => string.Empty,
            });
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            // Remote verification is optional; never turn its failure into a failed media save.
            media.SetValue(Constants.AiWatermarkPropertyAlias, "OpenAI watermark check unavailable");
            logger.LogWarning("Watermark check could not complete for image media {MediaKey}", media.Key);
        }
    }

    private static void ClearWatermark(IMedia media)
    {
        if (media.HasProperty(Constants.AiWatermarkPropertyAlias))
            media.SetValue(Constants.AiWatermarkPropertyAlias, string.Empty);
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
        || IsManualSource(media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias));

    internal static bool IsManualSource(string? value) =>
        value is Constants.ManualDisclosureSourceValue or "[\"Manual\"]";

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
