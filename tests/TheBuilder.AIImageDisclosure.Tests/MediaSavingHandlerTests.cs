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
    public void InspectsImageWhenFileChanges()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: true);
        processor.Inspect(media).Returns(AiImageMetadata.NotDetected);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        handler.Handle(new MediaSavingNotification([media], new EventMessages()));

        processor.Received(1).Inspect(media);
    }

    [Fact]
    public void PreservesManualChoiceWhenOnlyMetadataChanges()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: true, fileDirty: false);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        handler.Handle(new MediaSavingNotification([media], new EventMessages()));

        processor.DidNotReceive().Inspect(Arg.Any<IMedia>());
        media.Received(1).SetValue(
            Constants.AiDisclosureSourcePropertyAlias,
            Constants.ManualDisclosureSourceValue);
    }

    [Fact]
    public void IgnoresNonImageMedia()
    {
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var media = CreateMedia(isImage: false, fileDirty: true);
        var handler = new AiImageDisclosureMediaSavingHandler(
            processor,
            Substitute.For<ILogger<AiImageDisclosureMediaSavingHandler>>());

        handler.Handle(new MediaSavingNotification([media], new EventMessages()));

        processor.DidNotReceive().Inspect(Arg.Any<IMedia>());
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
