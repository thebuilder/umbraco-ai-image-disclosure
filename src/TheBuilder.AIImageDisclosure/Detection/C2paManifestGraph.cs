using System.Text.Json;

namespace TheBuilder.AIImageDisclosure.Detection;

// Reader JSON separates ingredient assertions from other assertions. Match action URIs
// against their owning manifest and ingredient label, never a label in an unrelated manifest.
internal static class C2paManifestGraph
{
    internal readonly record struct Evidence(JsonElement Value, bool IsIngredient, bool IsComponent);
    private readonly record struct AssertionId(string Manifest, string Label);
    private sealed record Visit(string Label, bool IsComponent, HashSet<AssertionId> Removed, HashSet<string> Ancestors);

    public static IEnumerable<Evidence> Walk(JsonElement manifests, string activeManifest)
    {
        var pending = new Stack<Visit>();
        pending.Push(new Visit(activeManifest, false, [], []));
        var visits = 0;
        while (pending.TryPop(out var visit))
        {
            if (visit.Ancestors.Contains(visit.Label) || !manifests.TryGetProperty(visit.Label, out var manifest)) continue;
            if (++visits > C2paManifestParser.MaximumManifestCount)
                throw new C2paScanLimitException();
            yield return new Evidence(manifest, false, visit.IsComponent);
            var ancestors = new HashSet<string>(visit.Ancestors, StringComparer.Ordinal) { visit.Label };
            var removed = new HashSet<AssertionId>(visit.Removed);
            var placed = new HashSet<AssertionId>();
            foreach (var action in Actions(manifest))
            {
                var name = action.TryGetProperty("action", out var actionName) ? actionName.GetString() : null;
                if (name is not ("c2pa.placed" or "c2pa.removed")) continue;
                foreach (var reference in References(action))
                {
                    if (Resolve(reference, visit.Label) is not { } id) continue;
                    if (name == "c2pa.removed") removed.Add(id);
                    else placed.Add(id);
                }
            }
            if (!manifest.TryGetProperty("ingredients", out var ingredients)) continue;
            foreach (var ingredient in ingredients.EnumerateArray())
            {
                var relationship = ingredient.TryGetProperty("relationship", out var relation) ? relation.GetString() : null;
                if (relationship is not ("parentOf" or "componentOf")) continue;
                var component = visit.IsComponent || relationship == "componentOf";
                if (relationship == "componentOf")
                {
                    if (!ingredient.TryGetProperty("label", out var label)) continue;
                    var id = new AssertionId(visit.Label, label.GetString()!);
                    if (removed.Contains(id) || !placed.Contains(id)) continue;
                }
                yield return new Evidence(ingredient, true, component);
                if (ingredient.TryGetProperty("active_manifest", out var parent)
                    && parent.GetString() is { Length: > 0 } parentLabel)
                    pending.Push(new Visit(parentLabel, component, removed, ancestors));
            }
        }
    }

    public static bool IsActionsAssertion(string label) =>
        label is "c2pa.actions" or "c2pa.actions.v2"
        || HasInstanceSuffix(label, "c2pa.actions__") || HasInstanceSuffix(label, "c2pa.actions.v2__");

    private static bool HasInstanceSuffix(string label, string prefix) =>
        label.StartsWith(prefix, StringComparison.Ordinal)
        && int.TryParse(label.AsSpan(prefix.Length), out var instance) && instance > 0;

    private static IEnumerable<JsonElement> Actions(JsonElement manifest)
    {
        if (!manifest.TryGetProperty("assertions", out var assertions)) yield break;
        foreach (var assertion in assertions.EnumerateArray())
        {
            if (assertion.TryGetProperty("label", out var label) && label.GetString() is { } name
                && IsActionsAssertion(name) && assertion.TryGetProperty("data", out var data)
                && data.TryGetProperty("actions", out var actions))
                foreach (var action in actions.EnumerateArray()) yield return action;
        }
    }

    private static IEnumerable<JsonElement> References(JsonElement action)
    {
        if (!action.TryGetProperty("parameters", out var parameters)) yield break;
        if (parameters.TryGetProperty("ingredients", out var references))
            foreach (var reference in references.EnumerateArray()) yield return reference;
        else if (parameters.TryGetProperty("ingredient", out var reference))
            yield return reference;
    }

    private static AssertionId? Resolve(JsonElement reference, string manifest)
    {
        var uri = reference.ValueKind == JsonValueKind.String ? reference.GetString()
            : reference.TryGetProperty("url", out var url) ? url.GetString() : null;
        const string absolutePrefix = "self#jumbf=/c2pa/";
        const string localPrefix = "self#jumbf=c2pa.assertions/";
        if (uri is null) return null;
        if (uri.StartsWith(localPrefix, StringComparison.Ordinal))
            return new AssertionId(manifest, uri[localPrefix.Length..]);
        if (!uri.StartsWith(absolutePrefix, StringComparison.Ordinal)) return null;
        var path = uri[absolutePrefix.Length..].Split('/');
        return path.Length == 3 && path[1] == "c2pa.assertions"
            ? new AssertionId(path[0], path[2]) : null;
    }
}

internal sealed class C2paScanLimitException : Exception;
