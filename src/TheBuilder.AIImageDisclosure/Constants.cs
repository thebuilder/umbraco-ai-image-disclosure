namespace TheBuilder.AIImageDisclosure;

internal static class Constants
{
    internal const string AiWatermarkPropertyAlias = "aiWatermark";
    internal const string OpenAiWatermarkDetected = "OpenAI SynthID detected";
    internal const string WatermarkFlagAlias = "TheBuilder.AIImageDisclosure.Watermark";
    internal const string AiWatermarkPropertyDescription = "Read-only watermark evidence. A detected watermark does not distinguish full AI generation from partial editing.";

    public const string PackageName = "TheBuilder.AIImageDisclosure";
    public const string ShowBackofficeBadgesSetting = "TheBuilder:AIImageDisclosure:ShowBackofficeBadges";
    public const string GeneratedFlagAlias = PackageName + ".Generated";
    public const string ModifiedFlagAlias = PackageName + ".Modified";
    public const string DefaultImageMediaTypeAlias = "Image";
    public const string SourcePropertyAlias = "umbracoFile";
    public const string AiDisclosurePropertyAlias = "aiDisclosure";
    public const string AiGeneratorPropertyAlias = "aiGenerator";
    public const string AiDisclosureSourcePropertyAlias = "aiDisclosureSource";
    public const string AiDisclosureReasonPropertyAlias = "aiDisclosureReason";
    public const string AiDisclosureDataTypeName = "AI image disclosure";
    public const string AiDisclosureSourceDataTypeName = "AI image disclosure source";
    public const string AiGeneratorPropertyDescription =
        "Read-only software agent attached to the AI-relevant C2PA action, when provided by the credential.";
    public const string DropDownPropertyEditorAlias = "Umbraco.DropDown.Flexible";
    public const string DropDownPropertyEditorUiAlias = "Umb.PropertyEditorUi.Dropdown";
    public const string GeneratedDisclosureValue = "generated";
    public const string ModifiedDisclosureValue = "modified";
    public const string C2paDisclosureSourceValue = "C2PA";
    public const string ManualDisclosureSourceValue = "Manual";
    public const string ResumeAutomaticDisclosureSourceValue = "Resume automatic detection";
    public const string AiDisclosureReasonPropertyDescription =
        "Read-only reason for the latest automatic detection result, when no AI disclosure was found.";
}
