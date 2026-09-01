namespace TheBuilder.AIGeneratedImages.Detection;

internal interface IImageAiMetadataReader
{
    AiImageMetadata Read(Stream image, string mediaType);
}
