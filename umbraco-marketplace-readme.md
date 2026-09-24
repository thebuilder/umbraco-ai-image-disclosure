![AI disclosure properties on an Umbraco image](https://raw.githubusercontent.com/thebuilder/umbraco-ai-image-disclosure/refs/heads/main/apps/docs/content/screenshots/media-details.png)

# AI Image Disclosure

AI Image Disclosure checks signed C2PA Content Credentials for claims of AI generation or editing when you upload or replace an Umbraco Image file. Editors can review or override the result.

You can add an optional OpenAI watermark check for images without C2PA metadata. It requires a separate package and starts disabled.

Neither check guarantees that every AI-generated image will be identified. Missing credentials or a negative watermark result leave the origin unknown. The package does not guess from how an image looks.

The stored values align with the European Commission's optional labels for fully AI-generated and partially AI-modified content. [View the official EU icons and guidance](https://digital-strategy.ec.europa.eu/en/policies/eu-icons-labelling-ai-generated-content).

## Key features

- Verifies signed C2PA manifests before using their claims.
- Stores a simple `generated` or `modified` value for frontend use.
- Leaves Umbraco's native Media Grid and other collection views unchanged.
- Records the software agent attached to the relevant AI action, when available.
- Lets editors override a classification and resume automatic detection later.
- Exposes all stored values through Umbraco's media Delivery API.
- Supports optional OpenAI watermark checks for images without C2PA metadata, using an existing Umbraco.AI connection.

## Install

```sh
dotnet add package TheBuilder.AIImageDisclosure
```

The package supports Umbraco CMS 17.1 through 18.x on .NET 10.

Detection processes new uploads and file replacements only. Existing media is not scanned automatically, custom media type aliases are ignored, and public sites remain responsible for rendering an accessible disclosure.

[Read the documentation](https://ai-image-disclosure.thebuilder.dk/overview) for detection rules, editor guidance, Delivery API examples, and operational limits.
