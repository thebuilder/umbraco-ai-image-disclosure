namespace TheBuilder.AIImageDisclosure.Media;

/// <summary>Tracks progress through media IDs up to the maximum captured when a scan starts.</summary>
/// <param name="LastId">The last media ID processed.</param>
/// <param name="MaximumId">The highest media ID included in this scan.</param>
public sealed record RescanCursor(int LastId, int MaximumId);
