---
title: Delivery API
description: Request and render AI disclosure properties through Umbraco's media Delivery API.
seo:
  image: /og/delivery-api.png
---

Umbraco's Media Delivery API is disabled by default, even when the Content Delivery API is enabled. Enable both the parent API and its media section in `appsettings.json`:

```json
{
  "Umbraco": {
    "CMS": {
      "DeliveryApi": {
        "Enabled": true,
        "Media": {
          "Enabled": true
        }
      }
    }
  }
}
```

See the [official Media Delivery API documentation](https://docs.umbraco.com/umbraco-cms/reference/content-delivery-api/media-delivery-api) for public-access and API-key options.

Request the three scalar properties with the media item:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?fields=properties[aiDisclosure,aiGenerator,aiDisclosureSource]
```

A relevant response fragment contains values like:

```json
{
  "properties": {
    "aiDisclosure": "generated",
    "aiGenerator": "gpt-image",
    "aiDisclosureSource": "C2PA"
  }
}
```

## Rendering rules

```ts
const disclosure = media.properties.aiDisclosure;

const label =
  disclosure === "generated"
    ? "Fully AI-generated"
    : disclosure === "modified"
      ? "Partially AI-modified"
      : undefined;
```

- Render from `aiDisclosure`, never from `aiGenerator` or `aiDisclosureSource`.
- Treat an empty or missing value as not determined, not proof that the image is non-AI.
- Decide through editorial policy whether undetermined images need manual review.
- Give a visible icon accompanying text or an accessible name.

The package does not expose stable frontend icon assets. Obtain the public labels from the [European Commission's official icon downloads](https://digital-strategy.ec.europa.eu/en/policies/eu-icons-labelling-ai-generated-content) rather than referencing hashed files from the backoffice bundle. Your public website remains in control of placement, wording, and accessibility.
