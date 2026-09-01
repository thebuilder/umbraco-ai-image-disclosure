using Umbraco.Cms.Core.Packaging;

namespace TheBuilder.AIGeneratedImages.Migrations;

internal sealed class AiGeneratedImagesPackageMigrationPlan : PackageMigrationPlan
{
    public AiGeneratedImagesPackageMigrationPlan() : base(Constants.PackageName)
    {
    }

    protected override void DefinePlan() =>
        To<InstallAiGeneratedImagesSchema>(new Guid("a1488504-e8cf-4e22-a18b-128529ba22e7"));
}
