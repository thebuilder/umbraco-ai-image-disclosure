import { describe, expect, it } from "vitest";

import {
  readDisclosureFromValues,
  readDisclosureValue,
  requiresDisclosureLookup,
} from "./disclosure.js";

describe("AI disclosure collection helpers", () => {
  it("reads scalar and dropdown-array values", () => {
    expect(readDisclosureValue("Fully AI-generated")).toBe("Fully AI-generated");
    expect(readDisclosureValue(["Partially AI-modified"])).toBe("Partially AI-modified");
  });

  it("ignores unsupported property values", () => {
    expect(readDisclosureValue([])).toBeUndefined();
    expect(readDisclosureValue(true)).toBeUndefined();
  });

  it("finds the disclosure property by alias", () => {
    expect(
      readDisclosureFromValues([
        { alias: "unrelated", value: "value" },
        { alias: "aiDisclosure", value: ["Fully AI-generated"] },
      ]),
    ).toBe("Fully AI-generated");
  });

  it("loads details only for unresolved images", () => {
    expect(requiresDisclosureLookup({ contentTypeAlias: "Image" })).toBe(true);
    expect(
      requiresDisclosureLookup({
        contentTypeAlias: "Image",
        values: [{ alias: "aiDisclosure", value: "Fully AI-generated" }],
      }),
    ).toBe(false);
    expect(requiresDisclosureLookup({ contentTypeAlias: "umbracoFile" })).toBe(false);
  });
});
