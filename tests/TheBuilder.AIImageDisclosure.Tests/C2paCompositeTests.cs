using System.Text.Json;
using System.Text.Json.Nodes;
using TheBuilder.AIImageDisclosure.Detection;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class C2paCompositeTests
{
    private const string AiSource = "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia";

    [Theory]
    [InlineData("signed-metadata.png", AiImageDetectionStatus.Generated)]
    [InlineData("signed-composite.png", AiImageDetectionStatus.Modified)]
    internal void ReadsSignedMetadataAndComponentsWithNativeReader(string file, AiImageDetectionStatus expected)
    {
        using var image = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", file));
        var result = new C2paImageAiMetadataReader().Read(image, "image/png");
        Assert.Equal(expected, result.Status);
    }

    [Theory]
    [InlineData(false, AiImageDetectionStatus.Modified)]
    [InlineData(true, AiImageDetectionStatus.NotDetected)]
    internal void RemovalReferencesTheComponentInItsOwningManifest(bool remove, AiImageDetectionStatus expected)
    {
        Assert.Equal(expected, C2paManifestParser.Parse(CompositeChain(remove)).Status);
    }

    [Fact]
    public void SameLabelInUnrelatedManifestCannotRemoveAComponent()
    {
        var json = CompositeChain(true).Replace("/c2pa/parent/c2pa.assertions/", "/c2pa/unrelated/c2pa.assertions/", StringComparison.Ordinal);
        Assert.Equal(AiImageDetectionStatus.Modified, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void AiIngredientUsedOnlyAsModelInputIsIgnored()
    {
        var json = CompositeChain(false).Replace("componentOf", "inputTo", StringComparison.Ordinal);
        Assert.Equal(AiImageDetectionStatus.NotDetected, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void UnplacedComponentDoesNotContributeEvidence()
    {
        var json = CompositeChain(false).Replace("c2pa.placed", "c2pa.opened", StringComparison.Ordinal);
        Assert.Equal(AiImageDetectionStatus.NotDetected, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void NestedComponentCreationIsModifiedRatherThanGenerated()
    {
        var json = JsonNode.Parse(CompositeChain(false))!;
        var ingredient = json["manifests"]!["parent"]!["ingredients"]![0]!.AsObject();
        ingredient.Remove("digitalSourceType");
        ingredient["active_manifest"] = "ai";
        Assert.Equal(AiImageDetectionStatus.Modified, C2paManifestParser.Parse(json.ToJsonString()).Status);
    }

    [Fact]
    public void MetadataNamespaceCannotBeReboundToAnUnrelatedVocabulary()
    {
        var json = JsonSerializer.Serialize(new {
            active_manifest = "active", validation_state = "Valid", manifests = new { active = new {
                assertions = new[] { new { label = "c2pa.metadata", data = new Dictionary<string, object> {
                    ["@context"] = new Dictionary<string,string> { ["Iptc4xmpExt"] = "https://unrelated.invalid/" },
                    ["Iptc4xmpExt:DigitalSourceType"] = AiSource,
                } } },
            } },
        });
        Assert.Equal(AiImageDetectionStatus.NotDetected, C2paManifestParser.Parse(json).Status);
    }

    private static string CompositeChain(bool removed)
    {
        var actions = removed ? new object[] { new { action = "c2pa.removed", parameters = new {
            ingredients = new[] { new { url = "self#jumbf=/c2pa/parent/c2pa.assertions/c2pa.ingredient.v3", hash = "validated-by-reader" } },
        } } } : [];
        return JsonSerializer.Serialize(new {
            active_manifest = "current", validation_state = "Valid", manifests = new {
                current = new {
                    ingredients = new[] { new { relationship = "parentOf", active_manifest = "parent" } },
                    assertions = new[] { new { label = "c2pa.actions.v2", data = new { actions } } },
                },
                parent = new {
                    ingredients = new[] { new { label = "c2pa.ingredient.v3", relationship = "componentOf", digitalSourceType = AiSource } },
                    assertions = new[] { new { label = "c2pa.actions.v2", data = new { actions = new[] {
                        new { action = "c2pa.placed", parameters = new { ingredients = new[] {
                            new { url = "self#jumbf=c2pa.assertions/c2pa.ingredient.v3", hash = "validated-by-reader" },
                        } } },
                    } } } },
                },
                ai = new {
                    assertions = new[] { new { label = "c2pa.actions.v2", data = new { actions = new[] {
                        new { action = "c2pa.created", digitalSourceType = AiSource },
                    } } } },
                },
            },
        });
    }
}
