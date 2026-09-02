![AI disclosure badges in the Umbraco Media library](https://raw.githubusercontent.com/thebuilder/umbraco-ai-image-disclosure/refs/heads/main/apps/docs/content/screenshots/media-library-badges.png)

# AI Image Disclosure

AI Image Disclosure reads valid C2PA Content Credentials when a file is uploaded to, or replaced on, Umbraco's default `Image` media type. It stores `generated`, `modified`, or no classification, shows the result in the package-provided Media Grid, and exposes the same values through the Media Delivery API. Missing credentials remain unknown; the package does not guess from image pixels.

The stored values align with the European Commission's optional labels for fully AI-generated and partially AI-modified content. [View the official EU icons and guidance](https://digital-strategy.ec.europa.eu/en/policies/eu-icons-labelling-ai-generated-content).

## Key features

- Verifies signed C2PA manifests before using their claims.
- Stores a simple `generated` or `modified` value for frontend use.
- Shows clear badges in the package-provided Media Grid view.
- Records the software agent attached to the relevant AI action, when available.
- Lets editors override a classification and resume automatic detection later.
- Exposes all stored values through Umbraco's media Delivery API.

## Install

```sh
dotnet add package TheBuilder.AIImageDisclosure
```

The package supports Umbraco CMS 17.1 through 18.x on .NET 10.

Detection processes new uploads and file replacements only. Existing media is not scanned automatically, custom media type aliases are ignored, and public sites remain responsible for rendering an accessible disclosure.

[Read the documentation](https://ai-image-disclosure.thebuilder.dk/overview) for detection rules, editor guidance, Delivery API examples, and operational limits.
