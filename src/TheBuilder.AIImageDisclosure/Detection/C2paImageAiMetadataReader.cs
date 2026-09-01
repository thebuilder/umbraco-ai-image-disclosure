using ContentAuthenticity;

namespace TheBuilder.AIImageDisclosure.Detection;

internal sealed class C2paImageAiMetadataReader : IImageAiMetadataReader
{
    internal const long MaximumImageBytes = 64L * 1024 * 1024;

    public AiImageMetadata Read(Stream image, string mediaType)
    {
        try
        {
            using var buffered = image.CanSeek ? null : BufferNonSeekableStream(image);
            var source = buffered ?? image;
            if ((image.CanSeek && image.Length > MaximumImageBytes) || source.Length > MaximumImageBytes)
                return AiImageMetadata.InvalidMetadata;

            using var reader = new Reader().WithStream(source, mediaType);
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
        catch (Exception exception) when (!IsFatal(exception))
        {
            return AiImageMetadata.InvalidMetadata;
        }
    }

    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or StackOverflowException or AccessViolationException;

    private static MemoryStream BufferNonSeekableStream(Stream source)
    {
        var destination = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var bytesRead = source.Read(buffer.AsSpan());
            if (bytesRead == 0) break;
            if (destination.Length + bytesRead > MaximumImageBytes)
            {
                destination.SetLength(MaximumImageBytes + 1);
                break;
            }

            destination.Write(buffer.AsSpan(0, bytesRead));
        }

        destination.Position = 0;
        return destination;
    }
}
