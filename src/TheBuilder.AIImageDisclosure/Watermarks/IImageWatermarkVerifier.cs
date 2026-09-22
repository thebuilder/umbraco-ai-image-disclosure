namespace TheBuilder.AIImageDisclosure.Watermarks;

/// <summary>Optional verification of watermarks when an image has no Content Credentials.</summary>
public interface IImageWatermarkVerifier
{
    /// <summary>Checks an original image. Open the stream only when a check can run, and dispose it before returning.</summary>
    Task<ImageWatermarkResult> VerifyAsync(Func<Stream> openImage, string mediaType, CancellationToken cancellationToken = default);
}

/// <summary>The outcome of a watermark check, independently of C2PA classification.</summary>
public enum ImageWatermarkStatus
{
    /// <summary>The optional verifier is disabled.</summary>
    Disabled,
    /// <summary>A supported watermark was detected.</summary>
    Detected,
    /// <summary>No supported watermark was detected.</summary>
    NotDetected,
    /// <summary>The check could not be completed.</summary>
    Unavailable,
    /// <summary>The image cannot be checked by this verifier.</summary>
    Unsupported,
}

/// <summary>Watermark evidence. Detection alone does not distinguish full generation from editing.</summary>
public sealed record ImageWatermarkResult(
    ImageWatermarkStatus Status, ImageWatermarkProvider? Provider = null, string? Model = null, string? Reason = null)
{
    /// <summary>Formats evidence for the read-only media property; unknown providers are never attributed to a vendor.</summary>
    public string ToStoredValue() => Status switch
    {
        ImageWatermarkStatus.Disabled => string.Empty,
        ImageWatermarkStatus.Detected => Provider?.DetectedValue ?? "AI watermark detected",
        ImageWatermarkStatus.NotDetected => Provider is null ? "No AI watermark detected" : $"No {Provider.Name} watermark detected",
        ImageWatermarkStatus.Unavailable => Provider is null ? "Watermark check unavailable" : $"{Provider.Name} watermark check unavailable",
        ImageWatermarkStatus.Unsupported => Provider is null ? "Image unsupported by watermark check" : $"Image unsupported by {Provider.Name} watermark check",
        _ => throw new ArgumentOutOfRangeException(nameof(Status)),
    };
}

/// <summary>Identifies the provider and watermark scheme independently of the detection outcome.</summary>
public sealed record ImageWatermarkProvider(string Name, string Scheme)
{
    /// <summary>The stored positive evidence used by the media tree sign.</summary>
    public string DetectedValue => $"{Name} {Scheme} detected";
}

internal sealed class DisabledImageWatermarkVerifier : IImageWatermarkVerifier
{
    public Task<ImageWatermarkResult> VerifyAsync(Func<Stream> openImage, string mediaType, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ImageWatermarkResult(ImageWatermarkStatus.Disabled));
}
