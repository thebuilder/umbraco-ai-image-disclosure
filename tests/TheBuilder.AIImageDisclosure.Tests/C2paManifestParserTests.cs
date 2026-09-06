using TheBuilder.AIImageDisclosure.Detection;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class C2paManifestParserTests
{
    [Fact]
    public void DetectsAiGeneratedImageAndSoftwareAgent()
    {
        var result = C2paManifestParser.Parse(ManifestWithAction(
            "c2pa.created",
            "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"));

        Assert.Equal(AiImageDetectionStatus.Generated, result.Status);
        Assert.Equal("gpt-image", result.Generator);
    }

    [Theory]
    [InlineData("c2pa.metadata", "Iptc4xmpExt:DigitalSourceType")]
    [InlineData("stds.iptc", "Iptc4xmpExt:DigitalSourceType")]
    [InlineData("stds.iptc.photometadata", "Iptc4xmpExt:DigitalSourceType")]
    public void DetectsAiSourceFromMetadataAssertions(string label, string sourceProperty)
    {
        var json = $$"""
            { "active_manifest": "active", "manifests": { "active": {
              "assertions": [{ "label": "{{label}}", "data": { "{{sourceProperty}}": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia" } }]
            } }, "validation_state": "Valid" }
            """;

        Assert.Equal(AiImageDetectionStatus.Generated, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void InputToAndUnrelatedHistoryDoNotMarkImageAsAi()
    {
        const string json = """
            { "active_manifest": "active", "manifests": {
              "active": { "ingredients": [{ "relationship": "inputTo", "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia" }] },
              "other": { "assertions": [{ "label": "c2pa.ingredient.v3", "data": { "relationship": "inputTo", "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia" } }] }
            }, "validation_state": "Valid" }
            """;

        Assert.Equal(AiImageDetectionStatus.NotDetected, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void ClassifiesAiEditedImageAsPartiallyModified()
    {
        var result = C2paManifestParser.Parse(ManifestWithAction(
            "c2pa.edited",
            "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"));

        Assert.Equal(AiImageDetectionStatus.Modified, result.Status);
        Assert.Equal("gpt-image", result.Generator);
    }

    [Theory]
    [InlineData("http://cv.iptc.org/newscodes/digitalsourcetype/compositeWithTrainedAlgorithmicMedia")]
    [InlineData("http://cv.iptc.org/newscodes/digitalsourcetype/compositedWithTrainedAlgorithmicMedia")]
    public void ClassifiesCompositeAiSourceAsPartiallyModified(string sourceType)
    {
        var result = C2paManifestParser.Parse(ManifestWithAction("c2pa.created", sourceType));

        Assert.Equal(AiImageDetectionStatus.Modified, result.Status);
    }

    [Fact]
    public void DoesNotTreatClaimGeneratorAsAiGenerator()
    {
        const string json = """
            {
              "active_manifest": "edited",
              "manifests": {
                "edited": {
                  "ingredients": [{ "relationship": "parentOf", "active_manifest": "source" }]
                },
                "source": {
                  "claim_generator_info": [{ "name": "OpenAI Media Service API" }],
                  "assertions": [{
                    "label": "c2pa.actions.v2",
                    "data": { "actions": [{
                      "action": "c2pa.created",
                      "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"
                    }]}
                  }]
                }
              },
              "validation_state": "Trusted"
            }
            """;

        var result = C2paManifestParser.Parse(json);

        Assert.Equal(AiImageDetectionStatus.Generated, result.Status);
        Assert.Null(result.Generator);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Unknown")]
    public void RejectsUnusableValidationState(string state)
    {
        var json = ManifestWithAction(
            "c2pa.created",
            "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia")
            .Replace("\"Valid\"", $"\"{state}\"", StringComparison.Ordinal);

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void RejectsMalformedJson() =>
        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, C2paManifestParser.Parse("{").Status);

    [Fact]
    public void RejectsValidJsonWithMalformedManifestShapes()
    {
        const string json = """
            { "active_manifest": "active", "manifests": [], "validation_state": "Valid" }
            """;

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void StopsFollowingCyclicIngredientChains()
    {
        const string json = """
            {
              "active_manifest": "first",
              "manifests": {
                "first": { "ingredients": [{ "relationship": "parentOf", "active_manifest": "second" }] },
                "second": { "ingredients": [{ "relationship": "parentOf", "active_manifest": "first" }] }
              },
              "validation_state": "Valid"
            }
            """;

        Assert.Equal(AiImageDetectionStatus.NotDetected, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void RejectsManifestJsonAboveResourceLimit()
    {
        var json = new string(' ', C2paManifestParser.MaximumManifestJsonCharacters + 1);

        Assert.Equal(AiImageDetectionReason.ManifestLimitExceeded, C2paManifestParser.Parse(json).Reason);
    }

    [Fact]
    public void RejectsManifestGraphsAboveResourceLimit()
    {
        var manifests = string.Join(
            ',',
            Enumerable.Range(0, C2paManifestParser.MaximumManifestCount + 1).Select(index => $"\"m{index}\":{{}}"));
        var json = $$"""
            {
              "active_manifest": "m0",
              "manifests": { {{manifests}} },
              "validation_state": "Valid"
            }
            """;

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void KeepsFullyGeneratedWhenAiEditingFollowsAiCreation()
    {
        var json = ManifestWithActions(
            ("c2pa.created", "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"),
            ("c2pa.edited", "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"));

        Assert.Equal(AiImageDetectionStatus.Generated, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void CompositeEvidenceTakesPrecedenceOverAiCreation()
    {
        var json = ManifestWithActions(
            ("c2pa.created", "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"),
            ("c2pa.edited", "http://cv.iptc.org/newscodes/digitalsourcetype/compositeWithTrainedAlgorithmicMedia"));

        Assert.Equal(AiImageDetectionStatus.Modified, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void CompositeEvidenceInParentTakesPrecedenceOverActiveManifestCreation()
    {
        const string json = """
            {
              "active_manifest": "active",
              "manifests": {
                "active": {
                  "assertions": [{
                    "label": "c2pa.actions.v2",
                    "data": { "actions": [{
                      "action": "c2pa.created",
                      "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"
                    }]}
                  }],
                  "ingredients": [{ "relationship": "parentOf", "active_manifest": "parent" }]
                },
                "parent": {
                  "assertions": [{
                    "label": "c2pa.actions.v2",
                    "data": { "actions": [{
                      "action": "c2pa.edited",
                      "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/compositeWithTrainedAlgorithmicMedia"
                    }]}
                  }]
                }
              },
              "validation_state": "Valid"
            }
            """;

        Assert.Equal(AiImageDetectionStatus.Modified, C2paManifestParser.Parse(json).Status);
    }

    [Fact]
    public void UsesDirectIngredientDisclosureWhenReferencedManifestIsUnavailable()
    {
        const string json = """
            {
              "active_manifest": "active",
              "manifests": {
                "active": {
                  "ingredients": [{
                    "relationship": "parentOf",
                    "active_manifest": "missing",
                    "digitalSourceType": "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"
                  }]
                }
              },
              "validation_state": "Valid"
            }
            """;

        Assert.Equal(AiImageDetectionStatus.Generated, C2paManifestParser.Parse(json).Status);
    }

    private static string ManifestWithAction(string action, string digitalSourceType) => $$"""
        {
          "active_manifest": "active",
          "manifests": {
            "active": {
              "claim_generator_info": [{ "name": "OpenAI Media Service API" }],
              "assertions": [{
                "label": "c2pa.actions.v2",
                "data": { "actions": [{
                  "action": "{{action}}",
                  "softwareAgent": { "name": "gpt-image", "version": "2.0" },
                  "digitalSourceType": "{{digitalSourceType}}"
                }]}
              }]
            }
          },
          "validation_state": "Valid"
        }
        """;

    private static string ManifestWithActions(params (string Action, string SourceType)[] actions)
    {
        var actionJson = string.Join(
            ',',
            actions.Select(item => $$"""
                { "action": "{{item.Action}}", "digitalSourceType": "{{item.SourceType}}" }
                """));
        return $$"""
            {
              "active_manifest": "active",
              "manifests": {
                "active": {
                  "assertions": [{
                    "label": "c2pa.actions.v2",
                    "data": { "actions": [{{actionJson}}] }
                  }]
                }
              },
              "validation_state": "Valid"
            }
            """;
    }
}
