using Umbraco.AI.Core.EditableModels;

namespace TheBuilder.AIImageDisclosure.OpenAI;

internal sealed class OpenAiConnectionSettings
{
    [AIField(IsSensitive = true)]
    public string? ApiKey { get; set; }

    [AIField]
    public string? OrganizationId { get; set; }

    [AIField]
    public string? Endpoint { get; set; } = "https://api.openai.com/v1";
}
