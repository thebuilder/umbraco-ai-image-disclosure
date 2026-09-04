using Microsoft.Extensions.Configuration;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Backoffice;
using Umbraco.Cms.Api.Management.ViewModels.Tree;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class AiDisclosureFlagProviderTests
{
    [Fact]
    public async Task AddsDisclosureFlagsForDetectedAndEditorValuesWithOneMediaLookup()
    {
        Guid generatedKey = Guid.NewGuid();
        Guid modifiedKey = Guid.NewGuid();
        Guid editorGeneratedKey = Guid.NewGuid();
        Guid editorModifiedKey = Guid.NewGuid();
        IMedia generated = CreateImage(generatedKey, Constants.GeneratedDisclosureValue);
        IMedia modified = CreateImage(modifiedKey, Constants.ModifiedDisclosureValue);
        IMedia editorGenerated = CreateImage(editorGeneratedKey, "[\"generated\"]");
        IMedia editorModified = CreateImage(editorModifiedKey, "[\"modified\"]");
        var mediaService = Substitute.For<IMediaService>();
        mediaService.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns([generated, modified, editorGenerated, editorModified]);
        var provider = new AiDisclosureFlagProvider(mediaService, new ConfigurationBuilder().Build());
        var items = new[]
        {
            new MediaTreeItemResponseModel { Id = generatedKey },
            new MediaTreeItemResponseModel { Id = modifiedKey },
            new MediaTreeItemResponseModel { Id = editorGeneratedKey },
            new MediaTreeItemResponseModel { Id = editorModifiedKey },
        };

        Assert.True(provider.CanProvideFlags<MediaTreeItemResponseModel>());
        await provider.PopulateFlagsAsync(items);

        Assert.Contains(items[0].Flags, flag => flag.Alias == Constants.GeneratedFlagAlias);
        Assert.Contains(items[1].Flags, flag => flag.Alias == Constants.ModifiedFlagAlias);
        Assert.Contains(items[2].Flags, flag => flag.Alias == Constants.GeneratedFlagAlias);
        Assert.Contains(items[3].Flags, flag => flag.Alias == Constants.ModifiedFlagAlias);
        mediaService.Received(1).GetByIds(Arg.Is<IEnumerable<Guid>>(ids => ids.ToHashSet().SetEquals(new[] { generatedKey, modifiedKey, editorGeneratedKey, editorModifiedKey })));
    }

    [Fact]
    public async Task CanDisableBackofficeBadges()
    {
        var mediaService = Substitute.For<IMediaService>();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [Constants.ShowBackofficeBadgesSetting] = "false",
            })
            .Build();
        var provider = new AiDisclosureFlagProvider(mediaService, configuration);
        var item = new MediaTreeItemResponseModel { Id = Guid.NewGuid() };

        await provider.PopulateFlagsAsync([item]);

        Assert.Empty(item.Flags);
        mediaService.DidNotReceive().GetByIds(Arg.Any<IEnumerable<Guid>>());
    }

    private static IMedia CreateImage(Guid key, string disclosure)
    {
        var media = Substitute.For<IMedia>();
        var contentType = Substitute.For<ISimpleContentType>();
        contentType.Alias.Returns(Constants.DefaultImageMediaTypeAlias);
        media.Key.Returns(key);
        media.ContentType.Returns(contentType);
        media.GetValue<string>(Constants.AiDisclosurePropertyAlias).Returns(disclosure);
        return media;
    }
}
