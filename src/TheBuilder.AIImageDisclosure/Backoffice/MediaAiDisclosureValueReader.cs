using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.Backoffice;

/// <summary>The stored AI disclosure values of one Image media item.</summary>
internal sealed record MediaAiDisclosureValues(string? Disclosure, string? Watermark, string? Source);

/// <summary>Reads stored AI disclosure values for a batch of media items.</summary>
internal interface IMediaAiDisclosureValueReader
{
    /// <summary>Returns the values of the given media keys. Callers pass only Image media keys.</summary>
    IReadOnlyDictionary<Guid, MediaAiDisclosureValues> GetImageValues(IReadOnlyCollection<Guid> keys);
}

internal sealed class MediaAiDisclosureValueReader(IMediaService mediaService) : IMediaAiDisclosureValueReader
{
    public IReadOnlyDictionary<Guid, MediaAiDisclosureValues> GetImageValues(IReadOnlyCollection<Guid> keys)
    {
        if (keys.Count == 0)
        {
            return new Dictionary<Guid, MediaAiDisclosureValues>();
        }

        return mediaService.GetByIds(keys)
            .ToDictionary(
                media => media.Key,
                media => new MediaAiDisclosureValues(
                    media.GetValue<string>(Constants.AiDisclosurePropertyAlias),
                    media.GetValue<string>(Constants.AiWatermarkPropertyAlias),
                    media.GetValue<string>(Constants.AiDisclosureSourcePropertyAlias)));
    }
}
