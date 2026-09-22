using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheBuilder.AIImageDisclosure.Watermarks;
using TheBuilder.AIImageDisclosure.Detection;
using TheBuilder.AIImageDisclosure.Media;
using TheBuilder.AIImageDisclosure.Migrations;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;
using TheBuilder.AIImageDisclosure.Backoffice;

namespace TheBuilder.AIImageDisclosure.Composers;

/// <summary>Registers AI image disclosure detection with Umbraco.</summary>
public sealed class AiImageDisclosureComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.TryAddSingleton<IImageWatermarkVerifier, DisabledImageWatermarkVerifier>();
        builder.Services.AddSingleton<IImageAiMetadataReader, C2paImageAiMetadataReader>();
        builder.Services.AddSingleton<IMediaAiMetadataProcessor, MediaAiMetadataProcessor>();
        builder.Services.AddOptions<MediaAiRescanOptions>();
        builder.Services.AddSingleton<MediaAiRescanService>();
        builder.Services.AddControllers().AddApplicationPart(typeof(Controllers.AiImageDisclosureController).Assembly);
        builder.AddNotificationAsyncHandler<MediaSavingNotification, AiImageDisclosureMediaSavingHandler>();
        builder.FlagProviders().Append<AiDisclosureFlagProvider>();
        builder.PackageMigrationPlans().Add(typeof(AiImageDisclosurePackageMigrationPlan));
    }
}
