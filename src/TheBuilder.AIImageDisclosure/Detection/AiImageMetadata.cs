namespace TheBuilder.AIImageDisclosure.Detection;

internal enum AiImageDetectionStatus
{
    NotDetected,
    Generated,
    Modified,
    InvalidMetadata,
}

internal enum AiImageDetectionReason
{
    None,
    NoContentCredentials,
    InvalidCredentials,
    ImageTooLarge,
    ManifestLimitExceeded,
    NoAiDeclaration,
    Unreadable,
}

internal static class AiImageDetectionReasonExtensions
{
    public static string ToStoredValue(this AiImageDetectionReason reason) => reason switch
    {
        AiImageDetectionReason.NoContentCredentials => "No content credentials",
        AiImageDetectionReason.InvalidCredentials => "Invalid content credentials",
        AiImageDetectionReason.ImageTooLarge => "Image exceeds scan size limit",
        AiImageDetectionReason.ManifestLimitExceeded => "Content credentials exceed scan limits",
        AiImageDetectionReason.NoAiDeclaration => "No AI declaration",
        AiImageDetectionReason.Unreadable => "Image could not be read",
        _ => string.Empty,
    };
}

internal sealed record AiImageMetadata(
    AiImageDetectionStatus Status,
    string? Generator = null,
    AiImageDetectionReason Reason = AiImageDetectionReason.None)
{
    public static AiImageMetadata NotDetected { get; } = new(AiImageDetectionStatus.NotDetected, Reason: AiImageDetectionReason.NoAiDeclaration);

    public static AiImageMetadata InvalidMetadata { get; } = new(AiImageDetectionStatus.InvalidMetadata, Reason: AiImageDetectionReason.InvalidCredentials);

    public static AiImageMetadata NoContentCredentials { get; } = new(AiImageDetectionStatus.NotDetected, Reason: AiImageDetectionReason.NoContentCredentials);

    public static AiImageMetadata ImageTooLarge { get; } = new(AiImageDetectionStatus.InvalidMetadata, Reason: AiImageDetectionReason.ImageTooLarge);

    public static AiImageMetadata ManifestLimitExceeded { get; } = new(AiImageDetectionStatus.InvalidMetadata, Reason: AiImageDetectionReason.ManifestLimitExceeded);

    public static AiImageMetadata Unreadable { get; } = new(AiImageDetectionStatus.InvalidMetadata, Reason: AiImageDetectionReason.Unreadable);

    public static AiImageMetadata Generated(string? generator) =>
        new(AiImageDetectionStatus.Generated, string.IsNullOrWhiteSpace(generator) ? null : generator.Trim());

    public static AiImageMetadata Modified(string? generator) =>
        new(AiImageDetectionStatus.Modified, string.IsNullOrWhiteSpace(generator) ? null : generator.Trim());
}
