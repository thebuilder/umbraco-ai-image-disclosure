import { defineConfig } from "blume";
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
    output: "static",
    site: "https://ai-image-disclosure.thebuilder.dk",
  },
  seo: {
    og: { enabled: false },
  },
  theme: {
    accent: "#7557e8",
    fonts: {
      display: "inter-tight",
    },
  },
  ai: {
    llmsTxt: true,
  },
});
