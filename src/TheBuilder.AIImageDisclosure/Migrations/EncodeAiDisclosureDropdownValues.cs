using TheBuilder.AIImageDisclosure.Media;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace TheBuilder.AIImageDisclosure.Migrations;

/// <summary>
/// Rewrites plain dropdown values stored by earlier versions as JSON arrays, so saving a media item in the
/// backoffice no longer clears its AI disclosure.
/// </summary>
internal sealed class EncodeAiDisclosureDropdownValues(
    IMigrationContext context,
    IMediaTypeService mediaTypeService) : AsyncMigrationBase(context)
{
    private static readonly string[] StoredValues =
    [
        Constants.GeneratedDisclosureValue,
        Constants.ModifiedDisclosureValue,
        Constants.C2paDisclosureSourceValue,
        Constants.ManualDisclosureSourceValue,
        Constants.ResumeAutomaticDisclosureSourceValue,
    ];

    protected override Task MigrateAsync()
    {
        int[] propertyTypeIds = mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)?.PropertyTypes
            .Where(property => property.Alias is Constants.AiDisclosurePropertyAlias or Constants.AiDisclosureSourcePropertyAlias)
            .Select(property => property.Id)
            .ToArray() ?? [];
        if (propertyTypeIds.Length == 0)
        {
            return Task.CompletedTask;
        }

        string table = SqlSyntax.GetQuotedTableName(Umbraco.Cms.Core.Constants.DatabaseSchema.Tables.PropertyData);
        string propertyTypeColumn = SqlSyntax.GetQuotedColumnName("propertyTypeId");
        var updated = 0;

        foreach (string column in new[] { "varcharValue", "textValue" }.Select(SqlSyntax.GetQuotedColumnName))
        {
            foreach (string value in StoredValues)
            {
                updated += Database.Execute(
                    $"UPDATE {table} SET {column} = @0 WHERE {column} = @1 AND {propertyTypeColumn} IN (@2)",
                    DropdownValue.Encode(value),
                    value,
                    propertyTypeIds);
            }
        }

        // The media cache still holds the plain values.
        RebuildCache = updated > 0;
        return Task.CompletedTask;
    }
}
