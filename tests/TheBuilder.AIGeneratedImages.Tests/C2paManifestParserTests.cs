using TheBuilder.AIGeneratedImages.Detection;

namespace TheBuilder.AIGeneratedImages.Tests;

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

    [Fact]
    public void DoesNotClassifyAiEditedImageAsFullyGenerated()
    {
        var result = C2paManifestParser.Parse(ManifestWithAction(
            "c2pa.edited",
            "http://cv.iptc.org/newscodes/digitalsourcetype/trainedAlgorithmicMedia"));

        Assert.Equal(AiImageDetectionStatus.NotDetected, result.Status);
    }

    [Fact]
    public void FollowsParentIngredientToAiGeneratedSource()
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
        Assert.Equal("OpenAI Media Service API", result.Generator);
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
}
