---
title: Optional OpenAI watermark check
description: Configure optional OpenAI watermark checks through app settings or an Umbraco.AI connection.
seo:
  image: /og/openai-watermarks.png
---

You can add an OpenAI watermark check to the core C2PA checks. It requires the separate `TheBuilder.AIImageDisclosure.OpenAI` package and starts disabled. An administrator must enable it before the package sends Media library images to OpenAI.

The check looks for a supported OpenAI SynthID watermark only when C2PA metadata is absent. It does not ask a model to judge whether the picture looks AI-generated. It cannot identify every AI-generated image, and a negative result does not prove that an image is human-made.

## Configure through app settings

Install the optional package. Umbraco.AI is not required:

```sh
dotnet add package TheBuilder.AIImageDisclosure.OpenAI
```

Add this section to `appsettings.json`:

```json
{
  "TheBuilder": {
    "AIImageDisclosure": {
      "OpenAI": {
        "Enabled": false,
        "ApiKey": ""
      }
    }
  }
}
```

Supply the key through your hosting environment or .NET user secrets instead of committing it to source control. The environment variable names are:

```text
TheBuilder__AIImageDisclosure__OpenAI__ApiKey
TheBuilder__AIImageDisclosure__OpenAI__Enabled
```

For local development, the user-secret key is `TheBuilder:AIImageDisclosure:OpenAI:ApiKey`. You can also set `OrganizationId` in the same configuration section if your OpenAI account requires it.

Set `Enabled` to `true` to allow eligible Media library images to be sent to OpenAI. Supplying a key alone does not enable automatic checks.

Open the **AI image disclosure** tab under **Settings** as an administrator. It shows whether a key is configured, without revealing it. The enable switch and Save button are read-only when configuration controls the check. Use **Test verification** to send the bundled sample image, even while automatic checks are disabled.

An explicit `Enabled` or `ApiKey` setting selects configuration mode and takes precedence over a saved Umbraco.AI connection. Missing or empty credentials do not cause a fallback to another key. Remove these settings to return to connection mode.

## Use an Umbraco.AI connection

Sites already using Umbraco.AI can install the separate adapter:

```sh
dotnet add package TheBuilder.AIImageDisclosure.OpenAI.UmbracoAI
```

Install the matching Umbraco.AI and Umbraco.AI.OpenAI packages for your CMS major version. The adapter requires CMS 17.5 or later with Umbraco.AI.Core 17.3 or later on CMS 17. For CMS 18, use the 18.x Umbraco.AI packages. The direct-key package supports CMS 17.1 through 18.x without Umbraco.AI.

If you used the earlier preview integration, add this adapter to keep using your saved connection. It preserves the selected connection and enable setting.

Restart Umbraco after installing. Create an active OpenAI connection in Umbraco.AI, or use one you already have. The connection must target the direct OpenAI API. Azure OpenAI and custom compatible endpoints are not supported for this check.

1. Open the **AI image disclosure** tab under **Settings** as an administrator.
2. Select an existing OpenAI connection.
3. Use **Test verification** to check the provenance endpoint with a bundled sample image. This can send the sample even while automatic checks are disabled. It never uses an image from your Media library.
4. Review the notice about sending eligible images to OpenAI.
5. Enable **OpenAI watermark fallback** and choose **Save**.

Turning the check off keeps the selected connection. To clear it, turn the check off, select the empty connection option, and save.

The adapter stores only the enabled setting and connection ID. It resolves the key on the server through Umbraco.AI. Editing or rotating the connection applies to subsequent checks. The settings response contains connection names and IDs, never keys. See [Umbraco.AI connections](https://docs.umbraco.com/ai-in-umbraco/17.latest/concepts/connections).

## Which images are checked?

The fallback runs during an automatic upload, file replacement, explicit resume of automatic detection, or administrator scan. All of these conditions must hold:

- The media uses Umbraco's default `Image` type.
- Local C2PA inspection reports **No content credentials**.
- The item is not in manual mode.
- The integration is enabled and has a usable configured key or selected connection.
- The file is PNG, JPEG, or WebP and is no larger than 50 MiB.

Valid credentials without an AI declaration, invalid credentials, unreadable files, and files beyond local inspection limits never trigger an upload. A missing disclosure alone is insufficient. Ordinary saves that do not change the image do not rerun verification.

## Read the result

The read-only `aiWatermark` property records the outcome:

| Value | Meaning |
| --- | --- |
| `OpenAI SynthID detected` | OpenAI reported a supported watermark. The backoffice shows **AI detected**. |
| `No OpenAI watermark detected` | OpenAI reported no supported watermark. The origin remains unknown. |
| `OpenAI watermark check unavailable` | The check could not complete, including missing credentials or endpoint access |
| `Image unsupported by OpenAI watermark check` | The image format or size is unsupported |
| Empty | No watermark check result applies |

The watermark result does not distinguish generation from editing. The package stores it separately from `aiDisclosure` and preserves manual choices. This API checks OpenAI's supported signals. It cannot detect AI use across every tool. See [OpenAI's content provenance guide](https://developers.openai.com/api/docs/guides/content-provenance).

## Privacy and failures

Enabling verification sends the original eligible image to OpenAI's `content_provenance_checks` endpoint. Review OpenAI's data terms before enabling it. This endpoint is not eligible for Zero Data Retention. C2PA inspection remains local and never fetches remote manifests.

Verification has a 10-second request timeout and limits the response size. Failures leave the classification undetermined and do not block a media save. Disabled or unavailable verification does not reopen the image for a remote check.

Rate limits temporarily pause requests for the affected connection. Testing another connection does not pause the selected one. A later failure cannot shorten an existing retry delay.

Administrator scans process one media item per request when this integration is installed, even while disabled. This limits request time. Scans track media IDs independently of batch size.

An API key alone does not guarantee access to the provenance endpoint. If **Test verification** reports unavailable, check that a key is configured and has endpoint access. If you use Umbraco.AI, also check that the connection is active and targets the direct OpenAI API. No real Media library image is needed to test the connection.
