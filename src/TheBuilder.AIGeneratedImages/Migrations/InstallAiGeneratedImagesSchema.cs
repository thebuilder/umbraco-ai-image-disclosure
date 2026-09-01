using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace TheBuilder.AIGeneratedImages.Migrations;

internal sealed class InstallAiGeneratedImagesSchema : AsyncPackageMigrationBase
{
    private static readonly Guid MigrationUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;
    private readonly IDataTypeService _dataTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _configurationSerializer;
    private readonly IShortStringHelper _shortStringHelper;

    public InstallAiGeneratedImagesSchema(
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

        var aiGeneratedDataType = await EnsureDataTypeAsync(
            imageMediaType,
            Constants.AiGeneratedPropertyAlias,
            Constants.AiGeneratedDataTypeKey,
            "AI generated",
            Constants.BooleanPropertyEditorAlias,
            Constants.BooleanPropertyEditorUiAlias);
        var aiGeneratorDataType = await EnsureDataTypeAsync(
            imageMediaType,
            Constants.AiGeneratorPropertyAlias,
            Constants.AiGeneratorDataTypeKey,
            "AI generator",
            Constants.TextPropertyEditorAlias,
            Constants.TextPropertyEditorUiAlias);

        var imageGroup = imageMediaType.PropertyGroups.FirstOrDefault(group =>
            group.PropertyTypes?.Any(property => property.Alias == Constants.SourcePropertyAlias) is true);
        var changed = AddPropertyIfMissing(
            imageMediaType,
            imageGroup,
            aiGeneratedDataType,
            Constants.AiGeneratedPropertyAlias,
            "AI generated",
            "Detected from valid C2PA metadata. Editors can override this value when metadata is absent.");
        changed |= AddPropertyIfMissing(
            imageMediaType,
            imageGroup,
            aiGeneratorDataType,
            Constants.AiGeneratorPropertyAlias,
            "AI generator",
            "Software agent named by the C2PA manifest, when available.");

        if (changed)
            await _mediaTypeService.UpdateAsync(imageMediaType, MigrationUserKey);
    }

    private async Task<IDataType> EnsureDataTypeAsync(
        IMediaType imageMediaType,
        string propertyAlias,
        Guid dataTypeKey,
        string dataTypeName,
        string editorAlias,
        string editorUiAlias)
    {
        var ownedDataType = await _dataTypeService.GetAsync(dataTypeKey);
        var property = imageMediaType.PropertyTypes.FirstOrDefault(item => item.Alias == propertyAlias);
        var propertyDataType = property is null ? null : await _dataTypeService.GetAsync(property.DataTypeKey);

        AiGeneratedImagesSchemaGuard.EnsureCompatible(
            propertyAlias,
            dataTypeKey,
            editorAlias,
            editorUiAlias,
            ownedDataType,
            property,
            propertyDataType);

        if ((propertyDataType ?? ownedDataType) is { } existing) return existing;
        if (!_propertyEditors.TryGet(editorAlias, out IDataEditor? editor) || editor is null)
            throw new InvalidOperationException($"The Umbraco editor {editorAlias} is not available.");

        var dataType = new DataType(editor, _configurationSerializer, -1)
        {
            Key = dataTypeKey,
            Name = dataTypeName,
            EditorUiAlias = editorUiAlias,
            ConfigurationData = new Dictionary<string, object>(),
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
        if (imageMediaType.PropertyTypes.Any(property => property.Alias == alias)) return false;

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
