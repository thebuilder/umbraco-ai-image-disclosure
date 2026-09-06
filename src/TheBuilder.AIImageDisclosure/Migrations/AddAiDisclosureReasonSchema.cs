using Umbraco.Cms.Core.Models;
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
        var existing = imageMediaType.PropertyTypes.FirstOrDefault(property =>
            property.Alias == Constants.AiDisclosureReasonPropertyAlias);
        if (existing is not null)
        {
            AiImageDisclosureSchemaGuard.EnsurePropertyUsesDataType(
                existing, dataType.Key, Constants.AiDisclosureReasonPropertyAlias);
            return;
        }

        var group = imageMediaType.PropertyGroups.FirstOrDefault(item =>
            item.PropertyTypes?.Any(property => property.Alias == Constants.SourcePropertyAlias) is true);
        var property = new PropertyType(shortStringHelper, dataType, Constants.AiDisclosureReasonPropertyAlias)
        {
            Name = "AI disclosure reason",
            Description = Constants.AiDisclosureReasonPropertyDescription,
            Mandatory = false,
            SortOrder = group?.PropertyTypes?.Select(item => item.SortOrder).DefaultIfEmpty(-1).Max() + 1
                ?? imageMediaType.PropertyTypes.Count(),
        };
        if (group is null)
            imageMediaType.AddPropertyType(property);
        else
            imageMediaType.AddPropertyType(property, group.Alias, group.Name);
        var result = await mediaTypeService.UpdateAsync(
            imageMediaType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        if (!result.Success)
            throw new InvalidOperationException($"Could not add the AI disclosure reason property: {result.Result}.");
    }
}
