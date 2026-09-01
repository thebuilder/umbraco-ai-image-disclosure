using Umbraco.Cms.Core.Models;

namespace TheBuilder.AIGeneratedImages.Migrations;

internal sealed class AiGeneratedImagesSchemaCollisionException(string message) : InvalidOperationException(message);

internal static class AiGeneratedImagesSchemaGuard
{
    public static void EnsureCompatible(
        string propertyAlias,
        Guid dataTypeKey,
        string editorAlias,
        string editorUiAlias,
        IDataType? ownedDataType,
        IPropertyType? property,
        IDataType? propertyDataType)
    {
        if (ownedDataType is not null && !IsCompatible(ownedDataType, dataTypeKey, editorAlias, editorUiAlias))
        {
            throw new AiGeneratedImagesSchemaCollisionException(
                $"The data type for {propertyAlias} exists but is not owned by {Constants.PackageName}.");
        }

        if (property is not null
            && (propertyDataType is null
                || property.DataTypeKey != propertyDataType.Key
                || !IsCompatible(propertyDataType, dataTypeKey, editorAlias, editorUiAlias)))
        {
            throw new AiGeneratedImagesSchemaCollisionException(
                $"The media property {propertyAlias} exists with an incompatible data type or editor.");
        }
    }

    private static bool IsCompatible(
        IDataType dataType,
        Guid dataTypeKey,
        string editorAlias,
        string editorUiAlias) =>
        dataType.Key == dataTypeKey
        && string.Equals(dataType.EditorAlias, editorAlias, StringComparison.Ordinal)
        && string.Equals(dataType.EditorUiAlias, editorUiAlias, StringComparison.Ordinal);
}
