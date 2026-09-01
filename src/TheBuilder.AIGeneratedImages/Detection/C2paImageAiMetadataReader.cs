using ContentAuthenticity;

namespace TheBuilder.AIGeneratedImages.Detection;

internal sealed class C2paImageAiMetadataReader : IImageAiMetadataReader
{
    public AiImageMetadata Read(Stream image, string mediaType)
    {
        try
        {
            using var reader = new Reader().WithStream(image, mediaType);
            return C2paManifestParser.Parse(reader.Json);
        }
        catch (C2paException exception) when (exception.Type == "ManifestNotFound")
        {
            return AiImageMetadata.NotDetected;
        }
        catch (C2paException)
        {
            return AiImageMetadata.InvalidMetadata;
        }
    }
}
