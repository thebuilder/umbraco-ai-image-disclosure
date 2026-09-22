using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.OpenAI;

internal sealed class OpenAiProvenanceSettingsStore(IServiceScopeFactory scopeFactory)
{
    private const string Key = "TheBuilder.AIImageDisclosure.OpenAI.Settings";
    private static readonly object Sync = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OpenAiProvenanceSettings Get()
    {
        lock (Sync)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var keyValueService = scope.ServiceProvider.GetRequiredService<IKeyValueService>();
                return JsonSerializer.Deserialize<OpenAiProvenanceSettings>(keyValueService.GetValue(Key) ?? "{}", JsonOptions)
                    ?? OpenAiProvenanceSettings.Disabled;
            }
            catch (JsonException)
            {
                return OpenAiProvenanceSettings.Disabled;
            }
        }
    }

    public void Save(OpenAiProvenanceSettings settings)
    {
        lock (Sync)
        {
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<IKeyValueService>()
                .SetValue(Key, JsonSerializer.Serialize(settings, JsonOptions));
        }
    }
}

internal sealed record OpenAiProvenanceSettings(bool Enabled, Guid? ConnectionId)
{
    public static OpenAiProvenanceSettings Disabled { get; } = new(false, null);
}
