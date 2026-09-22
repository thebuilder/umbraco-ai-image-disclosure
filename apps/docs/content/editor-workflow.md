---
title: Editor workflow
description: Review, override, and resume automatic AI image disclosure in Umbraco.
seo:
  image: /og/editor-workflow.png
---

## Read the evidence

Uploading or replacing `umbracoFile` on Umbraco's default `Image` media type runs C2PA detection during the save. Positive evidence stores `generated` or `modified`, sets the source to `C2PA`, and records the AI action's software agent when the credential provides one.

**AI generator** is read-only. It records evidence from the validated C2PA action and is not an editorial attribution field.

**AI disclosure reason** is also read-only. It explains why a check found no usable AI evidence. Possible values include **No content credentials**, **Invalid content credentials**, **Image exceeds scan size limit**, **Content credentials exceed scan limits**, **No AI declaration**, or **Image could not be read**. Positive AI results leave this property empty.

Saving a name, crop, or another media property does not rerun detection. Existing media is not scanned after installation.

## Optional watermark evidence

**AI watermark** is a separate read-only result from the [optional OpenAI watermark check](/openai-watermarks). `OpenAI SynthID detected` means the API reported a supported watermark. The media tree shows an **AI detected** sign. It does not set `generated` or `modified`, because the watermark cannot distinguish them.

The check requires a separate package and starts disabled. Neither the C2PA check nor the OpenAI check can identify every AI-generated image. Missing evidence leaves the origin unknown. The package does not guess from the image's appearance.

Images with any C2PA credentials, and images kept in manual mode, skip the remote check. Replacing an image clears stale watermark evidence before any new check.

## Override manually

Change **AI disclosure** to `generated`, `modified`, or empty and save. The source becomes `Manual`. A manual value survives later file replacements.

Clearing the disclosure also clears the generator so an undetermined item cannot retain stale generator text. An empty disclosure means no classification was established; it does not assert that the image is human-made.

## Resume automatic detection

**AI disclosure source** is normally managed by the package. Do not select `C2PA` or `Manual` directly. Use **Resume automatic detection** only when leaving manual mode.

Choose **Resume automatic detection** in **AI disclosure source**, then save. The package immediately inspects the current file, even though `umbracoFile` did not change.

- Positive evidence replaces the manual values and sets the source to `C2PA`.
- Missing, invalid, or unrecognised evidence clears the disclosure, generator, and source.
- A detection failure never blocks the media save.

The resume option is an action, not a stored source value. After saving, the source is either `C2PA` or empty.

Administrators can rescan existing Image media from the Media section's **AI disclosure scan** tab. Manual values are preserved; each request is bounded to 50 media entities and the dashboard can stop after the current batch. With the optional OpenAI integration installed, this limit is one entity per request.
