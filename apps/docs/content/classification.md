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

The parser also recognizes `compositedWithTrainedAlgorithmicMedia`, a spelling found in some C2PA guidance. It reads `c2pa.metadata` with namespaced `Iptc4xmpExt:DigitalSourceType`, plus the legacy `stds.iptc` and `stds.iptc.photometadata` labels. A `componentOf` AI ingredient marks the result `modified` only when the component is placed in the resulting claim and not subsequently removed. The reference is resolved within its owning manifest; `inputTo` ingredients and unrelated history do not count.

`aiGenerator` is populated only from the `softwareAgent` on the relevant AI action. The credential's `claim_generator_info` identifies software that created the Content Credential and is not treated as the image generator.

## Replacing a file

- Valid positive AI evidence sets an automatic value and source `C2PA`.
- A valid credential without recognised AI evidence clears a previous automatic value.
- Missing or invalid credentials also clear previous automatic values.
- Manual values remain untouched until an editor chooses **Resume automatic detection**.

## Why the package does not guess

Pixel-based AI detectors can produce false positives and false negatives. Unsigned XMP or EXIF tags are easy to edit. Neither signal should silently set a compliance-facing property.

Vendor watermarks such as SynthID may become useful complementary evidence when providers expose dependable verification services. They are not currently used by the package.
