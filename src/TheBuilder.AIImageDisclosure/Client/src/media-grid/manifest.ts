import { UMB_COLLECTION_ALIAS_CONDITION } from "@umbraco-cms/backoffice/collection";
import { UMB_MEDIA_GRID_COLLECTION_VIEW_ALIAS } from "@umbraco-cms/backoffice/media";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "collectionView",
    alias: UMB_MEDIA_GRID_COLLECTION_VIEW_ALIAS,
    name: "AI disclosure media grid",
    element: () => import("./ai-disclosure-media-grid.element.js"),
    weight: 300,
    meta: {
      label: "Grid",
      icon: "icon-grid",
      pathName: "grid",
    },
    conditions: [
      {
        alias: UMB_COLLECTION_ALIAS_CONDITION,
        match: "Umb.Collection.Media",
      },
    ],
  },
];
