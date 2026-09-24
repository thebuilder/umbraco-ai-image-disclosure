using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;

namespace TheBuilder.AIImageDisclosure.Migrations;

internal sealed class AddAiDisclosureReasonSchema(
    IMigrationContext context,
    IDataTypeService dataTypeService,
    IMediaTypeService mediaTypeService,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer,
    IShortStringHelper shortStringHelper) : AsyncMigrationBase(context)
{
    protected override async Task MigrateAsync()
    {
        var imageMediaType = mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)
            ?? throw new InvalidOperationException("The default Umbraco Image media type was not found.");
        var dataType = await new AiImageDisclosureDataTypeProvider(
            dataTypeService, propertyEditors, configurationSerializer).GetRequiredAsync(
                Umbraco.Cms.Core.Constants.DataTypes.Guids.LabelStringGuid, "Label (string)");
        if (!AiDisclosureSchema.AddPropertyIfMissing(
                imageMediaType, shortStringHelper, dataType, Constants.AiDisclosureReasonPropertyAlias,
                "AI disclosure reason", Constants.AiDisclosureReasonPropertyDescription)) return;
        var result = await mediaTypeService.UpdateAsync(
            imageMediaType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        if (!result.Success)
            throw new InvalidOperationException($"Could not add the AI disclosure reason property: {result.Result}.");
    }
}
