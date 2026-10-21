using Microsoft.Extensions.Configuration;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Backoffice;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.Cms.Api.Management.ViewModels;
using Umbraco.Cms.Api.Management.ViewModels.Media.Collection;
using Umbraco.Cms.Api.Management.ViewModels.MediaType;
using Umbraco.Cms.Api.Management.ViewModels.Tree;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class AiDisclosureFlagProviderTests
{
    private static readonly Guid ImageTypeKey = Guid.NewGuid();
    private static readonly Dictionary<Guid, MediaAiDisclosureValues> NoValues = [];

    public static TheoryData<string> ItemKinds => ["tree", "collection"];

    [Fact]
    public void ProvidesFlagsForMediaTreeAndCollectionItems()
    {
        var provider = CreateProvider(CreateReader(NoValues));

        Assert.True(provider.CanProvideFlags<MediaTreeItemResponseModel>());
        Assert.True(provider.CanProvideFlags<MediaCollectionResponseModel>());
    }

    [Theory]
    [MemberData(nameof(ItemKinds))]
    public async Task AddsDisclosureFlagsForDetectedAndEditorValues(string kind)
    {
        Guid generatedKey = Guid.NewGuid();
        Guid modifiedKey = Guid.NewGuid();
        Guid editorGeneratedKey = Guid.NewGuid();
        Guid editorModifiedKey = Guid.NewGuid();
        var reader = CreateReader(new Dictionary<Guid, MediaAiDisclosureValues>
        {
            [generatedKey] = new(Constants.GeneratedDisclosureValue, null, null),
            [modifiedKey] = new(Constants.ModifiedDisclosureValue, null, null),
            [editorGeneratedKey] = new("[\"generated\"]", null, null),
            [editorModifiedKey] = new("[\"modified\"]", null, null),
        });
        var provider = CreateProvider(reader);
        IHasFlags[] items =
        [
            CreateItem(kind, generatedKey),
            CreateItem(kind, modifiedKey),
            CreateItem(kind, editorGeneratedKey),
            CreateItem(kind, editorModifiedKey),
        ];

        await PopulateAsync(provider, kind, items);

        Assert.Contains(items[0].Flags, flag => flag.Alias == Constants.GeneratedFlagAlias);
        Assert.Contains(items[1].Flags, flag => flag.Alias == Constants.ModifiedFlagAlias);
        Assert.Contains(items[2].Flags, flag => flag.Alias == Constants.GeneratedFlagAlias);
        Assert.Contains(items[3].Flags, flag => flag.Alias == Constants.ModifiedFlagAlias);
        reader.Received(1).GetImageValues(Arg.Is<IReadOnlyCollection<Guid>>(keys =>
            keys.ToHashSet().SetEquals(new[] { generatedKey, modifiedKey, editorGeneratedKey, editorModifiedKey })));
    }

    [Theory]
    [MemberData(nameof(ItemKinds))]
    public async Task SkipsOtherMediaTypesBeforeReadingValues(string kind)
    {
        Guid imageKey = Guid.NewGuid();
        var reader = CreateReader(NoValues);
        IHasFlags[] items = [CreateItem(kind, imageKey), CreateItem(kind, Guid.NewGuid(), mediaTypeKey: Guid.NewGuid())];

        await PopulateAsync(CreateProvider(reader), kind, items);

        reader.Received(1).GetImageValues(Arg.Is<IReadOnlyCollection<Guid>>(keys => keys.SequenceEqual(new[] { imageKey })));
    }

    [Theory]
    [MemberData(nameof(ItemKinds))]
    public async Task CanDisableBackofficeBadges(string kind)
    {
        var reader = CreateReader(NoValues);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [Constants.ShowBackofficeBadgesSetting] = "false",
            })
            .Build();
        IHasFlags item = CreateItem(kind, Guid.NewGuid());

        await PopulateAsync(CreateProvider(reader, configuration), kind, [item]);

        Assert.Empty(item.Flags);
        reader.DidNotReceive().GetImageValues(Arg.Any<IReadOnlyCollection<Guid>>());
    }

    [Theory]
    [InlineData("tree", "", true)]
    [InlineData("tree", "Manual", false)]
    [InlineData("tree", "[\"Manual\"]", false)]
    [InlineData("collection", "", true)]
    [InlineData("collection", "Manual", false)]
    [InlineData("collection", "[\"Manual\"]", false)]
    public async Task WatermarkFlagDoesNotOverrideAnExplicitManualUndeterminedChoice(string kind, string source, bool expected)
    {
        Guid key = Guid.NewGuid();
        var reader = CreateReader(new Dictionary<Guid, MediaAiDisclosureValues>
        {
            [key] = new(string.Empty, Constants.OpenAiWatermarkDetected, source),
        });
        IHasFlags item = CreateItem(kind, key);

        await PopulateAsync(CreateProvider(reader), kind, [item]);

        Assert.Equal(expected, item.Flags.Any(flag => flag.Alias == Constants.WatermarkFlagAlias));
        Assert.DoesNotContain(item.Flags, flag => flag.Alias == Constants.GeneratedFlagAlias);
    }

    [Fact]
    public async Task RecognizesRegisteredProviderEvidenceWithoutOpenAiAttribution()
    {
        var watermarkProvider = new ImageWatermarkProvider("Example", "TestMark");
        Guid key = Guid.NewGuid();
        var reader = CreateReader(new Dictionary<Guid, MediaAiDisclosureValues>
        {
            [key] = new(string.Empty, watermarkProvider.DetectedValue, null),
        });
        IHasFlags item = CreateItem("collection", key);

        await PopulateAsync(CreateProvider(reader, watermarkProviders: [watermarkProvider]), "collection", [item]);

        Assert.Contains(item.Flags, flag => flag.Alias == Constants.WatermarkFlagAlias);
    }

    [Fact]
    public void ReaderLoadsMediaOnceAndMapsTheirValues()
    {
        IMedia generated = CreateMedia(Constants.GeneratedDisclosureValue);
        IMedia modified = CreateMedia(Constants.ModifiedDisclosureValue);
        var mediaService = Substitute.For<IMediaService>();
        mediaService.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns([generated, modified]);

        var values = new MediaAiDisclosureValueReader(mediaService).GetImageValues([generated.Key, modified.Key]);

        Assert.Equal(Constants.GeneratedDisclosureValue, values[generated.Key].Disclosure);
        Assert.Equal(Constants.ModifiedDisclosureValue, values[modified.Key].Disclosure);
        mediaService.Received(1).GetByIds(Arg.Any<IEnumerable<Guid>>());
    }

    private static IMediaAiDisclosureValueReader CreateReader(IReadOnlyDictionary<Guid, MediaAiDisclosureValues> values)
    {
        var reader = Substitute.For<IMediaAiDisclosureValueReader>();
        reader.GetImageValues(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(values);
        return reader;
    }

    private static AiDisclosureFlagProvider CreateProvider(
        IMediaAiDisclosureValueReader reader,
        IConfiguration? configuration = null,
        IEnumerable<ImageWatermarkProvider>? watermarkProviders = null)
    {
        var imageType = Substitute.For<IMediaType>();
        imageType.Key.Returns(ImageTypeKey);
        var mediaTypeService = Substitute.For<IMediaTypeService>();
        mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias).Returns(imageType);

        return new AiDisclosureFlagProvider(
            reader,
            mediaTypeService,
            configuration ?? new ConfigurationBuilder().Build(),
            watermarkProviders ?? []);
    }

    private static IHasFlags CreateItem(string kind, Guid key, Guid? mediaTypeKey = null) =>
        kind == "tree"
            ? new MediaTreeItemResponseModel { Id = key, MediaType = new MediaTypeReferenceResponseModel { Id = mediaTypeKey ?? ImageTypeKey } }
            : new MediaCollectionResponseModel { Id = key, MediaType = new MediaTypeCollectionReferenceResponseModel { Id = mediaTypeKey ?? ImageTypeKey } };

    private static Task PopulateAsync(AiDisclosureFlagProvider provider, string kind, IHasFlags[] items) =>
        kind == "tree"
            ? provider.PopulateFlagsAsync(items.Cast<MediaTreeItemResponseModel>())
            : provider.PopulateFlagsAsync(items.Cast<MediaCollectionResponseModel>());

    private static IMedia CreateMedia(string disclosure)
    {
        var media = Substitute.For<IMedia>();
        media.Key.Returns(Guid.NewGuid());
        media.GetValue<string>(Constants.AiDisclosurePropertyAlias).Returns(disclosure);
        return media;
    }
}
