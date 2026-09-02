using NSubstitute;
using System.Text.Json;
using TheBuilder.AIImageDisclosure.Detection;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class C2paIntegrationTests
{
    [Fact]
    public void ReaderPolicyDisablesAllNetworkAccess()
    {
        using var settings = JsonDocument.Parse(C2paImageAiMetadataReader.OfflineReaderSettings);

        Assert.False(settings.RootElement
            .GetProperty("verify")
            .GetProperty("remote_manifest_fetch")
            .GetBoolean());
        Assert.Empty(settings.RootElement
            .GetProperty("core")
            .GetProperty("allowed_network_hosts")
            .EnumerateArray());
    }

    [Fact]
    public void RemoteManifestReferenceDoesNotInvokeResolver()
    {
        var resolver = Substitute.For<ContentAuthenticity.IHttpResolver>();
        // Source: contentauth/c2pa-rs sdk/tests/fixtures/libpng-test_with_url.png.
        var imageBytes = Convert.FromBase64String(
            File.ReadAllText(Fixture("remote-manifest-reference.png.b64")));
        Assert.Contains(
            "http://localhost:5000/libpng-test.c2pa",
            System.Text.Encoding.UTF8.GetString(imageBytes),
            StringComparison.Ordinal);
        using var image = new MemoryStream(imageBytes);

        var result = new C2paImageAiMetadataReader(resolver).Read(image, "image/png");

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, result.Status);
        resolver.DidNotReceive().Resolve(Arg.Any<ContentAuthenticity.HttpResolverRequest>());
    }

    [Fact]
    public void OfflineResolverRejectsEveryRequest()
    {
        var resolver = new C2paImageAiMetadataReader.DenyAllHttpResolver();

        var response = resolver.Resolve(new ContentAuthenticity.HttpResolverRequest
        {
            Url = new Uri("https://example.invalid/manifest.c2pa"),
            Method = "GET",
        });

        Assert.Equal(403, response.Status);
        Assert.Empty(response.Body);
    }

    [Fact]
    public void ReadsFreshlyGeneratedOpenAiImage()
    {
        using var image = File.OpenRead(Fixture("openai-generated-c2pa.png"));

        var result = new C2paImageAiMetadataReader().Read(image, "image/png");

        Assert.Equal(AiImageDetectionStatus.Generated, result.Status);
        Assert.Equal("gpt-image", result.Generator);
    }

    [Fact]
    public void ReadsProvidedGoogleManifestChain()
    {
        var result = C2paManifestParser.Parse(File.ReadAllText(Fixture("google-generated-c2pa.json")));

        Assert.Equal(AiImageDetectionStatus.Generated, result.Status);
        Assert.Null(result.Generator);
    }

    [Fact]
    public void RejectsMalformedImageWithoutThrowing()
    {
        using var image = new MemoryStream([0x01, 0x02, 0x03]);

        var result = new C2paImageAiMetadataReader().Read(image, "image/png");

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, result.Status);
    }

    [Fact]
    public void RejectsImagesAboveResourceLimitBeforeNativeParsing()
    {
        var image = Substitute.For<Stream>();
        image.CanSeek.Returns(true);
        image.Length.Returns(C2paImageAiMetadataReader.MaximumImageBytes + 1);

        var result = new C2paImageAiMetadataReader().Read(image, "image/png");

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, result.Status);
    }

    [Fact]
    public void RejectsNonSeekableImagesAboveResourceLimitBeforeNativeParsing()
    {
        using var image = new RepeatingNonSeekableStream(C2paImageAiMetadataReader.MaximumImageBytes + 1);

        var result = new C2paImageAiMetadataReader().Read(image, "image/png");

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, result.Status);
    }

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private sealed class RepeatingNonSeekableStream(long length) : Stream
    {
        private long remaining = length;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = (int)Math.Min(remaining, count);
            Array.Clear(buffer, offset, bytesRead);
            remaining -= bytesRead;
            return bytesRead;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
