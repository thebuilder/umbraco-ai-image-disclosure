import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";

// EU icons for labelling AI-generated content, shipped unmodified from
// https://digital-strategy.ec.europa.eu/en/policies/eu-icons-labelling-ai-generated-content
// The SVGs carry wide padding, so each entry records the shape's bounding box to crop it as a CSS background.
// Thumbnails use the 50% transparent variants; the tree keeps the solid icon for contrast on white.
const iconBase = "/App_Plugins/TheBuilder.AIImageDisclosure/icons/";

const icons = {
  ai: {
    viewBox: [566.93, 566.93],
    box: [89.28, 100.72, 365.49, 365.49],
    files: { solid: "eu-ai.svg", transparent: "eu-ai-transparent.svg" },
  },
  generated: {
    viewBox: [1789.84, 566.93],
    box: [207.3, 144.36, 1384.24, 266.41],
    files: { transparent: "eu-ai-generated-transparent.svg" },
  },
  modified: {
    viewBox: [1700.79, 566.93],
    box: [231.11, 144.36, 1230.56, 266.41],
    files: { transparent: "eu-ai-modified-transparent.svg" },
  },
};

export const euIconStyles = `
  .eu-ai-icon {
    display: inline-block;
    flex: none;
    height: var(--eu-ai-icon-height, 1rem);
    background-repeat: no-repeat;
    forced-color-adjust: none;
  }
`;

// The signs in umbraco-package.json own the flag aliases, labels and EU icon choice.
// Map them once, and again only when the registered signs change, not on every card render.
let iconsByFlag = new Map();
umbExtensionsRegistry.byType("entitySign").subscribe((signs) => {
  iconsByFlag = new Map(
    signs
      .filter((sign) => Object.hasOwn(icons, sign.meta?.euIcon) && sign.forEntityTypes?.includes("media"))
      .flatMap((sign) => (sign.forEntityFlags ?? []).map((alias) => [alias, { icon: sign.meta.euIcon, label: sign.meta.label }])),
  );
});

/** Returns `{ icon, label }` for the first flag that one of this package's entity signs handles. */
export function euIconForFlags(flags) {
  return flags?.map((flag) => iconsByFlag.get(flag.alias)).find(Boolean);
}

/** Returns markup for an EU icon cropped to its shape. Size it with `--eu-ai-icon-height`. */
export function euIconMarkup(icon, label, variant = "transparent") {
  const { viewBox, box, files } = icons[icon];
  const [width, height] = viewBox;
  const [x, y, boxWidth, boxHeight] = box;
  const h = "var(--eu-ai-icon-height, 1rem)";
  const scale = (value) => `calc(${h} * ${value / boxHeight})`;

  const file = files[variant] ?? files.transparent;

  return `<span class="eu-ai-icon" role="img" aria-label="${escapeAttribute(label)}"
    style="width: ${scale(boxWidth)}; background-image: url('${iconBase}${file}');
      background-size: ${scale(width)} ${scale(height)}; background-position: ${scale(-x)} ${scale(-y)}"></span>`;
}

function escapeAttribute(value) {
  return String(value ?? "").replace(/[&<>"']/g, (character) => `&#${character.charCodeAt(0)};`);
}
