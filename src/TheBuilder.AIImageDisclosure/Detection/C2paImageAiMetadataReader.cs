using ContentAuthenticity;

namespace TheBuilder.AIImageDisclosure.Detection;

internal sealed class C2paImageAiMetadataReader : IImageAiMetadataReader
{
    private readonly IHttpResolver httpResolver;

    internal const long MaximumImageBytes = 64L * 1024 * 1024;
    internal const string OfflineReaderSettings = """
        {
          "verify": {
            "remote_manifest_fetch": false
          },
          "core": {
            "allowed_network_hosts": []
          }
        }
        """;

    public C2paImageAiMetadataReader()
        : this(DenyAllHttpResolver.Instance)
    {
    }

    internal C2paImageAiMetadataReader(IHttpResolver httpResolver)
    {
        this.httpResolver = httpResolver;
    }

    public AiImageMetadata Read(Stream image, string mediaType)
    {
        try
        {
            using var buffered = image.CanSeek ? null : BufferNonSeekableStream(image);
            var source = buffered ?? image;
            if ((image.CanSeek && image.Length > MaximumImageBytes) || source.Length > MaximumImageBytes)
                return AiImageMetadata.ImageTooLarge;

            using var contextBuilder = new ContextBuilder();
            contextBuilder.SetSettings(OfflineReaderSettings, "json");
            contextBuilder.SetHttpResolver(httpResolver);
            using var context = contextBuilder.Build();
            using var reader = new Reader(context).WithStream(source, mediaType);
            return C2paManifestParser.Parse(reader.Json);
        }
        catch (C2paException exception) when (exception.Type.TrimEnd(':') == "ManifestNotFound")
        {
            return AiImageMetadata.NoContentCredentials;
        }
        catch (C2paException)
        {
            return AiImageMetadata.InvalidMetadata;
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            return AiImageMetadata.Unreadable;
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

    internal sealed class DenyAllHttpResolver : IHttpResolver
    {
        public static readonly DenyAllHttpResolver Instance = new();

        public HttpResolverResponse Resolve(HttpResolverRequest request) => new()
        {
            Status = 403,
            Body = [],
        };
    }
}
