import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UMB_MEDIA_GRID_COLLECTION_VIEW_ALIAS } from "@umbraco-cms/backoffice/media";
import { manifests as mediaGrid } from "./media-grid/manifest.js";

// Keep Umbraco's alias because saved media-layout configuration refers to it directly.
umbExtensionsRegistry.unregister(UMB_MEDIA_GRID_COLLECTION_VIEW_ALIAS);

export const manifests: Array<UmbExtensionManifest> = [...mediaGrid];
