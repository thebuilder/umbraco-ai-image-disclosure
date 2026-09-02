using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace TheBuilder.AIImageDisclosure.Migrations;

internal sealed class UpgradePreviewAiImageDisclosureSchema(
    IMigrationContext context,
    IDataTypeService dataTypeService,
    IMediaTypeService mediaTypeService,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer) : AsyncMigrationBase(context)
{
    private static readonly Guid MigrationUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;
    private readonly AiImageDisclosureDataTypeProvider _dataTypes =
        new(dataTypeService, propertyEditors, configurationSerializer);

    protected override async Task MigrateAsync()
    {
        var imageMediaType = mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)
            ?? throw new InvalidOperationException("The default Umbraco Image media type was not found.");
        var sourceProperty = GetUpgradeableProperty(
            imageMediaType,
            Constants.AiDisclosureSourcePropertyAlias,
            Umbraco.Cms.Core.Constants.DataTypes.Guids.LabelStringGuid,
            AiDisclosureSchema.SourceDataTypeKey);
        var generatorProperty = GetUpgradeableProperty(
            imageMediaType,
            Constants.AiGeneratorPropertyAlias,
            Umbraco.Cms.Core.Constants.DataTypes.Guids.TextstringGuid,
            Umbraco.Cms.Core.Constants.DataTypes.Guids.LabelStringGuid);
        var disclosureSourceDataType = await _dataTypes.GetDisclosureSourceAsync();
        var generatorDataType = await _dataTypes.GetRequiredAsync(
            Umbraco.Cms.Core.Constants.DataTypes.Guids.LabelStringGuid,
            "Label (string)");

        var changed = ApplyDataType(sourceProperty, disclosureSourceDataType);
        changed |= ApplyDataType(generatorProperty, generatorDataType);

        if (!string.Equals(
                generatorProperty.Description,
                Constants.AiGeneratorPropertyDescription,
                StringComparison.Ordinal))
        {
            generatorProperty.Description = Constants.AiGeneratorPropertyDescription;
            changed = true;
        }

        if (changed)
            await mediaTypeService.UpdateAsync(imageMediaType, MigrationUserKey);
    }

    internal static bool ApplyDataType(IPropertyType property, IDataType targetDataType)
    {
        if (property.DataTypeKey == targetDataType.Key) return false;

        property.DataTypeId = targetDataType.Id;
        property.DataTypeKey = targetDataType.Key;
        property.PropertyEditorAlias = targetDataType.EditorAlias;
        property.ValueStorageType = targetDataType.DatabaseType;
        return true;
    }

    internal static IPropertyType GetUpgradeableProperty(
        IMediaType imageMediaType,
        string alias,
        Guid previewDataTypeKey,
        Guid targetDataTypeKey)
    {
        var property = imageMediaType.PropertyTypes.FirstOrDefault(property => property.Alias == alias)
            ?? throw new AiImageDisclosureSchemaCollisionException(
                $"The media property {alias} is missing and cannot be upgraded safely.");
        if (property.DataTypeKey != previewDataTypeKey && property.DataTypeKey != targetDataTypeKey)
        {
            throw new AiImageDisclosureSchemaCollisionException(
                $"The media property {alias} uses an unknown data type and cannot be upgraded safely.");
        }

        return property;
    }
}
