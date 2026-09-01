using Microsoft.Extensions.Logging;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Detection;
using TheBuilder.AIImageDisclosure.Media;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class MediaAiMetadataProcessorTests
{
    [Fact]
    public void ClearsAutomaticMetadataWhenMediaPathIsUnavailable()
    {
        var media = Substitute.For<IMedia>();
        var processor = new MediaAiMetadataProcessor(
            Substitute.For<IImageAiMetadataReader>(),
            Substitute.For<IMediaService>(),
            new MediaUrlGeneratorCollection(() => []),
            Substitute.For<ILogger<MediaAiMetadataProcessor>>());

        var result = processor.Inspect(media);

        Assert.Equal(AiImageDetectionStatus.InvalidMetadata, result.Status);
        media.Received(1).SetValue(Constants.AiDisclosurePropertyAlias, string.Empty);
        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
        media.Received(1).SetValue(Constants.AiDisclosureSourcePropertyAlias, string.Empty);
    }

    [Fact]
    public void AppliesGeneratedMetadata()
    {
        var media = Substitute.For<IMedia>();

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.Generated("gpt-image"));

        media.Received(1).SetValue(Constants.AiDisclosurePropertyAlias, Constants.GeneratedDisclosureValue);
        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, "gpt-image");
        media.Received(1).SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.C2paDisclosureSourceValue);
    }

    [Fact]
    public void AppliesModifiedMetadata()
    {
        var media = Substitute.For<IMedia>();

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.Modified("Adobe Firefly"));

        media.Received(1).SetValue(Constants.AiDisclosurePropertyAlias, Constants.ModifiedDisclosureValue);
        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, "Adobe Firefly");
        media.Received(1).SetValue(Constants.AiDisclosureSourcePropertyAlias, Constants.C2paDisclosureSourceValue);
    }

    [Fact]
    public void ClearsAutomaticMetadataWhenNothingIsDetected()
    {
        var media = Substitute.For<IMedia>();

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.NotDetected);

        media.Received(1).SetValue(Constants.AiDisclosurePropertyAlias, string.Empty);
        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
        media.Received(1).SetValue(Constants.AiDisclosureSourcePropertyAlias, string.Empty);
    }

    [Fact]
    public void ClearsAutomaticMetadataWhenC2paIsInvalid()
    {
        var media = Substitute.For<IMedia>();

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.InvalidMetadata);

        media.Received(1).SetValue(Constants.AiDisclosurePropertyAlias, string.Empty);
        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
        media.Received(1).SetValue(Constants.AiDisclosureSourcePropertyAlias, string.Empty);
    }

    [Fact]
    public void PreservesManualMetadataWhenNothingIsDetected()
    {
        var media = Substitute.For<IMedia>();
        media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias).Returns(true);
        media.IsPropertyDirty(Constants.AiGeneratorPropertyAlias).Returns(true);

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.NotDetected);

        media.Received(1).SetValue(
            Constants.AiDisclosureSourcePropertyAlias,
            Constants.ManualDisclosureSourceValue);
        media.DidNotReceive().SetValue(Constants.AiDisclosurePropertyAlias, Arg.Any<object?>());
        media.DidNotReceive().SetValue(Constants.AiGeneratorPropertyAlias, Arg.Any<object?>());
    }

    [Fact]
    public void PreservesPreviouslySavedManualMetadataWhenFileChanges()
    {
        var media = Substitute.For<IMedia>();
        media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias)
            .Returns(Constants.ManualDisclosureSourceValue);

        MediaAiMetadataProcessor.Apply(media, AiImageMetadata.NotDetected);

        media.Received(1).SetValue(
            Constants.AiDisclosureSourcePropertyAlias,
            Constants.ManualDisclosureSourceValue);
        media.DidNotReceive().SetValue(Constants.AiDisclosurePropertyAlias, Arg.Any<object?>());
        media.DidNotReceive().SetValue(Constants.AiGeneratorPropertyAlias, Arg.Any<object?>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PreservesPreviouslySavedManualMetadataForLaterInspection(int statusValue)
    {
        var media = Substitute.For<IMedia>();
        media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias)
            .Returns(Constants.ManualDisclosureSourceValue);
        var result = (AiImageDetectionStatus)statusValue switch
        {
            AiImageDetectionStatus.Generated => AiImageMetadata.Generated("gpt-image"),
            AiImageDetectionStatus.Modified => AiImageMetadata.Modified("Firefly"),
            _ => AiImageMetadata.InvalidMetadata,
        };

        MediaAiMetadataProcessor.Apply(media, result);

        media.Received(1).SetValue(
            Constants.AiDisclosureSourcePropertyAlias,
            Constants.ManualDisclosureSourceValue);
        media.DidNotReceive().SetValue(Constants.AiDisclosurePropertyAlias, Arg.Any<object?>());
        media.DidNotReceive().SetValue(Constants.AiGeneratorPropertyAlias, Arg.Any<object?>());
    }
}
