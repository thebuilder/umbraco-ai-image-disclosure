namespace TheBuilder.AIImageDisclosure.Detection;

internal interface IImageAiMetadataReader
{
    AiImageMetadata Read(Stream image, string mediaType);
}
