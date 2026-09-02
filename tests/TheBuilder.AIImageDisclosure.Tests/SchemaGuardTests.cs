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

    [Fact]
    public void AcceptsDisclosureSourceDataTypeUsingOwnedConfiguration()
    {
        var dataType = DisclosureDataType(
            Constants.C2paDisclosureSourceValue,
            Constants.ManualDisclosureSourceValue,
            Constants.ResumeAutomaticDisclosureSourceValue);

        AiImageDisclosureSchemaGuard.EnsureDisclosureSourceDataTypeIsCompatible(dataType);
    }

    [Fact]
    public void AppliesTargetDataTypeInPlace()
    {
        var previewDataTypeKey = Guid.NewGuid();
        var targetDataTypeKey = Guid.NewGuid();
        var property = Substitute.For<IPropertyType>();
        property.Alias.Returns(Constants.AiDisclosureSourcePropertyAlias);
        property.DataTypeKey.Returns(previewDataTypeKey);
        var target = Substitute.For<IDataType>();
        target.Id.Returns(42);
        target.Key.Returns(targetDataTypeKey);
        target.EditorAlias.Returns(Constants.DropDownPropertyEditorAlias);
        target.DatabaseType.Returns(ValueStorageType.Nvarchar);

        var changed = UpgradePreviewAiImageDisclosureSchema.ApplyDataType(property, target);

        Assert.True(changed);
        property.Received().DataTypeId = 42;
        property.Received().DataTypeKey = targetDataTypeKey;
        property.Received().PropertyEditorAlias = Constants.DropDownPropertyEditorAlias;
        property.Received().ValueStorageType = ValueStorageType.Nvarchar;
    }

    [Fact]
    public void AcceptsKnownPreviewPropertyDataType()
    {
        var previewDataTypeKey = Guid.NewGuid();
        var property = Substitute.For<IPropertyType>();
        property.Alias.Returns(Constants.AiDisclosureSourcePropertyAlias);
        property.DataTypeKey.Returns(previewDataTypeKey);
        var mediaType = Substitute.For<IMediaType>();
        mediaType.PropertyTypes.Returns([property]);

        var result = UpgradePreviewAiImageDisclosureSchema.GetUpgradeableProperty(
            mediaType,
            Constants.AiDisclosureSourcePropertyAlias,
            previewDataTypeKey,
            Guid.NewGuid());

        Assert.Same(property, result);
    }

    [Fact]
    public void RejectsUnknownPreviewPropertyDataType()
    {
        var mediaType = Substitute.For<IMediaType>();
        var property = Substitute.For<IPropertyType>();
        property.Alias.Returns(Constants.AiDisclosureSourcePropertyAlias);
        property.DataTypeKey.Returns(Guid.NewGuid());
        mediaType.PropertyTypes.Returns([property]);

        Assert.Throws<AiImageDisclosureSchemaCollisionException>(() =>
            UpgradePreviewAiImageDisclosureSchema.GetUpgradeableProperty(
                mediaType,
                Constants.AiDisclosureSourcePropertyAlias,
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    private static IDataType DisclosureDataType(params string[] items)
    {
        var dataType = Substitute.For<IDataType>();
        dataType.EditorAlias.Returns(Constants.DropDownPropertyEditorAlias);
        dataType.EditorUiAlias.Returns(Constants.DropDownPropertyEditorUiAlias);
        dataType.ConfigurationObject.Returns(new DropDownFlexibleConfiguration
        {
            Items = items.Length > 0
                ? items.ToList()
                : [Constants.GeneratedDisclosureValue, Constants.ModifiedDisclosureValue],
            Multiple = false,
        });
        return dataType;
    }
}
