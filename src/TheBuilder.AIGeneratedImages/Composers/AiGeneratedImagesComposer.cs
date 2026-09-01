using Microsoft.Extensions.DependencyInjection;
using TheBuilder.AIGeneratedImages.Detection;
using TheBuilder.AIGeneratedImages.Media;
using TheBuilder.AIGeneratedImages.Migrations;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace TheBuilder.AIGeneratedImages.Composers;

/// <summary>Registers AI-generated image detection with Umbraco.</summary>
public sealed class AiGeneratedImagesComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IImageAiMetadataReader, C2paImageAiMetadataReader>();
        builder.Services.AddSingleton<IMediaAiMetadataProcessor, MediaAiMetadataProcessor>();
        builder.AddNotificationHandler<MediaSavingNotification, AiGeneratedMediaSavingHandler>();
        builder.PackageMigrationPlans().Add(typeof(AiGeneratedImagesPackageMigrationPlan));
    }
}
