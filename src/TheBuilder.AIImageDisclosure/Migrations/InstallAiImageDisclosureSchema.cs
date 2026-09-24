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
    private readonly AiImageDisclosureDataTypeProvider _dataTypes;
    private readonly IMediaTypeService _mediaTypeService;
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
        _dataTypes = new AiImageDisclosureDataTypeProvider(dataTypeService, propertyEditors, configurationSerializer);
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
    }

    protected override async Task MigrateAsync()
    {
        var imageMediaType = _mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)
            ?? throw new InvalidOperationException("The default Umbraco Image media type was not found.");

        var aiDisclosureDataType = await _dataTypes.GetDisclosureAsync();
        var aiGeneratorDataType = await _dataTypes.GetRequiredAsync(
            Umbraco.Cms.Core.Constants.DataTypes.Guids.LabelStringGuid,
            "Label (string)");
        var aiDisclosureSourceDataType = await _dataTypes.GetDisclosureSourceAsync();

        var changed = AiDisclosureSchema.AddPropertyIfMissing(
            imageMediaType,
            _shortStringHelper,
            aiDisclosureDataType,
            Constants.AiDisclosurePropertyAlias,
            "AI disclosure",
            "Fully AI-generated or partially AI-modified, detected from valid C2PA metadata. Editors can override this value.");
        changed |= AiDisclosureSchema.AddPropertyIfMissing(
            imageMediaType,
            _shortStringHelper,
            aiGeneratorDataType,
            Constants.AiGeneratorPropertyAlias,
            "AI generator",
            Constants.AiGeneratorPropertyDescription);
        changed |= AiDisclosureSchema.AddPropertyIfMissing(
            imageMediaType,
            _shortStringHelper,
            aiDisclosureSourceDataType,
            Constants.AiDisclosureSourcePropertyAlias,
            "AI disclosure source",
            "C2PA when detected automatically, Manual after an editor override, or choose Resume automatic detection to reprocess the current file.");
        changed |= AiDisclosureSchema.AddPropertyIfMissing(
            imageMediaType,
            _shortStringHelper,
            aiGeneratorDataType,
            Constants.AiDisclosureReasonPropertyAlias,
            "AI disclosure reason",
            Constants.AiDisclosureReasonPropertyDescription);

        if (changed)
            await _mediaTypeService.UpdateAsync(imageMediaType, MigrationUserKey);
    }

}
