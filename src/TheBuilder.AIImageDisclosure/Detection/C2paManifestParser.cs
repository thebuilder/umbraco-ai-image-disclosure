using System.Text.Json;

namespace TheBuilder.AIImageDisclosure.Detection;

internal static class C2paManifestParser
{
    internal const int MaximumManifestJsonCharacters = 4 * 1024 * 1024;
    internal const int MaximumManifestCount = 1024;
    private const string TrainedAlgorithmicMedia =
        "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia";
    private const string CompositeWithTrainedAlgorithmicMedia =
        "http://cv.iptc.org/newscodes/digitalsourcetype/compositeWithTrainedAlgorithmicMedia";
    private const string CompositedWithTrainedAlgorithmicMedia =
        "http://cv.iptc.org/newscodes/digitalsourcetype/compositedWithTrainedAlgorithmicMedia";

    public static AiImageMetadata Parse(string json)
    {
        try
        {
            if (json is null || json.Length > MaximumManifestJsonCharacters)
                return AiImageMetadata.InvalidMetadata;

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!HasUsableValidationState(root)
                || !root.TryGetProperty("active_manifest", out var activeManifestElement)
                || activeManifestElement.GetString() is not { Length: > 0 } activeManifest
                || !root.TryGetProperty("manifests", out var manifests)
                || manifests.ValueKind != JsonValueKind.Object
                || HasTooManyManifests(manifests))
            {
                return AiImageMetadata.InvalidMetadata;
            }

            return FindDisclosure(manifests, activeManifest) ?? AiImageMetadata.NotDetected;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return AiImageMetadata.InvalidMetadata;
        }
    }

    private static bool HasUsableValidationState(JsonElement root) =>
        root.TryGetProperty("validation_state", out var state)
        && state.GetString() is "Valid" or "Trusted";

    private static bool HasTooManyManifests(JsonElement manifests)
    {
        var count = 0;
        foreach (var _ in manifests.EnumerateObject())
        {
            if (++count > MaximumManifestCount) return true;
        }

        return false;
    }

    private static AiImageMetadata? FindDisclosure(
        JsonElement manifests,
        string activeManifest)
    {
        var pending = new Stack<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var directIngredientFallbacks = new List<DisclosureCandidate>();
        DisclosureCandidate? best = null;
        pending.Push(activeManifest);

        while (pending.TryPop(out var manifestLabel))
        {
            if (!visited.Add(manifestLabel) || !manifests.TryGetProperty(manifestLabel, out var manifest))
            {
                continue;
            }

            best = PreferStronger(best, FindDisclosureAction(manifest));

            if (!manifest.TryGetProperty("ingredients", out var ingredients)
                || ingredients.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var ingredient in ingredients.EnumerateArray())
            {
                if (!ingredient.TryGetProperty("relationship", out var relationship)
                    || relationship.GetString() != "parentOf")
                {
                    continue;
                }

                if (ingredient.TryGetProperty("active_manifest", out var parentLabel)
                    && parentLabel.GetString() is { Length: > 0 } parent)
                {
                    pending.Push(parent);
                }

                if (GetSourceType(ingredient) is { } sourceType)
                    directIngredientFallbacks.Add(CreateCandidate(sourceType, null));
            }
        }

        if (best is null)
        {
            foreach (var fallback in directIngredientFallbacks)
                best = PreferStronger(best, fallback);
        }

        return best?.Metadata;
    }

    private static DisclosureCandidate? FindDisclosureAction(JsonElement manifest)
    {
        if (!manifest.TryGetProperty("assertions", out var assertions)
            || assertions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        DisclosureCandidate? best = null;
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
                if (!action.TryGetProperty("action", out var actionType)
                    || actionType.GetString() is not { } actionName
                    || GetSourceType(action) is not { } sourceType)
                {
                    continue;
                }

                var generator = GetSoftwareAgentName(action) ?? GetGeneratorName(manifest);
                if (actionName is not ("c2pa.created" or "c2pa.edited"))
                {
                    continue;
                }

                if (sourceType == AiSourceType.Modified)
                {
                    best = PreferStronger(best, CreateCandidate(sourceType, generator));
                }
                else if (actionName == "c2pa.created")
                {
                    best = PreferStronger(
                        best,
                        new DisclosureCandidate(DisclosureStrength.Generated, AiImageMetadata.Generated(generator)));
                }
                else
                {
                    best = PreferStronger(
                        best,
                        new DisclosureCandidate(DisclosureStrength.Edited, AiImageMetadata.Modified(generator)));
                }
            }
        }

        return best;
    }

    private static AiSourceType? GetSourceType(JsonElement element)
    {
        if (!element.TryGetProperty("digitalSourceType", out var sourceType)) return null;

        return sourceType.GetString() switch
        {
            TrainedAlgorithmicMedia => AiSourceType.Generated,
            CompositeWithTrainedAlgorithmicMedia or CompositedWithTrainedAlgorithmicMedia => AiSourceType.Modified,
            _ => null,
        };
    }

    private static DisclosureCandidate CreateCandidate(AiSourceType sourceType, string? generator) =>
        sourceType == AiSourceType.Generated
            ? new DisclosureCandidate(DisclosureStrength.Generated, AiImageMetadata.Generated(generator))
            : new DisclosureCandidate(DisclosureStrength.Composite, AiImageMetadata.Modified(generator));

    private static DisclosureCandidate? PreferStronger(
        DisclosureCandidate? current,
        DisclosureCandidate? candidate) =>
        candidate is not null && (current is null || candidate.Strength > current.Strength)
            ? candidate
            : current;

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

    private enum AiSourceType
    {
        Generated,
        Modified,
    }

    private enum DisclosureStrength
    {
        Edited,
        Generated,
        Composite,
    }

    private sealed record DisclosureCandidate(DisclosureStrength Strength, AiImageMetadata Metadata);
}
