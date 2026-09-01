using TheBuilder.AIImageDisclosure.Detection;
using Umbraco.Cms.Core.Models;

namespace TheBuilder.AIImageDisclosure.Media;

internal interface IMediaAiMetadataProcessor
{
    AiImageMetadata Inspect(IMedia media);
}
