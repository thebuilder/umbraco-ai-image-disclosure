using TheBuilder.AIImageDisclosure.Detection;
using Umbraco.Cms.Core.Models;

namespace TheBuilder.AIImageDisclosure.Media;

internal interface IMediaAiMetadataProcessor
{
    Task<AiImageMetadata> InspectAsync(IMedia media, CancellationToken cancellationToken = default);

    Task<AiImageMetadata> ResumeAutomaticDetectionAsync(IMedia media, CancellationToken cancellationToken = default);
}
