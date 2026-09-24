using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TheBuilder.AIImageDisclosure.Watermarks;

namespace TheBuilder.AIImageDisclosure.OpenAI;

internal sealed class OpenAiWatermarkVerifier(
    IOpenAiConnectionResolver connectionResolver,
    IHttpClientFactory httpClientFactory,
    OpenAiProvenanceSettingsStore settingsStore,
    ILogger<OpenAiWatermarkVerifier> logger,
    TimeSpan requestTimeout = default) : IImageWatermarkVerifier
{
    internal const long MaximumImageBytes = 50L * 1024 * 1024;
    internal const int MaximumResponseBytes = 1024 * 1024;
    private static readonly Uri Endpoint = new("https://api.openai.com/v1/content_provenance_checks");
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan effectiveRequestTimeout = requestTimeout == default ? DefaultRequestTimeout : requestTimeout;
    private readonly OpenAiConnectionCooldowns cooldowns = new();
    internal static readonly ImageWatermarkProvider Provider = new("OpenAI", "SynthID");

    public async Task<ImageWatermarkResult> VerifyAsync(
        Func<Stream> openImage,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        if (connectionResolver.IsConfigurationManaged)
        {
            if (!connectionResolver.IsEnabled)
                return new ImageWatermarkResult(ImageWatermarkStatus.Disabled, Provider);
            return await VerifyConnectionAsync(null, openImage, mediaType, cancellationToken);
        }
        var settings = settingsStore.Get();
        if (!settings.Enabled || settings.ConnectionId is not { } connectionId)
            return new ImageWatermarkResult(ImageWatermarkStatus.Disabled);
        return await VerifyConnectionAsync(connectionId, openImage, mediaType, cancellationToken);
    }

    internal async Task<ImageWatermarkResult> VerifyConnectionAsync(
        Guid? connectionId,
        Func<Stream> openImage,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        var sourceId = connectionResolver.IsConfigurationManaged ? Guid.Empty : connectionId;
        if (sourceId is { } id && cooldowns.IsCoolingDown(id))
            return Unavailable("The OpenAI provenance check is temporarily unavailable.");
        var connection = await connectionResolver.ResolveAsync(connectionId, cancellationToken);
        if (connection is null) return Unavailable("The selected OpenAI connection is unavailable or unsupported.");
        return await SendAsync(openImage, mediaType, connection, cancellationToken);
    }

    internal async Task<ImageWatermarkResult> SendAsync(
        Func<Stream> openImage,
        string mediaType,
        ResolvedOpenAiConnection connection,
        CancellationToken cancellationToken = default)
    {
        if (cooldowns.IsCoolingDown(connection.Id)) return Unavailable("The OpenAI provenance check is temporarily unavailable.");
        if (!TryGetExtension(mediaType, out var extension))
            return new ImageWatermarkResult(ImageWatermarkStatus.Unsupported, Provider, Reason: "Only PNG, JPEG, and WebP images are supported.");

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(effectiveRequestTimeout);
        var requestToken = timeoutSource.Token;
        try
        {
            using var image = openImage();
            if (!image.CanRead) return new ImageWatermarkResult(ImageWatermarkStatus.Unsupported, Provider, Reason: "The image could not be read.");
            await using var buffer = new MemoryStream();
            var copyBuffer = new byte[81920];
            while (true)
            {
                var read = await image.ReadAsync(copyBuffer, requestToken);
                if (read == 0) break;
                if (buffer.Length + read > MaximumImageBytes)
                    return new ImageWatermarkResult(ImageWatermarkStatus.Unsupported, Provider, Reason: "The image exceeds the 50 MiB limit.");
                await buffer.WriteAsync(copyBuffer.AsMemory(0, read), requestToken);
            }
            if (buffer.Length == 0) return new ImageWatermarkResult(ImageWatermarkStatus.Unsupported, Provider, Reason: "The image is empty.");
            buffer.Position = 0;
            using var multipart = new MultipartFormDataContent();
            using var file = new StreamContent(buffer);
            file.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            multipart.Add(file, "file", $"image.{extension}");
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = multipart };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);
            if (!string.IsNullOrWhiteSpace(connection.OrganizationId))
                request.Headers.TryAddWithoutValidation("OpenAI-Organization", connection.OrganizationId);
            using var response = await httpClientFactory.CreateClient("TheBuilder.AIImageDisclosure.OpenAI").SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, requestToken);
            if (!response.IsSuccessStatusCode)
            {
                var delay = GetCooldownDuration(response);
                if (delay is { } duration && duration > TimeSpan.Zero)
                    cooldowns.Extend(connection.Id, GetCooldownUntil(DateTimeOffset.UtcNow, duration));
                var message = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound =>
                        "OpenAI access to the provenance endpoint is unavailable for this connection.",
                    (HttpStatusCode)429 => "OpenAI temporarily limited provenance checks. Try again later.",
                    _ => "The OpenAI provenance check is unavailable.",
                };
                logger.LogWarning("OpenAI provenance check returned HTTP {StatusCode}", (int)response.StatusCode);
                return Unavailable(message);
            }

            var json = await ReadBoundedResponseAsync(response.Content, requestToken);
            return ParseResponse(json);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("OpenAI provenance check timed out");
            return Unavailable("The OpenAI provenance check timed out.");
        }
        catch (Exception exception) when (!IsFatal(exception) && exception is not OperationCanceledException)
        {
            logger.LogWarning("OpenAI provenance check failed ({ExceptionType})", exception.GetType().Name);
            return Unavailable("The OpenAI provenance check is unavailable.");
        }
    }

    internal static ImageWatermarkResult ParseResponse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return Unavailable("The OpenAI provenance response was not understood.");
            var recognized = false;
            var detected = false;
            string? model = null;
            foreach (var result in results.EnumerateArray())
            {
                if (!result.TryGetProperty("type", out var type) || type.GetString() != "synthid") continue;
                if (result.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String)
                    model ??= modelElement.GetString();
                if (!result.TryGetProperty("outcome", out var outcome) || outcome.ValueKind != JsonValueKind.String)
                    return Unavailable("The OpenAI provenance response was not understood.");
                recognized = true;
                if (outcome.GetString() == "detected") detected = true;
                else if (outcome.GetString() != "not_detected") return Unavailable("The OpenAI provenance check did not complete.");
            }
            if (!recognized) return Unavailable("OpenAI did not return a SynthID check result.");
            return new ImageWatermarkResult(
                detected ? ImageWatermarkStatus.Detected : ImageWatermarkStatus.NotDetected,
                Provider, model);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
            return Unavailable("The OpenAI provenance response was not understood.");
        }
    }

    private async Task<string> ReadBoundedResponseAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > MaximumResponseBytes)
                return string.Empty;
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    internal static TimeSpan? GetCooldownDuration(HttpResponseMessage response)
    {
        var duration = response.Headers.RetryAfter?.Delta;
        if (duration is null && response.Headers.RetryAfter?.Date is { } date)
            duration = date - DateTimeOffset.UtcNow;
        if (response.StatusCode == (HttpStatusCode)429)
            return duration is null ? TimeSpan.FromSeconds(30) : Max(duration.Value, TimeSpan.FromSeconds(1));
        if ((int)response.StatusCode >= 500 || response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            return TimeSpan.FromSeconds(30);
        return null;
    }

    internal static DateTimeOffset GetCooldownUntil(DateTimeOffset now, TimeSpan duration)
    {
        var remaining = DateTimeOffset.MaxValue - now;
        return now.Add(duration > remaining ? remaining : duration);
    }

    internal static string GetFileExtension(string mediaType) => mediaType.ToLowerInvariant() switch
    {
        "image/png" => "png",
        "image/jpeg" => "jpg",
        "image/webp" => "webp",
        _ => string.Empty,
    };

    private static bool TryGetExtension(string mediaType, out string extension)
    {
        extension = GetFileExtension(mediaType);
        return extension.Length != 0;
    }

    private static ImageWatermarkResult Unavailable(string reason) => new(ImageWatermarkStatus.Unavailable, Provider, Reason: reason);
    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;
    private static bool IsFatal(Exception exception) => exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
}
