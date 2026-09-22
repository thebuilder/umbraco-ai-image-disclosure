using System.Net;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.EditableModels;
using NSubstitute;
using TheBuilder.AIImageDisclosure.OpenAI;
using TheBuilder.AIImageDisclosure.OpenAI.Controllers;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Providers;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.OpenAI.Tests;

public sealed class OpenAiWatermarkVerifierTests
{
    [Theory]
    [InlineData("{\"model\":\"gpt-4o-mini\",\"results\":[{\"type\":\"synthid\",\"outcome\":\"detected\"}]}", ImageWatermarkStatus.Detected)]
    [InlineData("{\"results\":[{\"type\":\"synthid\",\"outcome\":\"not_detected\"}]}", ImageWatermarkStatus.NotDetected)]
    [InlineData("{\"results\":[{\"type\":\"c2pa\",\"outcome\":\"detected\"}]}", ImageWatermarkStatus.Unavailable)]
    [InlineData("{\"results\":[{\"type\":\"synthid\",\"outcome\":\"failed\"}]}", ImageWatermarkStatus.Unavailable)]
    [InlineData("{\"results\":[{\"type\":\"synthid\"}]}", ImageWatermarkStatus.Unavailable)]
    [InlineData("{", ImageWatermarkStatus.Unavailable)]
    public void SelectsOnlyWellFormedSynthIdResults(string json, ImageWatermarkStatus expected)
    {
        Assert.Equal(expected, OpenAiWatermarkVerifier.ParseResponse(json).Status);
    }

    [Fact]
    public void ReadsModelFromSynthIdResult()
    {
        var result = OpenAiWatermarkVerifier.ParseResponse(
            "{\"model\":\"ignored-root-model\",\"results\":[{\"type\":\"synthid\",\"outcome\":\"detected\",\"model\":\"synthid-v2\"}]} ");

        Assert.Equal("synthid-v2", result.Model);
    }

    [Fact]
    public void BundledTestImageIs512PixelPngWithValidChunkCrcsAndDecodablePixels()
    {
        var png = OpenAiProvenanceController.ReadTestImage();
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png[..8]);
        var offset = 8;
        var width = 0;
        var height = 0;
        using var compressedPixels = new MemoryStream();
        var reachedEnd = false;
        while (offset < png.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4)));
            offset += 4;
            var type = png.AsSpan(offset, 4).ToArray();
            offset += 4;
            var data = png.AsSpan(offset, length).ToArray();
            offset += length;
            var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4));
            offset += 4;
            Assert.Equal(expectedCrc, PngCrc32(type, data));

            var chunkType = Encoding.ASCII.GetString(type);
            if (chunkType == "IHDR")
            {
                width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4)));
                height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4)));
                Assert.Equal(8, data[8]);
                Assert.Equal(2, data[9]);
            }
            else if (chunkType == "IDAT") compressedPixels.Write(data);
            else if (chunkType == "IEND") reachedEnd = true;
        }

        Assert.True(reachedEnd);
        Assert.Equal(512, width);
        Assert.Equal(512, height);
        compressedPixels.Position = 0;
        using var decoder = new ZLibStream(compressedPixels, CompressionMode.Decompress);
        using var decodedPixels = new MemoryStream();
        decoder.CopyTo(decodedPixels);
        Assert.Equal((512 * 3 + 1) * 512, decodedPixels.Length);
        Assert.Contains(decodedPixels.ToArray(), value => value != 0);
    }

    [Fact]
    public void HonorsRetryAfterLongerThanTenMinutesAndBoundsDateTimeOverflow()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMinutes(45));

        Assert.Equal(TimeSpan.FromMinutes(45), OpenAiWatermarkVerifier.GetCooldownDuration(response));
        Assert.Equal(DateTimeOffset.MaxValue,
            OpenAiWatermarkVerifier.GetCooldownUntil(DateTimeOffset.MaxValue.AddMinutes(-1), TimeSpan.FromDays(1)));
    }

    [Fact]
    public void SettingsAndTestRoutesMatchBackofficeApiContract()
    {
        var controllerType = typeof(OpenAiProvenanceController);
        Assert.Equal("umbraco/backoffice/api/ai-image-disclosure/openai",
            controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().Single().Template);
        Assert.Equal("settings", controllerType.GetMethod(nameof(OpenAiProvenanceController.GetSettings))!
            .GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template);
        Assert.Equal("settings", controllerType.GetMethod(nameof(OpenAiProvenanceController.SaveSettings))!
            .GetCustomAttributes(typeof(HttpPutAttribute), true).Cast<HttpPutAttribute>().Single().Template);
        Assert.Equal("test", controllerType.GetMethod(nameof(OpenAiProvenanceController.TestConnection))!
            .GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Single().Template);
    }

    [Theory]
    [InlineData("https://api.openai.com/v1", true)]
    [InlineData("https://api.openai.com/v1/", true)]
    [InlineData("https://api.openai.com/v1?redirect=evil.test", false)]
    [InlineData("https://api.openai.com.evil.test/v1", false)]
    [InlineData("http://api.openai.com/v1", false)]
    [InlineData("https://proxy.example/v1", false)]
    [InlineData("https://user:pass@api.openai.com/v1", false)]
    public void AllowsOnlyDirectOpenAiV1Endpoint(string endpoint, bool expected)
    {
        Assert.Equal(expected, OpenAiConnectionResolver.IsDirectOpenAiEndpoint(endpoint));
    }

    [Theory]
    [InlineData("image/png", "png")]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/webp", "webp")]
    [InlineData("image/gif", "")]
    public void MapsOnlySupportedMediaTypes(string mediaType, string expected)
    {
        Assert.Equal(expected, OpenAiWatermarkVerifier.GetFileExtension(mediaType));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SanitizesProviderFailuresAndDoesNotExposeBody(HttpStatusCode status)
    {
        var body = "provider error body contains secret diagnostic details";
        var handler = new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body),
        });
        var verifier = CreateVerifier(handler);
        await using var image = new MemoryStream([1, 2, 3]);

        var result = await verifier.SendAsync(image, "image/png", Connection(), TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Unavailable, result.Status);
        Assert.DoesNotContain(body, result.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostsImageToFixedEndpointWithOrganizationAndMultipartContent()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"type\":\"synthid\",\"outcome\":\"detected\"}]}", Encoding.UTF8, "application/json"),
            };
        });
        var verifier = CreateVerifier(handler);
        await using var image = new MemoryStream([1, 2, 3]);

        var result = await verifier.SendAsync(image, "image/png", Connection(), TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Detected, result.Status);
        Assert.Equal("https://api.openai.com/v1/content_provenance_checks", captured!.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer test-key", captured.Headers.Authorization!.ToString());
        Assert.Equal("org-test", Assert.Single(captured.Headers.GetValues("OpenAI-Organization")));
        Assert.Contains("multipart/form-data", captured.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task RejectsUnsupportedAndOversizedImagesBeforeNetwork()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be used"));
        var verifier = CreateVerifier(handler);
        await using var unsupported = new MemoryStream([1]);
        await using var oversized = new RepeatedByteStream(OpenAiWatermarkVerifier.MaximumImageBytes + 1);

        var unsupportedResult = await verifier.SendAsync(unsupported, "image/gif", Connection(), TestContext.Current.CancellationToken);
        var oversizedResult = await verifier.SendAsync(oversized, "image/png", Connection(), TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Unsupported, unsupportedResult.Status);
        Assert.Equal(ImageWatermarkStatus.Unsupported, oversizedResult.Status);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task ExplicitConnectionTestHonorsCooldownAfterRateLimit()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Headers = { RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30)) },
        });
        var verifier = CreateVerifier(handler);
        await using var firstImage = new MemoryStream([1, 2, 3]);
        await using var testImage = new MemoryStream([1, 2, 3]);

        var first = await verifier.SendAsync(firstImage, "image/png", Connection(), TestContext.Current.CancellationToken);
        var second = await verifier.VerifyConnectionAsync(Guid.NewGuid(), testImage, "image/png", TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Unavailable, first.Status);
        Assert.Equal(ImageWatermarkStatus.Unavailable, second.Status);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task TimeoutCoversResponseBodyRead()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new DelayedReadStream()),
        });
        var verifier = CreateVerifier(handler, TimeSpan.FromMilliseconds(50));
        await using var image = new MemoryStream([1, 2, 3]);

        var result = await verifier.SendAsync(image, "image/png", Connection(), TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Unavailable, result.Status);
        Assert.Contains("timed out", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettingsJsonContainsOnlyEnabledAndConnectionId()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new OpenAiProvenanceSettings(true, Guid.Parse("3d835629-67b5-46d1-8f0b-ff5c8936716d")));

        Assert.Contains("connectionid", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("test-key", json, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyApiKeyFieldIsMarkedSensitive()
    {
        var properties = typeof(OpenAiConnectionSettings).GetProperties();
        Assert.True(properties.Single(property => property.Name == "ApiKey")
            .GetCustomAttributes(typeof(AIFieldAttribute), inherit: true).Cast<AIFieldAttribute>().Single().IsSensitive);
        Assert.False(properties.Single(property => property.Name == "OrganizationId")
            .GetCustomAttributes(typeof(AIFieldAttribute), inherit: true).Cast<AIFieldAttribute>().Single().IsSensitive);
    }

    [Fact]
    public async Task ResolvesActiveOpenAiConnectionThroughSecretAwareModelResolver()
    {
        var connection = NewConnection("openai", active: true, new { ApiKey = "$Secrets:OpenAI", Endpoint = "https://api.openai.com/v1" });
        var settings = new OpenAiConnectionSettings
        {
            ApiKey = "resolved-secret-value",
            Endpoint = "https://api.openai.com/v1",
        };
        var (resolver, modelResolver) = CreateConnectionResolver(connection, settings);

        var resolved = await resolver.ResolveAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.NotNull(resolved);
        Assert.Equal("resolved-secret-value", resolved.ApiKey);
        modelResolver.Received(1).ResolveModel<OpenAiConnectionSettings>(connection.Settings, Arg.Any<AIEditableModelSchema?>());
    }

    [Fact]
    public async Task RejectsMissingInactiveWrongProviderMissingKeyAndCustomEndpointConnections()
    {
        var key = new OpenAiConnectionSettings { ApiKey = "key", Endpoint = "https://api.openai.com/v1" };
        var customEndpoint = new OpenAiConnectionSettings { ApiKey = "key", Endpoint = "https://proxy.example/v1" };
        var missingKey = new OpenAiConnectionSettings { Endpoint = "https://api.openai.com/v1" };
        var invalid = new (AIConnection? Connection, OpenAiConnectionSettings? Settings)[]
        {
            (null, key),
            (NewConnection("openai", active: false, new object()), key),
            (NewConnection("azure", active: true, new object()), key),
            (NewConnection("openai", active: true, new object()), missingKey),
            (NewConnection("openai", active: true, new object()), customEndpoint),
        };

        foreach (var (connection, settings) in invalid)
        {
            var (resolver, _) = CreateConnectionResolver(connection, settings);
            Assert.Null(await resolver.ResolveAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task RejectsUnauthenticatedSettingsRequests()
    {
        var backOfficeSecurity = Substitute.For<IBackOfficeSecurity>();
        backOfficeSecurity.CurrentUser.Returns((Umbraco.Cms.Core.Models.Membership.IUser?)null);
        var securityAccessor = Substitute.For<IBackOfficeSecurityAccessor>();
        securityAccessor.BackOfficeSecurity.Returns(backOfficeSecurity);
        var services = new ServiceCollection().AddSingleton(securityAccessor).BuildServiceProvider();
        var controller = new OpenAiProvenanceController(services);

        var result = await controller.GetSettings(TestContext.Current.CancellationToken);

        Assert.IsType<Microsoft.AspNetCore.Mvc.ForbidResult>(result);
    }

    [Fact]
    public async Task RejectsAuthenticatedNonAdminSettingsRequests()
    {
        var user = Substitute.For<Umbraco.Cms.Core.Models.Membership.IUser>();
        user.Groups.Returns([]);
        var backOfficeSecurity = Substitute.For<IBackOfficeSecurity>();
        backOfficeSecurity.CurrentUser.Returns(user);
        var securityAccessor = Substitute.For<IBackOfficeSecurityAccessor>();
        securityAccessor.BackOfficeSecurity.Returns(backOfficeSecurity);
        var services = new ServiceCollection().AddSingleton(securityAccessor).BuildServiceProvider();
        var controller = new OpenAiProvenanceController(services);

        var result = await controller.GetSettings(TestContext.Current.CancellationToken);

        Assert.IsType<Microsoft.AspNetCore.Mvc.ForbidResult>(result);
    }

    private static OpenAiWatermarkVerifier CreateVerifier(StubHandler handler, TimeSpan requestTimeout = default)
    {
        var keyValueService = Substitute.For<IKeyValueService>();
        var services = new ServiceCollection()
            .AddSingleton(keyValueService)
            .BuildServiceProvider();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler, disposeHandler: false));
        return new OpenAiWatermarkVerifier(new OpenAiConnectionResolver(services.GetRequiredService<IServiceScopeFactory>()), factory,
            new OpenAiProvenanceSettingsStore(services.GetRequiredService<IServiceScopeFactory>()),
            Substitute.For<ILogger<OpenAiWatermarkVerifier>>(), requestTimeout);
    }

    private static ResolvedOpenAiConnection Connection() =>
        new("test-key", "org-test", Guid.NewGuid(), "test");

    private static AIConnection NewConnection(string providerId, bool active, object settings) =>
        new()
        {
            Alias = "test-connection",
            Name = "Test connection",
            ProviderId = providerId,
            IsActive = active,
            Settings = settings,
        };

    private static (OpenAiConnectionResolver Resolver, IAIEditableModelResolver ModelResolver) CreateConnectionResolver(
        AIConnection? connection,
        OpenAiConnectionSettings? settings)
    {
        var connectionService = Substitute.For<IAIConnectionService>();
        connectionService.GetConnectionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connection));
        var modelResolver = Substitute.For<IAIEditableModelResolver>();
        modelResolver.ResolveModel<OpenAiConnectionSettings>(Arg.Any<object?>(), Arg.Any<AIEditableModelSchema?>())
            .Returns(settings);
        var openAiProvider = Substitute.For<IAIProvider>();
        openAiProvider.Id.Returns("openai");
        var providerCollection = new AIProviderCollection(() => [openAiProvider]);
        var services = new ServiceCollection()
            .AddSingleton(connectionService)
            .AddSingleton(modelResolver)
            .AddSingleton(providerCollection)
            .BuildServiceProvider();
        return (new OpenAiConnectionResolver(services.GetRequiredService<IServiceScopeFactory>()), modelResolver);
    }

    private static uint PngCrc32(byte[] type, byte[] data)
    {
        var crc = 0xffffffffu;
        foreach (var value in type.Concat(data))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
        }
        return crc ^ 0xffffffffu;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(response(request));
        }
    }

    private sealed class RepeatedByteStream : Stream
    {
        private readonly long length;
        private long remaining;
        public RepeatedByteStream(long length) { this.length = length; remaining = length; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get => length - remaining; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer)
        {
            var length = (int)Math.Min(buffer.Length, remaining);
            buffer[..length].Fill(42);
            remaining -= length;
            return length;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Read(buffer.Span));
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class DelayedReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            new(Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(_ => 0, cancellationToken));
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
