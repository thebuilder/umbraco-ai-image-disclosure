using Microsoft.Extensions.Logging;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Detection;
using TheBuilder.AIImageDisclosure.Media;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class WatermarkFallbackTests
{
    [Theory]
    [InlineData((int)AiImageDetectionReason.NoContentCredentials, true)]
    [InlineData((int)AiImageDetectionReason.InvalidCredentials, false)]
    [InlineData((int)AiImageDetectionReason.NoAiDeclaration, false)]
    [InlineData((int)AiImageDetectionReason.ImageTooLarge, false)]
    [InlineData((int)AiImageDetectionReason.ManifestLimitExceeded, false)]
    [InlineData((int)AiImageDetectionReason.Unreadable, false)]
    public async Task OnlyMissingCredentialsAllowAnExternalCheck(int reasonValue, bool expectedCheck)
    {
        var reason = (AiImageDetectionReason)reasonValue;
        var result = new AiImageMetadata(AiImageDetectionStatus.NotDetected, Reason: reason);
        var (processor, media, verifier, values) = Create(result);

        await processor.InspectAsync(media, TestContext.Current.CancellationToken);

        await verifier.Received(expectedCheck ? 1 : 0).VerifyAsync(
            Arg.Any<Func<Stream>>(), "image/png", TestContext.Current.CancellationToken);
        Assert.Equal(string.Empty, values[Constants.AiDisclosurePropertyAlias]);
        Assert.Equal(expectedCheck ? Constants.OpenAiWatermarkDetected : string.Empty,
            values[Constants.AiWatermarkPropertyAlias]);
    }

    [Theory]
    [InlineData("Manual")]
    [InlineData("[\"Manual\"]")]
    public async Task PreservesManualChoiceAndClearsStaleWatermarkWhenFileChanges(string manualSource)
    {
        var (processor, media, verifier, values) = Create(AiImageMetadata.NoContentCredentials);
        values[Constants.AiDisclosureSourcePropertyAlias] = manualSource;
        values[Constants.AiDisclosurePropertyAlias] = "modified";
        values[Constants.AiWatermarkPropertyAlias] = Constants.OpenAiWatermarkDetected;

        await processor.InspectAsync(media, TestContext.Current.CancellationToken);

        await verifier.DidNotReceiveWithAnyArgs().VerifyAsync(default!, default!, TestContext.Current.CancellationToken);
        Assert.Equal("Manual", values[Constants.AiDisclosureSourcePropertyAlias]);
        Assert.Equal("modified", values[Constants.AiDisclosurePropertyAlias]);
        Assert.Equal(string.Empty, values[Constants.AiWatermarkPropertyAlias]);
    }

    [Fact]
    public async Task ResumeCanCheckWatermarksAfterLeavingManualMode()
    {
        var (processor, media, verifier, values) = Create(AiImageMetadata.NoContentCredentials);
        values[Constants.AiDisclosureSourcePropertyAlias] = "Manual";
        await processor.ResumeAutomaticDetectionAsync(media, TestContext.Current.CancellationToken);
        await verifier.Received(1).VerifyAsync(Arg.Any<Func<Stream>>(), "image/png", TestContext.Current.CancellationToken);
        Assert.Equal(Constants.OpenAiWatermarkDetected, values[Constants.AiWatermarkPropertyAlias]);
        Assert.Equal(string.Empty, values[Constants.AiDisclosurePropertyAlias]);
    }

    [Fact]
    public async Task ExternalFailureDoesNotFailMediaProcessingOrClaimNegativeEvidence()
    {
        var (processor, media, verifier, values) = Create(AiImageMetadata.NoContentCredentials);
        verifier.VerifyAsync(Arg.Any<Func<Stream>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ImageWatermarkResult>>(_ => throw new HttpRequestException("private response"));
        await processor.InspectAsync(media, TestContext.Current.CancellationToken);
        Assert.Equal("Watermark check unavailable", values[Constants.AiWatermarkPropertyAlias]);
        Assert.Equal(string.Empty, values[Constants.AiDisclosurePropertyAlias]);
    }

    [Fact]
    public async Task DoesNotApplyResultWhenFileChangesDuringVerification()
    {
        var (processor, media, verifier, values) = Create(AiImageMetadata.NoContentCredentials);
        verifier.VerifyAsync(Arg.Any<Func<Stream>>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            values[Constants.SourcePropertyAlias] = "/media/replacement.png";
            return Task.FromResult(new ImageWatermarkResult(ImageWatermarkStatus.Detected, new ImageWatermarkProvider("OpenAI", "SynthID")));
        });
        await processor.InspectAsync(media, TestContext.Current.CancellationToken);
        Assert.Equal(string.Empty, values[Constants.AiWatermarkPropertyAlias]);
    }

    [Fact]
    public async Task DoesNotApplyFailedCheckToAReplacementFile()
    {
        var (processor, media, verifier, values) = Create(AiImageMetadata.NoContentCredentials);
        verifier.VerifyAsync(Arg.Any<Func<Stream>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ImageWatermarkResult>>(_ =>
            {
                values[Constants.SourcePropertyAlias] = "/media/replacement.png";
                throw new HttpRequestException();
            });
        await processor.InspectAsync(media, TestContext.Current.CancellationToken);
        Assert.Equal(string.Empty, values[Constants.AiWatermarkPropertyAlias]);
    }

    [Fact]
    public async Task DisabledVerifierDoesNotReopenMediaOrProduceUnavailableEvidence()
    {
        var opened = 0;
        var (processor, media, _, values) = Create(AiImageMetadata.NoContentCredentials,
            new DisabledImageWatermarkVerifier(), () => opened++);
        await processor.InspectAsync(media, TestContext.Current.CancellationToken);
        Assert.Equal(1, opened); // Only local C2PA inspection opens storage.
        Assert.Equal(string.Empty, values[Constants.AiWatermarkPropertyAlias]);
    }

    [Fact]
    public async Task StoresTheActualProviderInsteadOfAttributingAllEvidenceToOpenAi()
    {
        var (processor, media, verifier, values) = Create(AiImageMetadata.NoContentCredentials);
        verifier.VerifyAsync(Arg.Any<Func<Stream>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ImageWatermarkResult(ImageWatermarkStatus.Detected, new ImageWatermarkProvider("Example", "TestMark")));
        await processor.InspectAsync(media, TestContext.Current.CancellationToken);
        Assert.Equal("Example TestMark detected", values[Constants.AiWatermarkPropertyAlias]);
    }

    private static (MediaAiMetadataProcessor Processor, IMedia Media, IImageWatermarkVerifier Verifier, Dictionary<string, string> Values)
        Create(AiImageMetadata result, IImageWatermarkVerifier? actualVerifier = null, Action? onOpen = null)
    {
        var media = Substitute.For<IMedia>();
        var values = new Dictionary<string, string> { [Constants.SourcePropertyAlias] = "/media/test.png" };
        media.GetValue<string>(Arg.Any<string>()).Returns(call => values.GetValueOrDefault(call.ArgAt<string>(0)));
        media.When(item => item.SetValue(Arg.Any<string>(), Arg.Any<object?>())).Do(call =>
            values[call.ArgAt<string>(0)] = call.ArgAt<object?>(1)?.ToString() ?? string.Empty);
        media.HasProperty(Constants.AiWatermarkPropertyAlias).Returns(true);
        var propertyType = Substitute.For<IPropertyType>();
        propertyType.Alias.Returns(Constants.SourcePropertyAlias);
        propertyType.PropertyEditorAlias.Returns("Umbraco.ImageCropper");
        var property = Substitute.For<IProperty>();
        property.Alias.Returns(Constants.SourcePropertyAlias);
        property.PropertyType.Returns(propertyType);
        property.GetValue().Returns("/media/test.png");
        var properties = new PropertyCollection([property]);
        media.Properties.Returns(properties);
        var generator = Substitute.For<IMediaUrlGenerator>();
        generator.TryGetMediaPath(Arg.Any<string>(), Arg.Any<object>(), out Arg.Any<string?>())
            .Returns(call => { call[2] = "/media/test.png"; return true; });
        var reader = Substitute.For<IImageAiMetadataReader>();
        reader.Read(Arg.Any<Stream>(), Arg.Any<string>()).Returns(result);
        var service = Substitute.For<IMediaService>();
        service.GetMediaFileContentStream(Arg.Any<string>()).Returns(_ => { onOpen?.Invoke(); return new MemoryStream([1, 2, 3]); });
        var verifier = Substitute.For<IImageWatermarkVerifier>();
        verifier.VerifyAsync(Arg.Any<Func<Stream>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ImageWatermarkResult(ImageWatermarkStatus.Detected, new ImageWatermarkProvider("OpenAI", "SynthID")));
        return (new MediaAiMetadataProcessor(reader, service, new MediaUrlGeneratorCollection(() => [generator]),
            Substitute.For<ILogger<MediaAiMetadataProcessor>>(), actualVerifier ?? verifier), media, verifier, values);
    }
}
