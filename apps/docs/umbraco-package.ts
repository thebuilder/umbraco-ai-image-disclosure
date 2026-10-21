import { defineUmbracoPackage } from "@thebuilder/umbraco-docs";

export const aiImageDisclosurePackage = defineUmbracoPackage({
  id: "thebuilder.aiimagedisclosure",
  name: "AI Image Disclosure",
  summary:
    "Check Umbraco images for signed C2PA claims of AI use. An OpenAI watermark check is optional.",
  links: {
    docs: "https://ai-image-disclosure.thebuilder.dk",
    nuget: "https://www.nuget.org/packages/TheBuilder.AIImageDisclosure",
    marketplace:
      "https://marketplace.umbraco.com/package/thebuilder.aiimagedisclosure",
    github: "https://github.com/thebuilder/umbraco-ai-image-disclosure",
  },
  logo: "/logo-mark.svg",
  compatibility: {
    umbraco: ">=17.3 <19",
    dotnet: ">=10",
  },
  status: "preview",
  categories: ["Artificial Intelligence", "Editor Tools"],
});
