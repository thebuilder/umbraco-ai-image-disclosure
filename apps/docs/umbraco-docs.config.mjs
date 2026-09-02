import { defineOgConfig } from "@thebuilder/umbraco-docs/og";
import { fileURLToPath } from "node:url";

export default defineOgConfig({
  contentDir: fileURLToPath(new URL("./content", import.meta.url)),
  publicDir: fileURLToPath(new URL("./public", import.meta.url)),
  prefix: "/og",
  brand: "TheBuilder · AI Image Disclosure",
  accent: "#7557e8",
  logo: "/logo-mark.svg",
  root: {
    title: "AI Image Disclosure for Umbraco",
    description:
      "Detect AI-generated and AI-modified images from C2PA Content Credentials and support EU AI Act disclosure workflows in Umbraco.",
  },
});
