namespace TheBuilder.AIImageDisclosure.OpenAI;

internal sealed class EmptyOpenAiConnectionSource : IOpenAiConnectionSource
{
    public bool IsAvailable => false;
    public Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OpenAiConnectionOption>>([]);
    public Task<ResolvedOpenAiConnection?> ResolveAsync(Guid connectionId, CancellationToken cancellationToken) =>
        Task.FromResult<ResolvedOpenAiConnection?>(null);
}
