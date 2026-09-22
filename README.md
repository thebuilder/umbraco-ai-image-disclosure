<p align="center">
  <img src="https://raw.githubusercontent.com/thebuilder/umbraco-ai-image-disclosure/refs/heads/main/icon.png" width="128" height="128" alt="AI Image Disclosure logo">
</p>

<h1 align="center">AI Image Disclosure for Umbraco</h1>

<p align="center">Classify AI-generated and AI-modified images from signed C2PA Content Credentials.</p>

<p align="center">
  <a href="https://www.nuget.org/packages/TheBuilder.AIImageDisclosure"><img src="https://img.shields.io/nuget/vpre/TheBuilder.AIImageDisclosure?style=flat-square" alt="NuGet version"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/thebuilder/umbraco-ai-image-disclosure?style=flat-square" alt="MIT license"></a>
</p>

![AI disclosure properties on an Umbraco image](apps/docs/content/screenshots/media-details.png)

AI Image Disclosure reads valid C2PA Content Credentials when a file is uploaded to, or replaced on, Umbraco's default `Image` media type. It stores a small, stable classification that editors and frontends can rely on, while preserving manual control when provenance is absent or incomplete. Existing media is not scanned automatically.

## What it adds

The package adds five properties to the default Image media type:

- `aiDisclosure`: empty when unknown, `generated`, or `modified`.
- `aiGenerator`: read-only software agent evidence from the AI-relevant C2PA action, when provided by the credential.
- `aiDisclosureSource`: `C2PA`, `Manual`, or empty, plus a backoffice action to resume automatic detection.
- `aiDisclosureReason`: read-only human-readable reason for the latest automatic result; empty after positive AI detection.
- `aiWatermark`: read-only OpenAI watermark evidence when the optional fallback is enabled.

Editors can review the classification, generator evidence, and detection source on the media item. Umbraco's native signs mark classified items in the Media tree. Public sites decide where and how to render their own label.

Detection applies only to Umbraco's default `Image` media type and only when `umbracoFile` is uploaded or replaced. The package does not replace Umbraco's Media Grid, table view, media pickers, or public pages.

## C2PA and EU disclosure

[C2PA Content Credentials](https://spec.c2pa.org/specifications/specifications/2.4/explainer/Explainer.html) provide cryptographically verifiable provenance about an image's origin, edits, tools, and use of AI. AI Image Disclosure validates that evidence before mapping it to a simple editorial value.

The European Commission provides optional icons for fully AI-generated and partially AI-modified content as part of its guidance for EU AI Act Article 50 disclosure workflows. [View the official EU icons and placement guidance](https://digital-strategy.ec.europa.eu/en/policies/eu-icons-labelling-ai-generated-content). Using an icon does not establish legal compliance by itself.

## Install

AI Image Disclosure supports Umbraco CMS 17.1 through 18.x on .NET 10.

```sh
dotnet add package TheBuilder.AIImageDisclosure
```

Restart the application after installation. The package creates the **AI image disclosure** and **AI image disclosure source** data types and adds five properties to the default Image media type. Uploading or replacing an image runs detection when that media item is saved.

Editors can override the disclosure. This matters because metadata is easy to remove and many AI tools do not emit Content Credentials. Choose **Resume automatic detection** in `aiDisclosureSource` to reprocess the current file and leave manual mode.

Backoffice signs are enabled by default. Umbraco's current Media Grid cards do not render entity signs, and this package does not replace the grid or its card renderer. To hide signs while keeping detection and the media properties active, add:

```json
{
  "TheBuilder": {
    "AIImageDisclosure": {
      "ShowBackofficeBadges": false
    }
  }
}
```

## Optional OpenAI watermark fallback

Install `TheBuilder.AIImageDisclosure.OpenAI` alongside the matching Umbraco.AI and Umbraco.AI.OpenAI packages. The integration requires CMS 17.5 or later on 17.x, or CMS 18.x, and starts disabled. Administrators configure it under **Settings → AI image disclosure**, selecting an existing active OpenAI connection. Its key stays in Umbraco.AI.

Only automatic inspections that find **No content credentials** can upload an image. Invalid credentials, existing non-AI credentials, and manual overrides do not trigger this fallback. A positive result stores `OpenAI SynthID detected` in `aiWatermark` and shows an **AI detected** sign; it does not guess `generated` versus `modified`. See the [setup and privacy notes](apps/docs/content/openai-watermarks.md).

## Classification policy

Only C2PA manifests whose validation state is `Valid` or `Trusted` are considered. The package follows the active manifest and its parent ingredients, then applies these rules:

| C2PA evidence | Stored disclosure |
| --- | --- |
| `c2pa.created` with IPTC `trainedAlgorithmicMedia` | `generated` |
| `c2pa.edited` with an AI source type | `modified` |
| IPTC `compositeWithTrainedAlgorithmicMedia` | `modified` |
| No usable manifest or no positive AI evidence | Empty, meaning unknown |
| Invalid or unreadable C2PA data | Existing manual values are preserved |

Composite evidence takes precedence. An image created entirely by AI and then edited without a composite source remains `generated`; an AI edit signal without that creation evidence is `modified`. The parser also recognizes the `compositedWithTrainedAlgorithmicMedia` spelling used in some C2PA guidance.

A missing disclosure never means that an image is human-made.

The detector also reads validated `c2pa.metadata` declarations using namespaced `Iptc4xmpExt:DigitalSourceType`, plus the legacy `stds.iptc` and `stds.iptc.photometadata` assertion labels. A placed AI component is `modified`; a removed component does not count, and `inputTo` ingredients are ignored.

Detection is bounded: images over 64 MiB, extracted manifest JSON over 4 MiB, and manifest stores over 1,024 manifests are left undetermined for manual classification. Detection failures never block a media save.

Core C2PA detection reads embedded Content Credentials locally and never fetches remote manifests. The optional OpenAI fallback sends eligible images to OpenAI only after an administrator enables it.

Administrators can open the **AI disclosure scan** tab in the Media section to scan existing Image media in batches of up to 50. The dashboard reports scanned, manually preserved, and failed saves, and can stop after the current batch or resume after an error. The cursor is not durable after a page reload. Installing the optional OpenAI integration reduces requests to one media entity each to bound remote verification time.

## Delivery API

When Umbraco's media Delivery API is enabled, request the properties as normal media properties:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?fields=properties[aiDisclosure,aiGenerator,aiDisclosureSource]
```

Render the public label from `aiDisclosure`. Treat an empty value as not determined, not as proof that an image is not AI-generated.

## Detection boundaries

C2PA is the primary signal because it can carry signed provenance and a standardized digital source type. Potential complementary signals include:

- C2PA 2.4 `c2pa.ai-disclosure` assertions for model identification, content profiles, human oversight, and related metadata.
- Optional [OpenAI SynthID verification](apps/docs/content/openai-watermarks.md) for images without C2PA metadata. A positive result records separate evidence because it does not distinguish generation from editing.
- XMP or EXIF software tags as a weak hint only. They are unsigned and easy to edit.

Pixel-based AI detector models should not set the disclosure automatically. Their false-positive and false-negative behavior makes them more suitable for an editor warning or review queue.

## Platform support

The pinned `ContentAuthenticity` dependency bundles native C2PA binaries for Windows x64 and Arm64, Linux x64 and Arm64, and macOS Arm64. Intel macOS is not included by that dependency release. Restoring the native dependency and publishing the host uses more disk space than a managed-only Umbraco package.

## Documentation

Read the complete documentation at [ai-image-disclosure.thebuilder.dk](https://ai-image-disclosure.thebuilder.dk/overview).

## Development

```sh
dotnet test TheBuilder.AIImageDisclosure.slnx
dotnet build samples/TheBuilder.AIImageDisclosure.Example/TheBuilder.AIImageDisclosure.Example.csproj
dotnet pack src/TheBuilder.AIImageDisclosure/TheBuilder.AIImageDisclosure.csproj --configuration Release
pnpm docs:dev
```

The integration suite contains a generated OpenAI PNG with embedded Content Credentials and a supplied Google manifest chain.

## License

MIT. See [LICENSE](LICENSE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
