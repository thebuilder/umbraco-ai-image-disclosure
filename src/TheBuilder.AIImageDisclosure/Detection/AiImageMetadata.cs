namespace TheBuilder.AIImageDisclosure.Detection;

internal enum AiImageDetectionStatus
{
    NotDetected,
    Generated,
    Modified,
    InvalidMetadata,
}

internal sealed record AiImageMetadata(AiImageDetectionStatus Status, string? Generator = null)
{
    public static AiImageMetadata NotDetected { get; } = new(AiImageDetectionStatus.NotDetected);

    public static AiImageMetadata InvalidMetadata { get; } = new(AiImageDetectionStatus.InvalidMetadata);

    public static AiImageMetadata Generated(string? generator) =>
        new(AiImageDetectionStatus.Generated, string.IsNullOrWhiteSpace(generator) ? null : generator.Trim());

    public static AiImageMetadata Modified(string? generator) =>
        new(AiImageDetectionStatus.Modified, string.IsNullOrWhiteSpace(generator) ? null : generator.Trim());
}
