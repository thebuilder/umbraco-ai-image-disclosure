import { defineUmbracoPackage } from "@thebuilder/umbraco-docs";

export const aiImageDisclosurePackage = defineUmbracoPackage({
  id: "thebuilder.aiimagedisclosure",
  name: "AI Image Disclosure",
  summary:
    "Classify AI-generated and AI-modified Umbraco images from signed C2PA Content Credentials.",
  links: {
    docs: "https://ai-image-disclosure.thebuilder.dk",
    nuget: "https://www.nuget.org/packages/TheBuilder.AIImageDisclosure",
    marketplace:
      "https://marketplace.umbraco.com/package/thebuilder.aiimagedisclosure",
    github: "https://github.com/thebuilder/umbraco-ai-generated",
  },
  logo: "/logo-mark.svg",
  compatibility: {
    umbraco: ">=17.1 <19",
    dotnet: ">=10",
  },
  status: "stable",
  categories: ["Artificial Intelligence", "Editor Tools"],
});
