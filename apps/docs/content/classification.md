---
title: Classification
description: See how valid C2PA evidence maps to generated, modified, or unknown.
seo:
  image: /og/classification.png
---

The package uses C2PA claims only when the manifest validation state is `Valid` or `Trusted`. The detector follows the active manifest and parent ingredients.

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

The package does not use a visual AI classifier or unsigned XMP and EXIF tags to decide whether an image is AI-generated. It reads signed claims and, if you install and enable the OpenAI integration, checks for a supported watermark.

Neither check guarantees that every AI-generated image will be identified. A valid signature does not guarantee that every claim is true. Missing credentials and negative watermark results leave the origin unknown.

The [optional OpenAI watermark check](/openai-watermarks) checks OpenAI SynthID watermarks only when no C2PA metadata is present. It records separate `aiWatermark` evidence and never guesses `generated` versus `modified` from a watermark alone.
