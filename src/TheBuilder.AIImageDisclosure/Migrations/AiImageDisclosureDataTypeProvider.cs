using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.Migrations;

internal sealed class AiImageDisclosureDataTypeProvider(
    IDataTypeService dataTypeService,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer)
{
    private static readonly Guid MigrationUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;

    public Task<IDataType> GetDisclosureAsync() => GetOrCreateSingleSelectAsync(
        AiDisclosureSchema.DataTypeKey,
        Constants.AiDisclosureDataTypeName,
        [Constants.GeneratedDisclosureValue, Constants.ModifiedDisclosureValue],
        AiImageDisclosureSchemaGuard.EnsureDisclosureDataTypeIsCompatible);

    public Task<IDataType> GetDisclosureSourceAsync() => GetOrCreateSingleSelectAsync(
        AiDisclosureSchema.SourceDataTypeKey,
        Constants.AiDisclosureSourceDataTypeName,
        [
            Constants.C2paDisclosureSourceValue,
            Constants.ManualDisclosureSourceValue,
            Constants.ResumeAutomaticDisclosureSourceValue,
        ],
        AiImageDisclosureSchemaGuard.EnsureDisclosureSourceDataTypeIsCompatible);

    public async Task<IDataType> GetRequiredAsync(Guid key, string name) =>
        await dataTypeService.GetAsync(key)
        ?? throw new InvalidOperationException($"The built-in Umbraco {name} data type was not found.");

    private async Task<IDataType> GetOrCreateSingleSelectAsync(
        Guid key,
        string name,
        string[] items,
        Action<IDataType> ensureCompatible)
    {
        var existing = await dataTypeService.GetAsync(key);
        if (existing is not null)
        {
            ensureCompatible(existing);
            return existing;
        }

        if (!propertyEditors.TryGet(Constants.DropDownPropertyEditorAlias, out IDataEditor? editor) || editor is null)
        {
            throw new InvalidOperationException(
                $"The Umbraco editor {Constants.DropDownPropertyEditorAlias} is not available.");
        }

        var dataType = new DataType(editor, configurationSerializer, -1)
        {
            Key = key,
            Name = name,
            EditorUiAlias = Constants.DropDownPropertyEditorUiAlias,
            ConfigurationData = new Dictionary<string, object>
            {
                ["multiple"] = false,
                ["items"] = items,
            },
        };

        await dataTypeService.CreateAsync(dataType, MigrationUserKey);
        return dataType;
    }
}
