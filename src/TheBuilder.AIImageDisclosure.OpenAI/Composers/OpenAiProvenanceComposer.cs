using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheBuilder.AIImageDisclosure.Media;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace TheBuilder.AIImageDisclosure.OpenAI.Composers;

/// <summary>Registers OpenAI watermark verification and its backoffice settings API.</summary>
public sealed class OpenAiProvenanceComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<OpenAiProvenanceSettingsStore>();
        builder.Services.TryAddSingleton<IOpenAiConnectionResolver, OpenAiConfigurationResolver>();
        builder.Services.AddSingleton<OpenAiWatermarkVerifier>();
        builder.Services.AddSingleton(OpenAiWatermarkVerifier.Provider);
        builder.Services.Configure<MediaAiRescanOptions>(options => options.MaximumBatchSize = 1);
        builder.Services.AddHttpClient("TheBuilder.AIImageDisclosure.OpenAI", client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestVersion = HttpVersion.Version11;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.Services.Replace(ServiceDescriptor.Singleton<IImageWatermarkVerifier>(provider =>
            provider.GetRequiredService<OpenAiWatermarkVerifier>()));
        builder.Services.AddControllers().AddApplicationPart(typeof(Controllers.OpenAiProvenanceController).Assembly);
    }
}
