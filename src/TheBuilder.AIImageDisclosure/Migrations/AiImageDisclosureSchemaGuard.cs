using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace TheBuilder.AIImageDisclosure.Migrations;

internal sealed class AiImageDisclosureSchemaCollisionException(string message) : InvalidOperationException(message);

internal static class AiImageDisclosureSchemaGuard
{
    public static void EnsureDisclosureDataTypeIsCompatible(IDataType dataType)
        => EnsureSingleSelectDataTypeIsCompatible(
            dataType,
            Constants.AiDisclosureDataTypeName,
            [Constants.GeneratedDisclosureValue, Constants.ModifiedDisclosureValue]);

    public static void EnsureDisclosureSourceDataTypeIsCompatible(IDataType dataType)
        => EnsureSingleSelectDataTypeIsCompatible(
            dataType,
            Constants.AiDisclosureSourceDataTypeName,
            [
                Constants.C2paDisclosureSourceValue,
                Constants.ManualDisclosureSourceValue,
                Constants.ResumeAutomaticDisclosureSourceValue,
            ]);

    private static void EnsureSingleSelectDataTypeIsCompatible(
        IDataType dataType,
        string dataTypeName,
        IReadOnlyCollection<string> expectedItems)
    {
        var configuration = dataType.ConfigurationObject as DropDownFlexibleConfiguration;
        if (dataType.EditorAlias != Constants.DropDownPropertyEditorAlias
            || dataType.EditorUiAlias != Constants.DropDownPropertyEditorUiAlias
            || configuration is null
            || configuration.Multiple
            || configuration.Items is not { } items
            || !items.SequenceEqual(expectedItems, StringComparer.Ordinal))
        {
            throw new AiImageDisclosureSchemaCollisionException(
                $"The data type {dataTypeName} already exists with incompatible editor settings.");
        }
    }

    public static void EnsurePropertyUsesDataType(IPropertyType? property, Guid dataTypeKey, string propertyAlias)
    {
        if (property is not null && property.DataTypeKey != dataTypeKey)
        {
            throw new AiImageDisclosureSchemaCollisionException(
                $"The media property {propertyAlias} already exists with an incompatible data type.");
        }
    }
}
