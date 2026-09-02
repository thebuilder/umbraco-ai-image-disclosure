![AI disclosure badges in the Umbraco Media library](https://raw.githubusercontent.com/thebuilder/umbraco-ai-image-disclosure/refs/heads/main/apps/docs/content/screenshots/media-library-badges.png)

# AI Image Disclosure

AI Image Disclosure helps editors understand how an image was made. It reads signed C2PA Content Credentials when an Image is saved, classifies the result as fully AI-generated or partially AI-modified, and shows the matching disclosure badge in the Media library.

The stored values align with the European Commission's optional labels for fully AI-generated and partially AI-modified content. [View the official EU icons and guidance](https://digital-strategy.ec.europa.eu/en/policies/eu-icons-labelling-ai-generated-content).

## Key features

- Verifies signed C2PA manifests before using their claims.
- Stores a simple `generated` or `modified` value for frontend use.
- Shows clear badges in the supported Media collection view.
- Records the named generator when the manifest provides one.
- Lets editors override an unknown or incorrect classification.
- Exposes all stored values through Umbraco's media Delivery API.

## Install

```sh
dotnet add package TheBuilder.AIImageDisclosure
```

The package supports Umbraco CMS 17.1 through 18.x on .NET 10.

[Read the documentation](https://ai-image-disclosure.thebuilder.dk/overview) for detection rules, editor guidance, Delivery API examples, and operational limits.
