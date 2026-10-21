using TheBuilder.AIImageDisclosure.Media;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class DropdownValueTests
{
    [Fact]
    public void EncodesTheSelectionAsAJsonArrayLikeUmbracosDropdown() =>
        Assert.Equal("[\"generated\"]", DropdownValue.Encode(Constants.GeneratedDisclosureValue));

    [Theory]
    [InlineData("[\"generated\"]", "generated")]
    [InlineData("generated", "generated")]
    [InlineData("[\"Resume automatic detection\"]", "Resume automatic detection")]
    [InlineData("[]", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void DecodesBothTheJsonAndTheLegacyPlainForm(string? stored, string? expected) =>
        Assert.Equal(expected, DropdownValue.Decode(stored));

    [Fact]
    public void RoundTripsEveryPackageValue()
    {
        foreach (string value in new[]
                 {
                     Constants.GeneratedDisclosureValue,
                     Constants.ModifiedDisclosureValue,
                     Constants.C2paDisclosureSourceValue,
                     Constants.ManualDisclosureSourceValue,
                     Constants.ResumeAutomaticDisclosureSourceValue,
                 })
        {
            Assert.Equal(value, DropdownValue.Decode(DropdownValue.Encode(value)));
        }
    }
}
