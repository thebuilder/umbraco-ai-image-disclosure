using Microsoft.Extensions.Logging;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Detection;
using TheBuilder.AIImageDisclosure.Media;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class MediaSavingHandlerTests
{
    [Fact]
    public async Task InspectsImageWhenFileChanges()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: true);
        processor.InspectAsync(media, TestContext.Current.CancellationToken).Returns(AiImageMetadata.NotDetected);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        await handler.HandleAsync(new MediaSavingNotification([media], new EventMessages()), TestContext.Current.CancellationToken);

        await processor.Received(1).InspectAsync(media, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PreservesManualChoiceWhenOnlyMetadataChanges()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: false);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        await handler.HandleAsync(new MediaSavingNotification([media], new EventMessages()), TestContext.Current.CancellationToken);

        await processor.DidNotReceive().InspectAsync(Arg.Any<IMedia>(), TestContext.Current.CancellationToken);
        media.Received(1).SetValue(
            Constants.AiDisclosureSourcePropertyAlias,
            Constants.ManualDisclosureSourceValue);
    }

    [Fact]
    public async Task IgnoresNonImageMedia()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: false, fileDirty: true);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        await handler.HandleAsync(new MediaSavingNotification([media], new EventMessages()), TestContext.Current.CancellationToken);

        await processor.DidNotReceive().InspectAsync(Arg.Any<IMedia>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResumeAutomaticDetectionReprocessesCurrentFile()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: false);
        media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias).Returns(false);
        media.IsPropertyDirty(Constants.AiDisclosureSourcePropertyAlias).Returns(true);
        media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias)
            .Returns(Constants.ResumeAutomaticDisclosureSourceValue);
        processor.ResumeAutomaticDetectionAsync(media, TestContext.Current.CancellationToken).Returns(AiImageMetadata.Generated("gpt-image"));
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        await handler.HandleAsync(new MediaSavingNotification([media], new EventMessages()), TestContext.Current.CancellationToken);

        await processor.Received(1).ResumeAutomaticDetectionAsync(media, TestContext.Current.CancellationToken);
        await processor.DidNotReceive().InspectAsync(Arg.Any<IMedia>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClearingDisclosureAlsoClearsStaleGenerator()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: false);
        media.GetValue<string>(Constants.AiDisclosurePropertyAlias).Returns(string.Empty);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        await handler.HandleAsync(new MediaSavingNotification([media], new EventMessages()), TestContext.Current.CancellationToken);

        media.Received(1).SetValue(Constants.AiGeneratorPropertyAlias, string.Empty);
        media.Received(1).SetValue(
            Constants.AiDisclosureSourcePropertyAlias,
            Constants.ManualDisclosureSourceValue);
    }

    [Fact]
    public async Task GeneratorChangeDoesNotCreateManualOverride()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: false);
        media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias).Returns(false);
        media.IsPropertyDirty(Constants.AiGeneratorPropertyAlias).Returns(true);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        await handler.HandleAsync(new MediaSavingNotification([media], new EventMessages()), TestContext.Current.CancellationToken);

        media.DidNotReceive().SetValue(
            Constants.AiDisclosureSourcePropertyAlias,
            Constants.ManualDisclosureSourceValue);
    }

    [Fact]
    public async Task ResubmittingC2paSourceDoesNotOverrideAnEditorialChange()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: false);
        media.IsPropertyDirty(Constants.AiDisclosureSourcePropertyAlias).Returns(true);
        media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias).Returns("C2PA");
        var handler = new AiImageDisclosureMediaSavingHandler(processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());
        await handler.HandleAsync(new MediaSavingNotification([media], new EventMessages()), TestContext.Current.CancellationToken);
        await processor.DidNotReceive().ResumeAutomaticDetectionAsync(media, TestContext.Current.CancellationToken);
        media.Received().SetValue(Constants.AiDisclosureSourcePropertyAlias, "Manual");
    }

    private static IMedia CreateMedia(bool isImage, bool fileDirty)
    {
        var mediaType = Substitute.For<ISimpleContentType>();
        mediaType.Alias.Returns(isImage ? Constants.DefaultImageMediaTypeAlias : "File");

        var media = Substitute.For<IMedia>();
        media.ContentType.Returns(mediaType);
        media.HasProperty(Constants.AiDisclosurePropertyAlias).Returns(true);
        media.HasProperty(Constants.AiGeneratorPropertyAlias).Returns(true);
        media.HasProperty(Constants.AiDisclosureSourcePropertyAlias).Returns(true);
        media.IsPropertyDirty(Constants.SourcePropertyAlias).Returns(fileDirty);
        media.IsPropertyDirty(Constants.AiDisclosurePropertyAlias).Returns(isImage && !fileDirty);
        return media;
    }
}
