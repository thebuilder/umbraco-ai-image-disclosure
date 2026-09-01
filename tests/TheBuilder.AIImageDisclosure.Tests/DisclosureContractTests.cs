namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class DisclosureContractTests
{
    [Fact]
    public void UsesStableMachineReadableValues()
    {
        Assert.Equal("generated", Constants.GeneratedDisclosureValue);
        Assert.Equal("modified", Constants.ModifiedDisclosureValue);
    }
}
