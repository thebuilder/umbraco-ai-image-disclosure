---
title: Operations
description: Review detection limits, supported platforms, and safe failure behavior.
seo:
  image: /og/operations.png
---

## Detection limits

Detection is intentionally bounded. The following inputs remain undetermined and can be classified manually:

- Images larger than 64 MiB
- Extracted manifest JSON larger than 4 MiB
- Manifest stores with more than 1,024 claims

Unreadable or invalid data never blocks a media save. Existing manual values are preserved.

## Native platform support

The pinned `ContentAuthenticity` dependency bundles native C2PA binaries for:

- Windows x64 and Arm64
- Linux x64 and Arm64
- macOS Arm64

Intel macOS is not included by that dependency release. The native binaries also make restore and publish output larger than a managed-only package.

## Troubleshooting

If an expected badge is missing:

1. Open the media item and check whether `aiDisclosure` has a value.
2. Confirm the image still contains its original Content Credentials. Re-encoding often removes them.
3. Confirm the media item was saved after installation.
4. Check application logs for a bounded detection warning.
5. Set a manual value when reliable file-level evidence is unavailable.
