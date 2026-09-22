using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.Controllers;

namespace TheBuilder.AIImageDisclosure.OpenAI.Composers;

/// <summary>Registers optional OpenAI watermark verification and its backoffice settings API.</summary>
public sealed class OpenAiProvenanceComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<OpenAiProvenanceSettingsStore>();
        builder.Services.AddSingleton<OpenAiConnectionResolver>();
        builder.Services.AddSingleton<OpenAiWatermarkVerifier>();
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
