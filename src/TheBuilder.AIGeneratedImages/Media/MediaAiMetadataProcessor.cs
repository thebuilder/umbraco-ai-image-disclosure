using Microsoft.Extensions.Logging;
using MimeKit;
using TheBuilder.AIGeneratedImages.Detection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AIGeneratedImages.Media;

internal sealed class MediaAiMetadataProcessor(
    IImageAiMetadataReader metadataReader,
    IMediaService mediaService,
    MediaUrlGeneratorCollection mediaUrlGenerators,
    ILogger<MediaAiMetadataProcessor> logger) : IMediaAiMetadataProcessor
{
    public AiImageMetadata Inspect(IMedia media)
    {
        if (!media.TryGetMediaPath(Constants.SourcePropertyAlias, mediaUrlGenerators, out string? mediaPath)
            || string.IsNullOrWhiteSpace(mediaPath))
        {
            return AiImageMetadata.InvalidMetadata;
        }

        try
        {
            using var source = mediaService.GetMediaFileContentStream(mediaPath);
            var result = metadataReader.Read(source, MimeTypes.GetMimeType(mediaPath));
            Apply(media, result);
            return result;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not inspect image media {MediaKey} at {MediaPath}", media.Key, mediaPath);
            return AiImageMetadata.InvalidMetadata;
        }
    }

    internal static void Apply(IMedia media, AiImageMetadata result)
    {
        switch (result.Status)
        {
            case AiImageDetectionStatus.Generated:
                media.SetValue(Constants.AiGeneratedPropertyAlias, true);
                media.SetValue(Constants.AiGeneratorPropertyAlias, result.Generator ?? string.Empty);
                break;
            case AiImageDetectionStatus.NotDetected:
                media.SetValue(Constants.AiGeneratedPropertyAlias, false);
                media.SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
                break;
            case AiImageDetectionStatus.InvalidMetadata:
                break;
            default:
                throw new InvalidOperationException($"Unsupported AI image detection status {result.Status}.");
        }
    }
}
