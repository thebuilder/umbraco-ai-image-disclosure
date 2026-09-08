---
title: Quickstart
description: Install AI Image Disclosure and classify your first Umbraco image.
seo:
  image: /og/quickstart.png
---

## Requirements

- Umbraco CMS 17.1 through 18.x
- .NET 10
- A supported native platform listed under [Operations](/operations)
- Umbraco's default media type with the exact alias `Image`

> **Package scope**
>
> - Processes new uploads and file replacements only.
> - Does not backfill existing media.
> - Uses Umbraco's native signs without replacing its Media collection views.
> - Does not add disclosure labels to public pages.

Installation stops rather than overwriting an existing data type or media property that uses one of the package's aliases with an incompatible definition.

## Install

```sh
dotnet add package TheBuilder.AIImageDisclosure
```

Restart the application. On first startup, the package creates the **AI image disclosure** and **AI image disclosure source** data types and adds three properties to the default Image media type.

## Test an image

1. Open the **Media** section.
2. [Download the known-good C2PA test image](/test-assets/openai-generated-c2pa.png).
3. Upload the image in the Media section. It must use Umbraco's default `Image` media type.
4. Save the media item.
5. Confirm that **AI disclosure**, **AI generator**, and **AI disclosure source** contain the expected values.

The expected values are:

```text
aiDisclosure: generated
aiGenerator: gpt-image
aiDisclosureSource: C2PA
```

Detection runs when an image file is uploaded or replaced. A detection failure never blocks the media save.

AI-generated and AI-modified images also receive a native Umbraco sign in the Media tree. Signs are enabled by default. Set `TheBuilder:AIImageDisclosure:ShowBackofficeBadges` to `false` to hide them without disabling detection. Umbraco's current Media Grid cards do not render entity signs, so the package leaves those cards unchanged.

![Detected AI disclosure properties on an Umbraco Image media item](./screenshots/media-details.png)

## Set a manual value

Select `generated` or `modified` in **AI disclosure**, then save. The source changes to `Manual` so later file processing does not silently replace the editor's decision.

Clearing **AI disclosure** also clears **AI generator** and records a manual, undetermined result. It does not mean the image is human-made.

## Resume automatic detection

Choose **Resume automatic detection** in **AI disclosure source**, then save. The package immediately reprocesses the current file and replaces the manual values with the detected result, or clears them when the file contains no usable AI evidence. Replacing the image file is not required.
