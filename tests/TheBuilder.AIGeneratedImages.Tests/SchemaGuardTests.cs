using NSubstitute;
using TheBuilder.AIGeneratedImages.Migrations;
using Umbraco.Cms.Core.Models;

namespace TheBuilder.AIGeneratedImages.Tests;

public sealed class SchemaGuardTests
{
    [Fact]
    public void AcceptsOwnedBooleanSchema()
    {
        var dataType = CreateDataType(
            Constants.AiGeneratedDataTypeKey,
            Constants.BooleanPropertyEditorAlias,
            Constants.BooleanPropertyEditorUiAlias);
        var property = Substitute.For<IPropertyType>();
        property.DataTypeKey.Returns(Constants.AiGeneratedDataTypeKey);

        AiGeneratedImagesSchemaGuard.EnsureCompatible(
            Constants.AiGeneratedPropertyAlias,
            Constants.AiGeneratedDataTypeKey,
            Constants.BooleanPropertyEditorAlias,
            Constants.BooleanPropertyEditorUiAlias,
            dataType,
            property,
            dataType);
    }

    [Fact]
    public void RejectsExistingPropertyWithForeignDataType()
    {
        var foreignDataType = CreateDataType(Guid.NewGuid(), "Umbraco.TextBox", "Umb.PropertyEditorUi.TextBox");
        var property = Substitute.For<IPropertyType>();
        property.DataTypeKey.Returns(foreignDataType.Key);

        Assert.Throws<AiGeneratedImagesSchemaCollisionException>(() =>
            AiGeneratedImagesSchemaGuard.EnsureCompatible(
                Constants.AiGeneratedPropertyAlias,
                Constants.AiGeneratedDataTypeKey,
                Constants.BooleanPropertyEditorAlias,
                Constants.BooleanPropertyEditorUiAlias,
                null,
                property,
                foreignDataType));
    }

    private static IDataType CreateDataType(Guid key, string editorAlias, string editorUiAlias)
    {
        var dataType = Substitute.For<IDataType>();
        dataType.Key.Returns(key);
        dataType.EditorAlias.Returns(editorAlias);
        dataType.EditorUiAlias.Returns(editorUiAlias);
        return dataType;
    }
}
