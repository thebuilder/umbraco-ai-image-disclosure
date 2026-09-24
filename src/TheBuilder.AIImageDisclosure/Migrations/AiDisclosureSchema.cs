using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

namespace TheBuilder.AIImageDisclosure.Migrations;

internal static class AiDisclosureSchema
{
    public static readonly Guid DataTypeKey = new("70eeba7d-e08d-4c7e-9744-990b2f047002");
    public static readonly Guid SourceDataTypeKey = new("6f8909a5-6dc7-40de-8b4d-daab8fc86226");

    public static bool AddPropertyIfMissing(
        IMediaType imageMediaType,
        IShortStringHelper shortStringHelper,
        IDataType dataType,
        string alias,
        string name,
        string description)
    {
        var existingProperty = imageMediaType.PropertyTypes.FirstOrDefault(property => property.Alias == alias);
        AiImageDisclosureSchemaGuard.EnsurePropertyUsesDataType(existingProperty, dataType.Key, alias);
        if (existingProperty is not null) return false;

        var imageGroup = imageMediaType.PropertyGroups.FirstOrDefault(group =>
            group.PropertyTypes?.Any(property => property.Alias == Constants.SourcePropertyAlias) is true);
        var property = new PropertyType(shortStringHelper, dataType, alias)
        {
            Name = name,
            Description = description,
            Mandatory = false,
            SortOrder = imageGroup?.PropertyTypes?.Select(item => item.SortOrder).DefaultIfEmpty(-1).Max() + 1
                ?? imageMediaType.PropertyTypes.Count(),
        };

        if (imageGroup is null)
            imageMediaType.AddPropertyType(property);
        else
            imageMediaType.AddPropertyType(property, imageGroup.Alias, imageGroup.Name);

        return true;
    }
}
