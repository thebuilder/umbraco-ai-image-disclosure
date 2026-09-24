using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace TheBuilder.AIImageDisclosure.OpenAI.UmbracoAI;

/// <summary>Adds Umbraco.AI connections as an optional credential source.</summary>
public sealed class OpenAiProvenanceComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<IOpenAiConnectionSource, OpenAiConnectionResolver>());
    }
}
