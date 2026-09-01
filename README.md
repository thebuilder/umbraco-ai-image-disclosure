# AI Image Disclosure for Umbraco

AI Image Disclosure classifies Umbraco Image media from signed C2PA Content Credentials. It adds three properties to the default Image media type:

- `aiDisclosure`: empty when unknown, `generated`, or `modified`.
- `aiGenerator`: the software agent named by the manifest, when available.
- `aiDisclosureSource`: read-only `C2PA` or `Manual`, so a manual override survives later file replacements.

The wording follows the European Commission's voluntary labels for fully AI-generated and partially AI-modified content. This package supplies metadata that a site can use when rendering a label; it does not add an icon to the public website.

## Backoffice badges

The Media section's grid view overlays the matching European Commission badge on Image thumbnails whose `aiDisclosure` value is `generated` or `modified`. Undetermined images and other media remain unchanged. The badge is informational and does not alter thumbnail selection, navigation, or media actions.

The package intentionally limits this integration to the supported Media collection view. Media Picker dialogs use separate internal components and are left unchanged.

## Install

The package supports Umbraco CMS 17.1 through 18.x on .NET 10.

```sh
dotnet add package TheBuilder.AIImageDisclosure
```

Restart the Umbraco application after installing. The package creates an **AI image disclosure** dropdown and adds all three properties to the default Image media type. Uploading or replacing an image runs detection when that media item is saved.

Editors can change either property manually. This matters because removing metadata is easy and many AI tools do not emit Content Credentials.

## Classification policy

Only C2PA manifests whose validation state is `Valid` or `Trusted` are considered. The package follows the active manifest and its parent ingredients, then applies these rules:

| C2PA evidence | Stored disclosure |
| --- | --- |
| `c2pa.created` with IPTC `trainedAlgorithmicMedia` | `generated` |
| `c2pa.edited` with an AI source type | `modified` |
| IPTC `compositeWithTrainedAlgorithmicMedia` | `modified` |
| No usable manifest or no positive AI evidence | Empty (unknown) |
| Invalid or unreadable C2PA data | Existing manual values are preserved |

Composite evidence takes precedence. When a manifest says the image was created entirely by AI and then edited by AI without a composite source, it remains fully AI-generated; an edit signal without that creation evidence is partially AI-modified. The parser also recognizes the `compositedWithTrainedAlgorithmicMedia` spelling used in some C2PA guidance. A missing disclosure never means that an image is human-made.

Detection is intentionally bounded: images over 64 MiB, extracted manifest JSON over 4 MiB, and manifest stores over 1,024 claims are left undetermined for manual classification. Detection failures never block the media save.

## Delivery API

When Umbraco's media Delivery API is enabled, request the properties as normal media properties:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?expand=properties[$all]&fields=properties[aiDisclosure,aiGenerator,aiDisclosureSource]
```

Render your public label from `aiDisclosure`; treat an empty value as “not determined,” not “not AI.”

## Other detection methods

C2PA is the primary signal because it can carry signed provenance and a standardized digital source type. Useful future complements are:

- C2PA 2.4 `c2pa.ai-disclosure` assertions for more detailed model and training disclosures.
- Vendor watermark detectors such as SynthID when a supported provider exposes a verification service.
- XMP/EXIF software tags as a weak hint only; they are unsigned and trivial to edit.

Pixel-based “AI detector” models should not automatically set the disclosure. Their false-positive and false-negative behavior makes them better suited to an editor warning or review queue.

## Platform note

The pinned `ContentAuthenticity` dependency bundles native C2PA binaries for Windows x64/Arm64, Linux x64/Arm64, and macOS Arm64. Intel macOS is not included by that dependency release. Restoring the native dependency and publishing the host therefore uses substantially more disk space than a typical managed-only Umbraco package.

## Development

```sh
dotnet test TheBuilder.AIImageDisclosure.slnx
dotnet build samples/TheBuilder.AIImageDisclosure.Example/TheBuilder.AIImageDisclosure.Example.csproj
dotnet pack src/TheBuilder.AIImageDisclosure/TheBuilder.AIImageDisclosure.csproj --configuration Release
```

The integration suite contains a freshly generated OpenAI PNG with embedded Content Credentials and a supplied Google manifest chain.

## License

MIT. See [LICENSE](LICENSE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
