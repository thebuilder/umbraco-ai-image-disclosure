---
title: Operations
description: Review detection limits, supported platforms, and safe failure behavior.
seo:
  image: /og/operations.png
---

## Detection limits

Detection is intentionally bounded. The following inputs remain undetermined and can be classified manually:

- Images larger than 64 MiB
- Extracted manifest JSON larger than 4 MiB
- Manifest stores with more than 1,024 manifests

Detection runs synchronously while a new image file or replacement is saved, so C2PA parsing can add processing time to that request. Existing media is not scanned or queued in the background, and saving unrelated fields does not trigger detection.

AI Image Disclosure reads embedded Content Credentials only. Remote manifest fetching is disabled, the network host allow-list is empty, and the reader uses a deny-all HTTP resolver. Processing an uploaded image does not make outbound network requests.

Unreadable or invalid data never blocks a media save. A replacement without usable positive AI evidence clears previous automatic values; existing manual values are preserved.

## Schema lifecycle

Installation adds owned data types and properties to Umbraco's default `Image` media type. Existing aliases with incompatible definitions stop the migration so the package does not overwrite another schema.

Removing the NuGet package does not remove the data types, properties, or stored values. This is intentional: uninstalling code should not silently delete editorial data. Remove that schema manually only after confirming it is no longer needed.

## Native platform support

The pinned `ContentAuthenticity` dependency bundles native C2PA binaries for:

- Windows x64 and Arm64
- Linux x64 and Arm64
- macOS Arm64

Intel macOS is not included by that dependency release. The native binaries also make restore and publish output larger than a managed-only package.

## Compatibility status

The package remains preview. CI runs backend tests against Umbraco 17.1, the latest 17.x, and the latest 18.x. The included example application builds and has been browser-smoke-tested on Umbraco 18.1.1. Backoffice badges use Umbraco's native flag and sign extension points. Umbraco's current Media Grid cards do not host entity signs, so the package does not add a badge there or replace the collection view.

## Troubleshooting

If an expected classification is missing:

1. Confirm the item uses the media type alias `Image`.
2. Upload a new file or replace `umbracoFile`; saving other fields is insufficient.
3. Test with the [known-good C2PA image](/test-assets/openai-generated-c2pa.png).
4. Inspect `aiDisclosure` and `aiDisclosureSource` on the media item.
5. Confirm the image still contains its original Content Credentials. Re-encoding often removes them.
6. Check application logs for a detection warning.
7. Use a manual value only when reliable file-level evidence is unavailable.
