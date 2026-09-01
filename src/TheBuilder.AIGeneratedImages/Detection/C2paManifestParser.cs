using System.Text.Json;

namespace TheBuilder.AIGeneratedImages.Detection;

internal static class C2paManifestParser
{
    private const string TrainedAlgorithmicMedia =
        "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia";

    public static AiImageMetadata Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!HasUsableValidationState(root)
                || !root.TryGetProperty("active_manifest", out var activeManifestElement)
                || activeManifestElement.GetString() is not { Length: > 0 } activeManifest
                || !root.TryGetProperty("manifests", out var manifests))
            {
                return AiImageMetadata.InvalidMetadata;
            }

            return FindGeneratedManifest(manifests, activeManifest, []) ?? AiImageMetadata.NotDetected;
        }
        catch (JsonException)
        {
            return AiImageMetadata.InvalidMetadata;
        }
    }

    private static bool HasUsableValidationState(JsonElement root) =>
        root.TryGetProperty("validation_state", out var state)
        && state.GetString() is "Valid" or "Trusted";

    private static AiImageMetadata? FindGeneratedManifest(
        JsonElement manifests,
        string manifestLabel,
        HashSet<string> visited)
    {
        if (!visited.Add(manifestLabel) || !manifests.TryGetProperty(manifestLabel, out var manifest))
            return null;

        var generated = FindGeneratedAction(manifest);
        if (generated is not null) return generated;

        if (!manifest.TryGetProperty("ingredients", out var ingredients)
            || ingredients.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var ingredient in ingredients.EnumerateArray())
        {
            if (!ingredient.TryGetProperty("relationship", out var relationship)
                || relationship.GetString() != "parentOf")
            {
                continue;
            }

            if (HasAiSourceType(ingredient))
                return AiImageMetadata.Generated(GetGeneratorName(manifest));

            if (ingredient.TryGetProperty("active_manifest", out var parentLabel)
                && parentLabel.GetString() is { Length: > 0 } parent
                && FindGeneratedManifest(manifests, parent, visited) is { } inherited)
            {
                return inherited;
            }
        }

        return null;
    }

    private static AiImageMetadata? FindGeneratedAction(JsonElement manifest)
    {
        if (!manifest.TryGetProperty("assertions", out var assertions)
            || assertions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var assertion in assertions.EnumerateArray())
        {
            if (!assertion.TryGetProperty("label", out var label)
                || label.GetString() is not { } labelValue
                || !labelValue.StartsWith("c2pa.actions", StringComparison.Ordinal)
                || !assertion.TryGetProperty("data", out var data)
                || !data.TryGetProperty("actions", out var actions))
            {
                continue;
            }

            foreach (var action in actions.EnumerateArray())
            {
                if (action.TryGetProperty("action", out var actionType)
                    && actionType.GetString() == "c2pa.created"
                    && HasAiSourceType(action))
                {
                    return AiImageMetadata.Generated(GetSoftwareAgentName(action) ?? GetGeneratorName(manifest));
                }
            }
        }

        return null;
    }

    private static bool HasAiSourceType(JsonElement element) =>
        element.TryGetProperty("digitalSourceType", out var sourceType)
        && sourceType.GetString() == TrainedAlgorithmicMedia;

    private static string? GetSoftwareAgentName(JsonElement action)
    {
        if (!action.TryGetProperty("softwareAgent", out var agent)) return null;

        return agent.ValueKind switch
        {
            JsonValueKind.String => agent.GetString(),
            JsonValueKind.Object when agent.TryGetProperty("name", out var name) => name.GetString(),
            _ => null,
        };
    }

    private static string? GetGeneratorName(JsonElement manifest)
    {
        if (!manifest.TryGetProperty("claim_generator_info", out var generators)
            || generators.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var generator in generators.EnumerateArray())
        {
            if (generator.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } value)
                return value;
        }

        return null;
    }
}
