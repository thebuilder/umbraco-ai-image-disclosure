using NSubstitute;
using TheBuilder.AIImageDisclosure.Migrations;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class AiDisclosureSchemaTests
{
    [Fact]
    public void AddsReadOnlyPropertiesBesideTheImageFileWithoutDuplicatingThem()
    {
        var helper = new DefaultShortStringHelper(new DefaultShortStringHelperConfig());
        var dataType = LabelDataType();
        var mediaType = new MediaType(helper, -1) { Alias = "Image", Name = "Image" };
        mediaType.AddPropertyType(new PropertyType(helper, dataType, Constants.SourcePropertyAlias) { SortOrder = 4 }, "content", "Content");

        Assert.True(AiDisclosureSchema.AddPropertyIfMissing(mediaType, helper, dataType,
            Constants.AiWatermarkPropertyAlias, "AI watermark", "Watermark evidence"));
        var property = Assert.Single(mediaType.PropertyTypes, p => p.Alias == Constants.AiWatermarkPropertyAlias);
        // Umbraco fills the data type key when it reloads the saved schema.
        property.DataTypeKey = dataType.Key;
        Assert.False(AiDisclosureSchema.AddPropertyIfMissing(mediaType, helper, dataType,
            Constants.AiWatermarkPropertyAlias, "Replacement name", "Replacement description"));
        Assert.Equal("AI watermark", property.Name);
        Assert.Equal("Watermark evidence", property.Description);
        Assert.Equal(5, property.SortOrder);
        Assert.False(property.Mandatory);
        Assert.Contains(property, Assert.Single(mediaType.PropertyGroups).PropertyTypes!);
    }

    [Fact]
    public void PreservesUngroupedPlacementAndRejectsForeignExistingProperties()
    {
        var helper = new DefaultShortStringHelper(new DefaultShortStringHelperConfig());
        var mediaType = new MediaType(helper, -1) { Alias = "Image", Name = "Image" };
        var dataType = LabelDataType();
        Assert.True(AiDisclosureSchema.AddPropertyIfMissing(mediaType, helper, dataType,
            Constants.AiDisclosureReasonPropertyAlias, "AI disclosure reason", "Reason"));
        Assert.Empty(mediaType.PropertyGroups);
        Assert.Equal(0, Assert.Single(mediaType.PropertyTypes).SortOrder);
        Assert.Throws<AiImageDisclosureSchemaCollisionException>(() =>
            AiDisclosureSchema.AddPropertyIfMissing(mediaType, helper, LabelDataType(),
                Constants.AiDisclosureReasonPropertyAlias, "AI disclosure reason", "Reason"));
    }

    private static IDataType LabelDataType()
    {
        var dataType = Substitute.For<IDataType>();
        dataType.Key.Returns(Guid.NewGuid());
        dataType.Id.Returns(42);
        dataType.EditorAlias.Returns("Umbraco.Label");
        dataType.DatabaseType.Returns(ValueStorageType.Nvarchar);
        return dataType;
    }
}
