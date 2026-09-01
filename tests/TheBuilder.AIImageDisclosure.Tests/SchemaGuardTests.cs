using NSubstitute;
using TheBuilder.AIImageDisclosure.Migrations;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class SchemaGuardTests
{
    [Fact]
    public void AcceptsPropertyUsingExpectedDataType()
    {
        var dataTypeKey = Guid.NewGuid();
        var property = Substitute.For<IPropertyType>();
        property.DataTypeKey.Returns(dataTypeKey);

        AiImageDisclosureSchemaGuard.EnsurePropertyUsesDataType(
            property,
            dataTypeKey,
            Constants.AiDisclosurePropertyAlias);
    }

    [Fact]
    public void RejectsExistingPropertyWithForeignDataType()
    {
        var foreignDataTypeKey = Guid.NewGuid();
        var property = Substitute.For<IPropertyType>();
        property.DataTypeKey.Returns(foreignDataTypeKey);

        Assert.Throws<AiImageDisclosureSchemaCollisionException>(() =>
            AiImageDisclosureSchemaGuard.EnsurePropertyUsesDataType(
                property,
                Guid.NewGuid(),
                Constants.AiDisclosurePropertyAlias));
    }

    [Fact]
    public void RejectsDisclosureDataTypeUsingForeignEditor()
    {
        var dataType = Substitute.For<IDataType>();
        dataType.EditorAlias.Returns("Umbraco.TextBox");

        Assert.Throws<AiImageDisclosureSchemaCollisionException>(() =>
            AiImageDisclosureSchemaGuard.EnsureDisclosureDataTypeIsCompatible(dataType));
    }

    [Fact]
    public void AcceptsDisclosureDataTypeUsingOwnedConfiguration()
    {
        var dataType = DisclosureDataType();

        AiImageDisclosureSchemaGuard.EnsureDisclosureDataTypeIsCompatible(dataType);
    }

    [Fact]
    public void RejectsDisclosureDataTypeWithModifiedOptions()
    {
        var dataType = DisclosureDataType();
        dataType.ConfigurationObject.Returns(new DropDownFlexibleConfiguration
        {
            Items = [Constants.GeneratedDisclosureValue],
        });

        Assert.Throws<AiImageDisclosureSchemaCollisionException>(() =>
            AiImageDisclosureSchemaGuard.EnsureDisclosureDataTypeIsCompatible(dataType));
    }

    private static IDataType DisclosureDataType()
    {
        var dataType = Substitute.For<IDataType>();
        dataType.EditorAlias.Returns(Constants.DropDownPropertyEditorAlias);
        dataType.EditorUiAlias.Returns(Constants.DropDownPropertyEditorUiAlias);
        dataType.ConfigurationObject.Returns(new DropDownFlexibleConfiguration
        {
            Items = [Constants.GeneratedDisclosureValue, Constants.ModifiedDisclosureValue],
            Multiple = false,
        });
        return dataType;
    }
}
