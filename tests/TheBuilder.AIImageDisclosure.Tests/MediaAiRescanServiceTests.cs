using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Detection;
using TheBuilder.AIImageDisclosure.Media;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class MediaAiRescanServiceTests
{
    [Fact]
    public void PagesThroughTheWholeLibraryWithBoundedStableOrdering()
    {
        var entities = Substitute.For<IEntityService>();
        var media = Substitute.For<IMediaService>();
        var ids = Enumerable.Range(1, 105).Select(Entity).ToArray();
        entities.GetPagedDescendants(-1, UmbracoObjectTypes.Media, Arg.Any<long>(), 50,
            out Arg.Any<long>(), null, Arg.Any<Ordering>()).Returns(call => {
                call[4] = 105L;
                Assert.Equal("Id", call.Arg<Ordering>().OrderBy);
                return ids.Skip(checked((int)call.ArgAt<long>(2)) * 50).Take(50);
            });
        var service = new MediaAiRescanService(entities, media, NullLogger<MediaAiRescanService>.Instance);
        var first = service.Scan(0, 999, 7, TestContext.Current.CancellationToken);
        var second = service.Scan(first.NextPageIndex, 999, 7, TestContext.Current.CancellationToken);
        var last = service.Scan(second.NextPageIndex, 999, 7, TestContext.Current.CancellationToken);
        Assert.False(first.Done);
        Assert.False(second.Done);
        Assert.True(last.Done);
        Assert.Equal(105, last.TotalItems);
        Assert.Equal(105, media.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "GetById"));
    }

    [Fact]
    public void ScanUsesTheSaveNotificationOnceAndPreservesManualChoices()
    {
        var entities = Substitute.For<IEntityService>();
        var mediaService = Substitute.For<IMediaService>();
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var automatic = Media("C2PA", out var automaticValues);
        var manual = Media("Manual", out var manualValues);
        var folder = Media("", out _, "Folder");
        var trashed = Media("", out _); trashed.Trashed.Returns(true);
        entities.GetPagedDescendants(-1, UmbracoObjectTypes.Media, 0, 50, out Arg.Any<long>(), null, Arg.Any<Ordering>())
            .Returns(call => { call[4] = 4L; return new[] { Entity(1), Entity(2), Entity(3), Entity(4) }; });
        mediaService.GetById(1).Returns(automatic);
        mediaService.GetById(2).Returns(manual);
        mediaService.GetById(3).Returns(folder);
        mediaService.GetById(4).Returns(trashed);
        processor.ResumeAutomaticDetectionAsync(automatic, Arg.Any<CancellationToken>()).Returns(_ => {
            MediaAiMetadataProcessor.ApplyAutomatic(automatic, AiImageMetadata.Modified("Test model"));
            return AiImageMetadata.Modified("Test model");
        });
        var handler = new AiImageDisclosureMediaSavingHandler(processor, NullLogger<AiImageDisclosureMediaSavingHandler>.Instance);
        mediaService.Save(automatic, 7).Returns(_ => {
            handler.HandleAsync(new MediaSavingNotification([automatic], new EventMessages()), CancellationToken.None).GetAwaiter().GetResult();
            return OperationResult.Attempt.Succeed(new EventMessages());
        });
        var service = new MediaAiRescanService(entities, mediaService, NullLogger<MediaAiRescanService>.Instance);
        var result = service.Scan(0, 50, 7, TestContext.Current.CancellationToken);
        Assert.True(result.Scanned == 1, result.ToString());
        Assert.Equal(1, result.SkippedManual);
        Assert.Equal(0, result.Failed);
        Assert.Equal("C2PA", automaticValues[Constants.AiDisclosureSourcePropertyAlias]);
        Assert.Equal("modified", automaticValues[Constants.AiDisclosurePropertyAlias]);
        Assert.Equal("Manual", manualValues[Constants.AiDisclosureSourcePropertyAlias]);
        processor.Received(1).ResumeAutomaticDetectionAsync(automatic, Arg.Any<CancellationToken>());
        processor.DidNotReceive().ResumeAutomaticDetectionAsync(manual, Arg.Any<CancellationToken>());
        mediaService.DidNotReceive().Save(manual, Arg.Any<int>());
    }

    [Fact]
    public void CancelledSaveIsReportedAsFailure()
    {
        var entities = Substitute.For<IEntityService>();
        var mediaService = Substitute.For<IMediaService>();
        var image = Media("", out _);
        var page = new[] { Entity(1) };
        entities.GetPagedDescendants(-1, UmbracoObjectTypes.Media, 0, 50, out Arg.Any<long>(), null, Arg.Any<Ordering>())
            .Returns(page);
        mediaService.GetById(1).Returns(image);
        mediaService.Save(image, 7).Returns(_ => default);
        var result = new MediaAiRescanService(entities, mediaService, NullLogger<MediaAiRescanService>.Instance).Scan(0, 50, 7, TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Scanned);
    }

    private static IMedia Media(string source, out Dictionary<string, string> values, string alias = "Image")
    {
        var stored = new Dictionary<string,string> { [Constants.AiDisclosureSourcePropertyAlias] = source };
        var dirty = new HashSet<string>();
        var media = Substitute.For<IMedia>();
        media.ContentType.Alias.Returns(alias);
        media.HasProperty(Arg.Any<string>()).Returns(true);
        media.GetValue<string>(Arg.Any<string>()).Returns(call => stored.GetValueOrDefault(call.ArgAt<string>(0)));
        media.IsPropertyDirty(Arg.Any<string>()).Returns(call => dirty.Contains(call.Arg<string>()));
        media.When(x => x.SetValue(Arg.Any<string>(), Arg.Any<string>())).Do(call => {
            var key = call.ArgAt<string>(0); var value = call.ArgAt<string>(1);
            if (stored.GetValueOrDefault(key) != value) dirty.Add(key);
            stored[key] = value;
        });
        values = stored;
        return media;
    }

    private static IEntitySlim Entity(int id)
    {
        var entity = Substitute.For<IEntitySlim>();
        entity.Id.Returns(id);
        return entity;
    }
}
