using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Detection;
using TheBuilder.AIImageDisclosure.Media;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class MediaAiRescanServiceTests
{
    [Fact]
    public void ScansTheWholeLibraryWithBoundedStableOrdering()
    {
        var fixture = new Fixture(Enumerable.Range(1, 105));
        var first = fixture.Service.Scan(null, 999, 7, TestContext.Current.CancellationToken);
        var second = fixture.Service.Scan(first.NextCursor, 999, 7, TestContext.Current.CancellationToken);
        var last = fixture.Service.Scan(second.NextCursor, 999, 7, TestContext.Current.CancellationToken);
        Assert.False(first.Done);
        Assert.False(second.Done);
        Assert.True(last.Done);
        Assert.Equal(new RescanCursor(105, 105), last.NextCursor);
        Assert.Equal(Enumerable.Range(1, 105), fixture.Visited);
    }

    [Fact]
    public void DeletingScannedMediaDoesNotSkipTheNextImage()
    {
        var fixture = new Fixture([1, 2, 3]);
        var first = fixture.Service.Scan(null, 1, 7, TestContext.Current.CancellationToken);
        fixture.Ids.Remove(1);
        var second = fixture.Service.Scan(first.NextCursor, 1, 7, TestContext.Current.CancellationToken);
        var last = fixture.Service.Scan(second.NextCursor, 1, 7, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { 1, 2, 3 }, fixture.Visited);
        Assert.True(last.Done);
    }

    [Fact]
    public void NewUploadsAreExcludedAndDeletingTheUpperBoundStillCompletes()
    {
        var fixture = new Fixture([1, 2, 3]);
        var first = fixture.Service.Scan(null, 1, 7, TestContext.Current.CancellationToken);
        fixture.Ids.Remove(3);
        fixture.Ids.Add(4);
        var second = fixture.Service.Scan(first.NextCursor, 1, 7, TestContext.Current.CancellationToken);
        var last = fixture.Service.Scan(second.NextCursor, 1, 7, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { 1, 2 }, fixture.Visited);
        Assert.Equal(3, last.NextCursor.MaximumId);
        Assert.True(last.Done);
    }

    [Fact]
    public void ChangingTheBatchLimitDoesNotChangeCursorMeaning()
    {
        var fixture = new Fixture([1, 2, 3, 4]);
        fixture.Options.MaximumBatchSize = 1;
        var first = fixture.Service.Scan(null, 50, 7, TestContext.Current.CancellationToken);
        fixture.Options.MaximumBatchSize = 2;
        var second = fixture.Service.Scan(first.NextCursor, 50, 7, TestContext.Current.CancellationToken);
        var last = fixture.Service.Scan(second.NextCursor, 50, 7, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { 1, 2, 3, 4 }, fixture.Visited);
        Assert.True(last.Done);
    }

    [Fact]
    public void EmptyLibraryCompletesImmediately()
    {
        var fixture = new Fixture([]);
        var result = fixture.Service.Scan(null, 50, 7, TestContext.Current.CancellationToken);
        Assert.True(result.Done);
        Assert.Equal(new RescanCursor(0, 0), result.NextCursor);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(11, 10)]
    public void RejectsInvalidCursors(int lastId, int maximumId)
    {
        var fixture = new Fixture([1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Service.Scan(new(lastId, maximumId), 50, 7, TestContext.Current.CancellationToken));
        Assert.Empty(fixture.Visited);
    }

    [Fact]
    public void ScanUsesTheSaveNotificationOnceAndPreservesManualChoices()
    {
        var fixture = new Fixture([1, 2, 3, 4]);
        var processor = Substitute.For<IMediaAiMetadataProcessor>();
        var automatic = Media("C2PA", out var automaticValues);
        var manual = Media("Manual", out var manualValues);
        var folder = Media("", out _, "Folder");
        var trashed = Media("", out _); trashed.Trashed.Returns(true);
        fixture.Media.GetById(1).Returns(automatic);
        fixture.Media.GetById(2).Returns(manual);
        fixture.Media.GetById(3).Returns(folder);
        fixture.Media.GetById(4).Returns(trashed);
        processor.ResumeAutomaticDetectionAsync(automatic, Arg.Any<CancellationToken>()).Returns(_ => {
            MediaAiMetadataProcessor.ApplyAutomatic(automatic, AiImageMetadata.Modified("Test model"));
            return AiImageMetadata.Modified("Test model");
        });
        var handler = new AiImageDisclosureMediaSavingHandler(processor, NullLogger<AiImageDisclosureMediaSavingHandler>.Instance);
        fixture.Media.Save(automatic, 7).Returns(_ => {
            handler.HandleAsync(new MediaSavingNotification([automatic], new EventMessages()), CancellationToken.None).GetAwaiter().GetResult();
            return OperationResult.Attempt.Succeed(new EventMessages());
        });
        var result = fixture.Service.Scan(null, 50, 7, TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.SkippedManual);
        Assert.Equal(0, result.Failed);
        Assert.Equal("C2PA", automaticValues[Constants.AiDisclosureSourcePropertyAlias]);
        Assert.Equal("modified", automaticValues[Constants.AiDisclosurePropertyAlias]);
        Assert.Equal("Manual", manualValues[Constants.AiDisclosureSourcePropertyAlias]);
        processor.Received(1).ResumeAutomaticDetectionAsync(automatic, Arg.Any<CancellationToken>());
        processor.DidNotReceive().ResumeAutomaticDetectionAsync(manual, Arg.Any<CancellationToken>());
        fixture.Media.DidNotReceive().Save(manual, Arg.Any<int>());
    }

    [Fact]
    public void CancelledSaveIsReportedAsFailure()
    {
        var fixture = new Fixture([1]);
        var image = Media("", out _);
        fixture.Media.GetById(1).Returns(image);
        fixture.Media.Save(image, 7).Returns(_ => default);
        var result = fixture.Service.Scan(null, 50, 7, TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Scanned);
    }

    private sealed class Fixture
    {
        public List<int> Ids { get; }
        public List<int> Visited { get; } = [];
        public IMediaService Media { get; } = Substitute.For<IMediaService>();
        public MediaAiRescanOptions Options { get; } = new();
        public MediaAiRescanService Service { get; }

        public Fixture(IEnumerable<int> ids)
        {
            Ids = ids.ToList();
            var entities = Substitute.For<IEntityService>();
            var scopes = Substitute.For<ICoreScopeProvider>();
            var predicates = new Dictionary<IQuery<IUmbracoEntity>, Func<IUmbracoEntity, bool>>();
            scopes.CreateQuery<IUmbracoEntity>().Returns(_ => {
                var query = Substitute.For<IQuery<IUmbracoEntity>>();
                query.Where(Arg.Any<Expression<Func<IUmbracoEntity, bool>>>()).Returns(call => {
                    predicates[query] = call.Arg<Expression<Func<IUmbracoEntity, bool>>>().Compile();
                    return query;
                });
                return query;
            });
            entities.GetPagedDescendants(-1, UmbracoObjectTypes.Media, Arg.Any<long>(), Arg.Any<int>(),
                out Arg.Any<long>(), Arg.Any<IQuery<IUmbracoEntity>>(), Arg.Any<Ordering>()).Returns(call => {
                Assert.Equal(0L, call.ArgAt<long>(2));
                var order = call.Arg<Ordering>();
                Assert.Equal("Id", order.OrderBy);
                var query = call.ArgAt<IQuery<IUmbracoEntity>?>(5);
                IEnumerable<IEntitySlim> rows = Ids.Select(Entity);
                if (query is not null) rows = rows.Where(entity => predicates[query](entity));
                var sorted = order.Direction == Direction.Descending ? rows.OrderByDescending(e => e.Id) : rows.OrderBy(e => e.Id);
                var result = sorted.ToArray();
                call[4] = (long)result.Length;
                return result.Take(call.ArgAt<int>(3));
            });
            Media.GetById(Arg.Any<int>()).Returns(call => { Visited.Add(call.Arg<int>()); return (IMedia?)null; });
            Service = new MediaAiRescanService(entities, scopes, Media, NullLogger<MediaAiRescanService>.Instance, Microsoft.Extensions.Options.Options.Create(Options));
        }
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
