---
title: Delivery API
description: Request and render AI disclosure properties through Umbraco's media Delivery API.
seo:
  image: /og/delivery-api.png
---

When Umbraco's media Delivery API is enabled, request the stored properties with the media item:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?expand=properties[$all]&fields=properties[aiDisclosure,aiGenerator,aiDisclosureSource]
```

A classified response contains values like:

```json
{
  "aiDisclosure": "generated",
  "aiGenerator": "gpt-image",
  "aiDisclosureSource": "C2PA"
}
```

## Rendering rules

- Render the generated label when `aiDisclosure === "generated"`.
- Render the modified label when `aiDisclosure === "modified"`.
- Treat an empty or missing value as not determined.
- Do not infer the label from `aiGenerator` or `aiDisclosureSource`.

The package adds badges to the Umbraco backoffice only. Your public website remains in control of placement, wording, and the official label asset it renders.
