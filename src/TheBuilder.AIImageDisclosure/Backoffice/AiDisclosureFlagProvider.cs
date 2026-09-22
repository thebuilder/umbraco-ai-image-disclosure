using Microsoft.Extensions.Configuration;
using Umbraco.Cms.Api.Management.Services.Flags;
using Umbraco.Cms.Api.Management.ViewModels;
using Umbraco.Cms.Api.Management.ViewModels.Tree;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.Backoffice;

internal sealed class AiDisclosureFlagProvider(
    IMediaService mediaService,
    IConfiguration configuration) : IFlagProvider
{
    private const string GeneratedDropdownValue = "[\"generated\"]";
    private const string ModifiedDropdownValue = "[\"modified\"]";

    public bool CanProvideFlags<TItem>()
        where TItem : IHasFlags =>
        typeof(TItem) == typeof(MediaTreeItemResponseModel);

    public Task PopulateFlagsAsync<TItem>(IEnumerable<TItem> itemViewModels)
        where TItem : IHasFlags
    {
        if (configuration.GetValue(Constants.ShowBackofficeBadgesSetting, true) is false)
        {
            return Task.CompletedTask;
        }

        TItem[] items = itemViewModels.ToArray();
        var mediaByKey = mediaService.GetByIds(items.Select(item => item.Id)).ToDictionary(media => media.Key);

        foreach (TItem item in items)
        {
            if (mediaByKey.TryGetValue(item.Id, out var media) is false ||
                media.ContentType.Alias.Equals(Constants.DefaultImageMediaTypeAlias, StringComparison.OrdinalIgnoreCase) is false)
            {
                continue;
            }

            switch (media.GetValue<string>(Constants.AiDisclosurePropertyAlias))
            {
                case Constants.GeneratedDisclosureValue:
                case GeneratedDropdownValue:
                    item.AddFlag(Constants.GeneratedFlagAlias);
                    break;
                case Constants.ModifiedDisclosureValue:
                case ModifiedDropdownValue:
                    item.AddFlag(Constants.ModifiedFlagAlias);
                    break;
                default:
                    if (media.GetValue<string>(Constants.AiWatermarkPropertyAlias) == Constants.OpenAiWatermarkDetected
                        && media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias) is not (Constants.ManualDisclosureSourceValue or "[\"Manual\"]"))
                        item.AddFlag(Constants.WatermarkFlagAlias);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
