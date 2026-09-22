namespace TheBuilder.AIImageDisclosure.Media;

/// <summary>Stable request limits for administrator media scans.</summary>
public sealed class MediaAiRescanOptions
{
    /// <summary>Maximum entities per request. Configure at startup so page boundaries remain stable.</summary>
    public int MaximumBatchSize { get; set; } = 50;
}
