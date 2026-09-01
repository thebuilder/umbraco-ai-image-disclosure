---
title: Classification
description: See how valid C2PA evidence maps to generated, modified, or unknown.
seo:
  image: /og/classification.png
---

Only manifests whose validation state is `Valid` or `Trusted` contribute evidence. The detector follows the active manifest and parent ingredients.

| C2PA evidence | Stored value |
| --- | --- |
| `c2pa.created` with IPTC `trainedAlgorithmicMedia` | `generated` |
| `c2pa.edited` with an AI source type | `modified` |
| IPTC `compositeWithTrainedAlgorithmicMedia` | `modified` |
| No usable manifest or no positive AI evidence | Empty |
| Invalid or unreadable C2PA data | Existing manual values are preserved |

Composite evidence takes precedence. An image created entirely by AI and then edited without a composite source remains `generated`. An AI edit signal without that creation evidence is `modified`.

The parser also recognizes `compositedWithTrainedAlgorithmicMedia`, a spelling found in some C2PA guidance.

## Why the package does not guess

Pixel-based AI detectors can produce false positives and false negatives. Unsigned XMP or EXIF tags are easy to edit. Neither signal should silently set a compliance-facing property.

Vendor watermarks such as SynthID may become useful complementary evidence when providers expose dependable verification services. They are not currently used by the package.
