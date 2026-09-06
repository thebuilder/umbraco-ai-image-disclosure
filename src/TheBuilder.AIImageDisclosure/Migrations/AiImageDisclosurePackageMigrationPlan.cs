using Umbraco.Cms.Core.Packaging;

namespace TheBuilder.AIImageDisclosure.Migrations;

internal sealed class AiImageDisclosurePackageMigrationPlan : PackageMigrationPlan
{
    public AiImageDisclosurePackageMigrationPlan() : base(Constants.PackageName)
    {
    }

    protected override void DefinePlan()
    {
        To<InstallAiImageDisclosureSchema>(new Guid("a1488504-e8cf-4e22-a18b-128529ba22e7"));
        To<UpgradePreviewAiImageDisclosureSchema>(new Guid("72a663b7-ff33-47b5-96e8-25cfbe4d5d53"));
        To<AddAiDisclosureReasonSchema>(new Guid("db1c4fbf-7d9f-4d0a-9747-9473ad858f91"));
    }
}
