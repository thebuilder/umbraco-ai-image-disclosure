import { fileURLToPath } from "node:url";
import sharp from "sharp";

const docsRoot = fileURLToPath(new URL("../", import.meta.url));
const repositoryRoot = fileURLToPath(new URL("../../../", import.meta.url));

await Promise.all([
  sharp(`${docsRoot}public/logo-mark.svg`)
    .resize(256, 256)
    .png()
    .toFile(`${docsRoot}favicon.png`),
  sharp(`${repositoryRoot}brand/icon.svg`)
    .resize(512, 512)
    .png()
    .toFile(`${repositoryRoot}icon.png`),
]);

console.log("Generated favicon.png and icon.png from their vector sources.");
