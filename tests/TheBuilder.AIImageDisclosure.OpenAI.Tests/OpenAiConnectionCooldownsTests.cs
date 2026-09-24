using TheBuilder.AIImageDisclosure.OpenAI;

namespace TheBuilder.AIImageDisclosure.OpenAI.Tests;

public sealed class OpenAiConnectionCooldownsTests
{
    [Fact]
    public void CooldownsAreIsolatedByConnection()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-09-22T12:00:00Z"));
        var cooldowns = new OpenAiConnectionCooldowns(clock);
        var firstConnection = Guid.NewGuid();
        var secondConnection = Guid.NewGuid();

        cooldowns.Extend(firstConnection, clock.GetUtcNow().AddMinutes(2));

        Assert.True(cooldowns.IsCoolingDown(firstConnection));
        Assert.False(cooldowns.IsCoolingDown(secondConnection));
    }

    [Fact]
    public void ExtendingCooldownNeverShortensItsExistingDeadline()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-09-22T12:00:00Z"));
        var cooldowns = new OpenAiConnectionCooldowns(clock);
        var connectionId = Guid.NewGuid();
        cooldowns.Extend(connectionId, clock.GetUtcNow().AddMinutes(20));

        cooldowns.Extend(connectionId, clock.GetUtcNow().AddMinutes(5));
        clock.Advance(TimeSpan.FromMinutes(6));
        Assert.True(cooldowns.IsCoolingDown(connectionId));

        clock.Advance(TimeSpan.FromMinutes(15));
        Assert.False(cooldowns.IsCoolingDown(connectionId));
    }

    [Fact]
    public void ConcurrentExtensionsKeepTheLongestDeadline()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-09-22T12:00:00Z"));
        var cooldowns = new OpenAiConnectionCooldowns(clock);
        var connection = Guid.NewGuid();
        Parallel.For(1, 61, minutes => cooldowns.Extend(connection, clock.GetUtcNow().AddMinutes(minutes)));
        clock.Advance(TimeSpan.FromMinutes(59));
        Assert.True(cooldowns.IsCoolingDown(connection));
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.False(cooldowns.IsCoolingDown(connection));
    }

    [Fact]
    public void ExpiredEntriesAreRemovedAndCanBeExtendedAgain()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-09-22T12:00:00Z"));
        var cooldowns = new OpenAiConnectionCooldowns(clock);
        var connectionId = Guid.NewGuid();
        cooldowns.Extend(connectionId, clock.GetUtcNow().AddSeconds(3));
        clock.Advance(TimeSpan.FromSeconds(4));

        Assert.False(cooldowns.IsCoolingDown(connectionId));
        cooldowns.Extend(connectionId, clock.GetUtcNow().AddSeconds(3));
        Assert.True(cooldowns.IsCoolingDown(connectionId));
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan amount) => current = current.Add(amount);
    }
}
