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
    private const string MetadataAssertion = "c2pa.metadata";

    public static AiImageMetadata Parse(string json)
    {
        try
        {
            if (json is null) return AiImageMetadata.InvalidMetadata;
            if (json.Length > MaximumManifestJsonCharacters) return AiImageMetadata.ManifestLimitExceeded;

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!HasUsableValidationState(root)
                || !root.TryGetProperty("active_manifest", out var activeManifestElement)
                || activeManifestElement.GetString() is not { Length: > 0 } activeManifest
                || !root.TryGetProperty("manifests", out var manifests)
                || manifests.ValueKind != JsonValueKind.Object
                || !manifests.TryGetProperty(activeManifest, out _))
            {
                return AiImageMetadata.InvalidMetadata;
            }

            if (HasTooManyManifests(manifests)) return AiImageMetadata.ManifestLimitExceeded;

            return FindDisclosure(manifests, activeManifest) ?? AiImageMetadata.NotDetected;
        }
        catch (C2paScanLimitException)
        {
            return AiImageMetadata.ManifestLimitExceeded;
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
        var directIngredientFallbacks = new List<DisclosureCandidate>();
        DisclosureCandidate? best = null;
        foreach (var evidence in C2paManifestGraph.Walk(manifests, activeManifest))
        {
            if (evidence.IsIngredient)
            {
                if (GetSourceType(evidence.Value) is not { } sourceType) continue;
                var candidate = CreateCandidate(sourceType, null);
                if (evidence.IsComponent)
                    best = PreferStronger(best, AsComponent(candidate));
                else
                    directIngredientFallbacks.Add(candidate);
                continue;
            }

            var detected = PreferStronger(
                FindDisclosureAction(evidence.Value), FindDisclosureMetadata(evidence.Value));
            best = PreferStronger(best, evidence.IsComponent ? AsComponent(detected) : detected);
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
                || !C2paManifestGraph.IsActionsAssertion(labelValue)
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

                var generator = GetSoftwareAgentName(action);
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

    private static DisclosureCandidate? FindDisclosureMetadata(JsonElement manifest)
    {
        if (!manifest.TryGetProperty("assertions", out var assertions)
            || assertions.ValueKind != JsonValueKind.Array) return null;
        DisclosureCandidate? best = null;
        foreach (var assertion in assertions.EnumerateArray())
        {
            if (!assertion.TryGetProperty("label", out var label)
                || label.GetString() is not (MetadataAssertion or "stds.iptc" or "stds.iptc.photometadata")
                || !assertion.TryGetProperty("data", out var data)) continue;
            if (GetMetadataSourceType(data) is { } sourceType)
                best = PreferStronger(best, CreateCandidate(sourceType, null));
        }
        return best;
    }

    private static DisclosureCandidate? AsComponent(DisclosureCandidate? candidate) =>
        candidate is null ? null : new DisclosureCandidate(
            DisclosureStrength.Composite, AiImageMetadata.Modified(candidate.Metadata.Generator));

    private static AiSourceType? GetSourceType(JsonElement element) =>
        element.TryGetProperty("digitalSourceType", out var sourceType) ? ParseSourceType(sourceType) : null;

    private static AiSourceType? GetMetadataSourceType(JsonElement data)
    {
        const string iptcNamespace = "http://iptc.org/std/Iptc4xmpExt/2008-02-29/";
        foreach (var property in data.EnumerateObject())
        {
            var separator = property.Name.IndexOf(':');
            if (separator < 0 || property.Name[(separator + 1)..] != "DigitalSourceType") continue;
            var prefix = property.Name[..separator];
            if (data.TryGetProperty("@context", out var context)
                && context.ValueKind == JsonValueKind.Object
                && context.TryGetProperty(prefix, out var binding))
            {
                if (binding.ValueKind != JsonValueKind.String || binding.GetString() != iptcNamespace) continue;
            }
            else if (prefix != "Iptc4xmpExt") continue;
            if (ParseSourceType(property.Value) is { } sourceType) return sourceType;
        }
        return null;
    }

    private static AiSourceType? ParseSourceType(JsonElement value) => value.GetString() switch
    {
        TrainedAlgorithmicMedia => AiSourceType.Generated,
        CompositeWithTrainedAlgorithmicMedia or CompositedWithTrainedAlgorithmicMedia => AiSourceType.Modified,
        _ => null,
    };

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
