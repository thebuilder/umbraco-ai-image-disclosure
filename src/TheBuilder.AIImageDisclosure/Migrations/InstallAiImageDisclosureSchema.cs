using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace TheBuilder.AIImageDisclosure.Migrations;

internal sealed class InstallAiImageDisclosureSchema : AsyncPackageMigrationBase
{
    private static readonly Guid MigrationUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;
    private readonly IDataTypeService _dataTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _configurationSerializer;
    private readonly IShortStringHelper _shortStringHelper;

    public InstallAiImageDisclosureSchema(
        IPackagingService packagingService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IMigrationContext context,
        IOptions<PackageMigrationSettings> packageMigrationsSettings,
        IDataTypeService dataTypeService,
        IMediaTypeService mediaTypeService,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer configurationSerializer)
        : base(
            packagingService,
            mediaService,
            mediaFileManager,
            mediaUrlGenerators,
            shortStringHelper,
            contentTypeBaseServiceProvider,
            context,
            packageMigrationsSettings)
    {
        _dataTypeService = dataTypeService;
        _mediaTypeService = mediaTypeService;
        _propertyEditors = propertyEditors;
        _configurationSerializer = configurationSerializer;
        _shortStringHelper = shortStringHelper;
    }

    protected override async Task MigrateAsync()
    {
        var imageMediaType = _mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)
            ?? throw new InvalidOperationException("The default Umbraco Image media type was not found.");

        var aiDisclosureDataType = await GetOrCreateDisclosureDataTypeAsync();
        var aiGeneratorDataType = await GetRequiredDataTypeAsync(
            Umbraco.Cms.Core.Constants.DataTypes.Guids.TextstringGuid,
            "Textstring");
        var aiDisclosureSourceDataType = await GetRequiredDataTypeAsync(
            Umbraco.Cms.Core.Constants.DataTypes.Guids.LabelStringGuid,
            "Label (string)");

        var imageGroup = imageMediaType.PropertyGroups.FirstOrDefault(group =>
            group.PropertyTypes?.Any(property => property.Alias == Constants.SourcePropertyAlias) is true);
        var changed = AddPropertyIfMissing(
            imageMediaType,
            imageGroup,
            aiDisclosureDataType,
            Constants.AiDisclosurePropertyAlias,
            "AI disclosure",
            "Fully AI-generated or partially AI-modified, detected from valid C2PA metadata. Editors can override this value.");
        changed |= AddPropertyIfMissing(
            imageMediaType,
            imageGroup,
            aiGeneratorDataType,
            Constants.AiGeneratorPropertyAlias,
            "AI generator",
            "Software agent named by the C2PA manifest, when available.");
        changed |= AddPropertyIfMissing(
            imageMediaType,
            imageGroup,
            aiDisclosureSourceDataType,
            Constants.AiDisclosureSourcePropertyAlias,
            "AI disclosure source",
            "C2PA when detected from Content Credentials, or Manual after an editor override.");

        if (changed)
            await _mediaTypeService.UpdateAsync(imageMediaType, MigrationUserKey);
    }

    private async Task<IDataType> GetRequiredDataTypeAsync(Guid key, string name)
    {
        return await _dataTypeService.GetAsync(key)
            ?? throw new InvalidOperationException($"The built-in Umbraco {name} data type was not found.");
    }

    private async Task<IDataType> GetOrCreateDisclosureDataTypeAsync()
    {
        var existing = await _dataTypeService.GetAsync(AiDisclosureSchema.DataTypeKey);
        if (existing is not null)
        {
            AiImageDisclosureSchemaGuard.EnsureDisclosureDataTypeIsCompatible(existing);
            return existing;
        }

        if (!_propertyEditors.TryGet(Constants.DropDownPropertyEditorAlias, out IDataEditor? editor) || editor is null)
        {
            throw new InvalidOperationException(
                $"The Umbraco editor {Constants.DropDownPropertyEditorAlias} is not available.");
        }

        var dataType = new DataType(editor, _configurationSerializer, -1)
        {
            Key = AiDisclosureSchema.DataTypeKey,
            Name = Constants.AiDisclosureDataTypeName,
            EditorUiAlias = Constants.DropDownPropertyEditorUiAlias,
            ConfigurationData = new Dictionary<string, object>
            {
                ["multiple"] = false,
                ["items"] = new[]
                {
                    Constants.GeneratedDisclosureValue,
                    Constants.ModifiedDisclosureValue,
                },
            },
        };

        await _dataTypeService.CreateAsync(dataType, MigrationUserKey);
        return dataType;
    }

    private bool AddPropertyIfMissing(
        IMediaType imageMediaType,
        PropertyGroup? imageGroup,
        IDataType dataType,
        string alias,
        string name,
        string description)
    {
        var existingProperty = imageMediaType.PropertyTypes.FirstOrDefault(property => property.Alias == alias);
        AiImageDisclosureSchemaGuard.EnsurePropertyUsesDataType(existingProperty, dataType.Key, alias);
        if (existingProperty is not null) return false;

        var property = new PropertyType(_shortStringHelper, dataType, alias)
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
