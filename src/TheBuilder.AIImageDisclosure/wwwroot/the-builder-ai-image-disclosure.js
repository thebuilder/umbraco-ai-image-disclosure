import { umbExtensionsRegistry as e } from "@umbraco-cms/backoffice/extension-registry";
import { UMB_MEDIA_GRID_COLLECTION_VIEW_ALIAS as i } from "@umbraco-cms/backoffice/media";
import { UMB_COLLECTION_ALIAS_CONDITION as t } from "@umbraco-cms/backoffice/collection";
const o = [
  {
    type: "collectionView",
    alias: i,
    name: "AI disclosure media grid",
    element: () => import("./ai-disclosure-media-grid.element-BZh0w2bc.js"),
    weight: 300,
    meta: {
      label: "Grid",
      icon: "icon-grid",
      pathName: "grid"
    },
    conditions: [
      {
        alias: t,
        match: "Umb.Collection.Media"
      }
    ]
  }
];
e.unregister(i);
const s = [...o];
export {
  s as manifests
};
//# sourceMappingURL=the-builder-ai-image-disclosure.js.map
