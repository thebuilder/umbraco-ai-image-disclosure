using Microsoft.Extensions.Configuration;
using TheBuilder.AIImageDisclosure.Media;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.Cms.Api.Management.Services.Flags;
using Umbraco.Cms.Api.Management.ViewModels;
using Umbraco.Cms.Api.Management.ViewModels.Media.Collection;
using Umbraco.Cms.Api.Management.ViewModels.Tree;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.Backoffice;

internal sealed class AiDisclosureFlagProvider(
    IMediaAiDisclosureValueReader valueReader,
    IMediaTypeService mediaTypeService,
    IConfiguration configuration,
    IEnumerable<ImageWatermarkProvider> watermarkProviders) : IFlagProvider
{
    // Keep recognizing existing evidence even after its integration package is removed.
    private readonly HashSet<string> detectedWatermarks = watermarkProviders.Select(provider => provider.DetectedValue)
        .Append(Constants.OpenAiWatermarkDetected).Append("AI watermark detected").ToHashSet(StringComparer.Ordinal);

    public bool CanProvideFlags<TItem>()
        where TItem : IHasFlags =>
        typeof(TItem) == typeof(MediaTreeItemResponseModel) ||
        typeof(TItem) == typeof(MediaCollectionResponseModel);

    public Task PopulateFlagsAsync<TItem>(IEnumerable<TItem> itemViewModels)
        where TItem : IHasFlags
    {
        if (configuration.GetValue(Constants.ShowBackofficeBadgesSetting, true) is false)
        {
            return Task.CompletedTask;
        }

        // Only Image media can carry a disclosure, so skip folders and files before loading anything.
        Guid? imageTypeKey = mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)?.Key;
        TItem[] items = itemViewModels.Where(item => imageTypeKey is not null && MediaTypeKey(item) == imageTypeKey).ToArray();
        IReadOnlyDictionary<Guid, MediaAiDisclosureValues> valuesByKey = valueReader.GetImageValues(items.Select(item => item.Id).ToArray());

        foreach (TItem item in items)
        {
            if (valuesByKey.TryGetValue(item.Id, out MediaAiDisclosureValues? values) is false)
            {
                continue;
            }

            switch (DropdownValue.Decode(values.Disclosure))
            {
                case Constants.GeneratedDisclosureValue:
                    item.AddFlag(Constants.GeneratedFlagAlias);
                    break;
                case Constants.ModifiedDisclosureValue:
                    item.AddFlag(Constants.ModifiedFlagAlias);
                    break;
                default:
                    if (detectedWatermarks.Contains(values.Watermark ?? string.Empty)
                        && !MediaAiMetadataProcessor.IsManualSource(values.Source))
                        item.AddFlag(Constants.WatermarkFlagAlias);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static Guid? MediaTypeKey<TItem>(TItem item) =>
        item switch
        {
            MediaTreeItemResponseModel treeItem => treeItem.MediaType.Id,
            MediaCollectionResponseModel collectionItem => collectionItem.MediaType.Id,
            _ => null,
        };
}
