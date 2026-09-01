export const disclosurePropertyAlias = "aiDisclosure";
export const generatedDisclosureValue = "Fully AI-generated";
export const modifiedDisclosureValue = "Partially AI-modified";

type DisclosureValue = {
  alias: string;
  value?: unknown;
};

type DisclosureCollectionItem = {
  contentTypeAlias?: string;
  values?: Array<DisclosureValue>;
};

export function readDisclosureValue(value: unknown): string | undefined {
  if (typeof value === "string") return value;
  if (Array.isArray(value) && typeof value[0] === "string") return value[0];
  return undefined;
}

export function readDisclosureFromValues(
  values: Array<DisclosureValue> | undefined,
): string | undefined {
  return readDisclosureValue(
    values?.find((property) => property.alias === disclosurePropertyAlias)?.value,
  );
}

export function requiresDisclosureLookup(item: DisclosureCollectionItem): boolean {
  return item.contentTypeAlias === "Image" && readDisclosureFromValues(item.values) === undefined;
}
