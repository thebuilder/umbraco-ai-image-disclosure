import { fileURLToPath } from "node:url";
import sharp from "sharp";

const docsRoot = fileURLToPath(new URL("../", import.meta.url));
const repositoryRoot = fileURLToPath(new URL("../../../", import.meta.url));

// The favicon lives in public/ so Blume links it by URL. A root-level icon gets inlined as a data URI in every
// page's <head>, ahead of the Open Graph tags, and Slack only reads the first 32 KB of a page.
await Promise.all([
  sharp(`${docsRoot}public/logo-mark.svg`)
    .resize(256, 256)
    .png()
    .toFile(`${docsRoot}public/favicon.png`),
  sharp(`${repositoryRoot}brand/icon.svg`)
    .resize(512, 512)
    .png()
    .toFile(`${repositoryRoot}icon.png`),
]);

console.log("Generated public/favicon.png and icon.png from their vector sources.");
