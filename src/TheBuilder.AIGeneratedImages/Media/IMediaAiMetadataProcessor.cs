using TheBuilder.AIGeneratedImages.Detection;
using Umbraco.Cms.Core.Models;

namespace TheBuilder.AIGeneratedImages.Media;

internal interface IMediaAiMetadataProcessor
{
    AiImageMetadata Inspect(IMedia media);
}
