using NSubstitute;
using TheBuilder.AIGeneratedImages.Detection;
using TheBuilder.AIGeneratedImages.Media;
using Umbraco.Cms.Core.Models;

namespace TheBuilder.AIGeneratedImages.Tests;

public sealed class MediaAiMetadataProcessorTests
{
    [Fact]
    public void AppliesGeneratedMetadata()
    {
        var media = Substitute.For<IMedia>();

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.Generated("gpt-image"));

        media.Received(1).SetValue(Constants.AiGeneratedPropertyAlias, true);
        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, "gpt-image");
    }

    [Fact]
    public void ClearsAutomaticMetadataWhenNothingIsDetected()
    {
        var media = Substitute.For<IMedia>();

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.NotDetected);

        media.Received(1).SetValue(Constants.AiGeneratedPropertyAlias, false);
        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
    }

    [Fact]
    public void PreservesManualMetadataWhenC2paIsInvalid()
    {
        var media = Substitute.For<IMedia>();

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.InvalidMetadata);

        media.DidNotReceive().SetValue(Arg.Any<string>(), Arg.Any<object?>());
    }
}
