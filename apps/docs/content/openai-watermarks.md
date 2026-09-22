---
title: OpenAI watermark fallback
description: Configure optional OpenAI SynthID verification using an existing Umbraco.AI connection.
seo:
  image: /og/openai-watermarks.png
---

The optional integration checks for an OpenAI SynthID watermark when local inspection finds **No content credentials**. It starts disabled. An administrator must enable it before media images are sent to OpenAI.

## Install

Install the matching Umbraco.AI and OpenAI provider packages for your CMS major version, then add the integration:

```sh
dotnet add package Umbraco.AI
dotnet add package Umbraco.AI.OpenAI
dotnet add package TheBuilder.AIImageDisclosure.OpenAI
```

For CMS 17, choose the 17.x Umbraco.AI packages: the integration requires Umbraco.AI.Core 17.3 or later and CMS 17.5 or later. For CMS 18, use the 18.x Umbraco.AI packages. The base disclosure package continues to support CMS 17.1 without the integration.

Restart Umbraco after installing. Create an active OpenAI connection in Umbraco.AI, or use one you already have. The connection must target the direct OpenAI API; Azure OpenAI and custom compatible endpoints are not supported for this check.

## Configure

1. Open **Settings → AI image disclosure** as an administrator.
2. Select an existing OpenAI connection.
3. Use **Test verification** to check the actual provenance endpoint with a bundled sample image. This can send the sample even while the fallback is disabled; it never uses an image from your Media library.
4. Review the notice about sending eligible images to OpenAI.
5. Enable **OpenAI watermark fallback** and choose **Save**.

Turning the fallback off keeps the selected connection. To clear it, turn the fallback off, select the empty connection option, and save.

The integration stores only the enabled setting and connection ID. It resolves the key on the server through Umbraco.AI, including supported configuration references and sensitive-field rules. Editing or rotating the connection applies to subsequent checks. The settings response contains connection names and IDs, never keys. See [Umbraco.AI connections](https://docs.umbraco.com/ai-in-umbraco/17.latest/concepts/connections).

## Which images are checked?

The fallback runs during an automatic upload, file replacement, explicit resume of automatic detection, or administrator scan. All of these conditions must hold:

- The media uses Umbraco's default `Image` type.
- Local C2PA inspection reports **No content credentials**.
- The item is not in manual mode.
- The integration is enabled and the selected connection is usable.
- The file is PNG, JPEG, or WebP and is no larger than 50 MiB.

Valid credentials without an AI declaration, invalid credentials, unreadable files, and files beyond local inspection limits never trigger an upload. A missing disclosure alone is insufficient. Ordinary saves that do not change the image do not rerun verification.

## Read the result

The read-only `aiWatermark` property records the outcome:

| Value | Meaning |
| --- | --- |
| `OpenAI SynthID detected` | A supported OpenAI watermark was detected; the backoffice shows **AI detected** |
| `No OpenAI watermark detected` | No supported watermark was detected; provenance remains undetermined |
| `OpenAI watermark check unavailable` | The check could not complete, including missing credentials or endpoint access |
| `Image unsupported by OpenAI watermark check` | The image format or size is unsupported |
| Empty | No watermark check result applies |

A positive watermark does not distinguish fully generated images from AI edits, so it does not set `aiDisclosure` to `generated` or `modified`. Manual decisions remain authoritative. A negative result is not proof that the image is human-made. This API checks OpenAI's supported signals; it is not a general detector for all vendors' AI output. See [OpenAI's content provenance guide](https://developers.openai.com/api/docs/guides/content-provenance).

## Privacy and failures

Enabling verification sends the original eligible image to OpenAI's `content_provenance_checks` endpoint. Review the provider's applicable data terms before enabling it; this endpoint is not eligible for Zero Data Retention. C2PA inspection remains local and never fetches remote manifests.

Verification has a 10-second request timeout and a bounded response size. Failures leave the classification undetermined and do not block a media save. Rate limits temporarily pause requests for the affected connection; testing another connection does not pause the selected one. A later failure cannot shorten an existing retry delay. Disabled or unavailable verification does not reopen the image for a remote check. Administrator scans process one media entity per request when this integration is installed, including while disabled, to bound request time and keep scan page numbers consistent.

An API key alone does not guarantee access to the provenance endpoint. If **Test verification** reports unavailable, check that the connection is active, resolves a key, targets the direct OpenAI API, and has endpoint access. No real Media library image is needed to test the connection.
