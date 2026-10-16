import { docsTheme } from "@thebuilder/umbraco-docs";
import { defineConfig } from "blume";
import { vercel } from "blume/analytics";
import { aiImageDisclosurePackage } from "./umbraco-package";

export default defineConfig({
  title: aiImageDisclosurePackage.name,
  description:
    "Check Umbraco images for C2PA evidence of AI use. Add an optional OpenAI watermark check. Missing evidence stays unknown.",
  logo: {
    image: "/logo-mark.svg",
    text: "AI Image Disclosure",
  },
  github: {
    owner: "thebuilder",
    repo: "umbraco-ai-image-disclosure",
    dir: "apps/docs",
  },
  content: {
    root: "content",
  },
  navigation: {
    tabs: [{ label: "Docs", path: "/", href: "/overview" }],
  },
  deployment: {
    site: "https://ai-image-disclosure.thebuilder.dk",
  },
  seo: {
    og: { enabled: false },
  },
  analytics: [vercel()],
  // The docs family's shared accent and fonts, so every package site looks alike.
  theme: { ...docsTheme },
  agents: {
    llmsTxt: true,
  },
});
