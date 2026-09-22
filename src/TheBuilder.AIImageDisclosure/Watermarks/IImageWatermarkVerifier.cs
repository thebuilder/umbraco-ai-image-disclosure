namespace TheBuilder.AIImageDisclosure.Watermarks;

/// <summary>Optional verification of watermarks when an image has no Content Credentials.</summary>
public interface IImageWatermarkVerifier
{
    /// <summary>Stable per-request rescan limit. Remote verifiers should use one to bound request duration.</summary>
    int MaximumRescanBatchSize => 50;

    /// <summary>Checks an original image without changing or taking ownership of its stream.</summary>
    Task<ImageWatermarkResult> VerifyAsync(Stream image, string mediaType, CancellationToken cancellationToken = default);
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
public sealed record ImageWatermarkResult(ImageWatermarkStatus Status, string? Model = null, string? Reason = null);

internal sealed class DisabledImageWatermarkVerifier : IImageWatermarkVerifier
{
    public Task<ImageWatermarkResult> VerifyAsync(Stream image, string mediaType, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ImageWatermarkResult(ImageWatermarkStatus.Disabled));
}
