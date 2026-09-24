namespace TheBuilder.AIImageDisclosure.OpenAI;

/// <summary>Tracks temporary OpenAI cooldowns independently for each configured connection.</summary>
internal sealed class OpenAiConnectionCooldowns(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<Guid, DateTimeOffset> cooldownUntilByConnection = [];
    private readonly object sync = new();

    public bool IsCoolingDown(Guid connectionId)
    {
        var now = clock.GetUtcNow();
        lock (sync)
        {
            RemoveExpired(now);
            return cooldownUntilByConnection.TryGetValue(connectionId, out var until) && until > now;
        }
    }

    public void Extend(Guid connectionId, DateTimeOffset until)
    {
        var now = clock.GetUtcNow();
        lock (sync)
        {
            RemoveExpired(now);
            if (until <= now) return;
            if (cooldownUntilByConnection.TryGetValue(connectionId, out var current) && current >= until) return;
            cooldownUntilByConnection[connectionId] = until;
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var connectionId in cooldownUntilByConnection
                     .Where(entry => entry.Value <= now)
                     .Select(entry => entry.Key)
                     .ToArray())
            cooldownUntilByConnection.Remove(connectionId);
    }
}
