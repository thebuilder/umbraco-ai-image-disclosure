import generatedBadgeUrl from "../assets/ai-generated-black.svg";
import modifiedBadgeUrl from "../assets/ai-modified-black.svg";
import {
  UMB_EDIT_MEDIA_WORKSPACE_PATH_PATTERN,
  UMB_MEDIA_DETAIL_STORE_CONTEXT,
  UMB_MEDIA_PLACEHOLDER_ENTITY_TYPE,
  type UmbMediaCollectionItemModel,
} from "@umbraco-cms/backoffice/media";
import { MediaService } from "@umbraco-cms/backoffice/external/backend-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import {
  css,
  customElement,
  html,
  ifDefined,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbFileDropzoneItemStatus } from "@umbraco-cms/backoffice/dropzone";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

import {
  generatedDisclosureValue,
  modifiedDisclosureValue,
  readDisclosureFromValues,
  requiresDisclosureLookup,
} from "./disclosure.js";

import "@umbraco-cms/backoffice/imaging";

type DisclosureBadge = {
  accessibleLabel: string;
  url: string;
};

@customElement("thebuilder-ai-disclosure-media-grid")
export class AiDisclosureMediaGridElement extends UmbLitElement {
  @state()
  private items: Array<UmbMediaCollectionItemModel> = [];

  @state()
  private selectable = false;

  @state()
  private selection: Array<string | null> = [];

  @state()
  private itemHrefs = new Map<string, string>();

  @state()
  private disclosureByUnique = new Map<string, string>();

  #collectionContext?: typeof UMB_COLLECTION_CONTEXT.TYPE;
  #mediaDetailStore?: typeof UMB_MEDIA_DETAIL_STORE_CONTEXT.TYPE;
  #loadingDisclosures = new Set<string>();
  #observedDisclosures = new Set<string>();
  #disclosureRetryTimers = new Map<string, number>();
  #disclosureRetryAttempts = new Map<string, number>();
  #visibilityObserver?: IntersectionObserver;
  #itemsRequest = 0;

  constructor() {
    super();

    this.consumeContext(UMB_COLLECTION_CONTEXT, (context) => {
      this.#collectionContext = context;
      context?.setupView(this);
      this.observe(
        context?.selection.selectable,
        (selectable) => (this.selectable = selectable ?? false),
        "aiDisclosureCollectionSelectableObserver",
      );
      this.observe(
        context?.selection.selection,
        (selection) => (this.selection = selection ?? []),
        "aiDisclosureCollectionSelectionObserver",
      );
      this.observe(
        context?.items,
        (items) => this.#setItems(items as Array<UmbMediaCollectionItemModel> | undefined),
        "aiDisclosureCollectionItemsObserver",
      );
    });
    this.consumeContext(UMB_MEDIA_DETAIL_STORE_CONTEXT, (store) => {
      this.#mediaDetailStore = store;
    });
  }

  async #setItems(items: Array<UmbMediaCollectionItemModel> | undefined) {
    const request = ++this.#itemsRequest;
    this.items = items ?? [];
    this.#pruneDisclosureState();
    const entries = await Promise.all(
      this.items.map(async (item) => {
        const href =
          (await this.#collectionContext?.requestItemHref?.(item)) ??
          UMB_EDIT_MEDIA_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique });
        return href ? ([item.unique, href] as const) : undefined;
      }),
    );
    if (request !== this.#itemsRequest) return;
    this.itemHrefs = new Map(
      entries.filter((entry): entry is readonly [string, string] => entry !== undefined),
    );
  }

  override firstUpdated() {
    this.#connectVisibilityObserver();
  }

  override connectedCallback() {
    super.connectedCallback();
    if (this.hasUpdated) this.#connectVisibilityObserver();
  }

  #connectVisibilityObserver() {
    if (this.#visibilityObserver) return;

    this.#visibilityObserver = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;

          const unique = (entry.target as HTMLElement).dataset.disclosureUnique;
          if (unique) {
            this.#visibilityObserver?.unobserve(entry.target);
            void this.#loadDisclosure(unique).then((loaded) => {
              if (!loaded) this.#scheduleDisclosureRetry(entry.target as HTMLElement, unique);
            });
          }
        }
      },
      { rootMargin: "200px" },
    );
    this.#observeUnresolvedCards();
  }

  override updated() {
    this.#observeUnresolvedCards();
  }

  override disconnectedCallback() {
    this.#visibilityObserver?.disconnect();
    this.#visibilityObserver = undefined;
    for (const timer of this.#disclosureRetryTimers.values()) window.clearTimeout(timer);
    this.#disclosureRetryTimers.clear();
    super.disconnectedCallback();
  }

  #observeUnresolvedCards() {
    if (!this.#visibilityObserver) return;

    for (const card of this.renderRoot.querySelectorAll<HTMLElement>("[data-disclosure-unique]")) {
      const unique = card.dataset.disclosureUnique;
      if (
        unique &&
        !this.#loadingDisclosures.has(unique) &&
        !this.#observedDisclosures.has(unique)
      ) {
        this.#visibilityObserver.observe(card);
      }
    }
  }

  async #loadDisclosure(unique: string): Promise<boolean> {
    if (this.#loadingDisclosures.has(unique) || this.#observedDisclosures.has(unique)) return true;
    this.#loadingDisclosures.add(unique);

    try {
      const { data, error } = await tryExecute(
        this,
        MediaService.getMediaById({ path: { id: unique } }),
        { disableNotifications: true },
      );
      if (error || !this.#isActiveDisclosureLookup(unique)) return false;

      this.#disclosureRetryAttempts.delete(unique);
      this.#setDisclosure(unique, readDisclosureFromValues(data?.values));

      const detail = this.#mediaDetailStore?.byUnique(unique);
      if (detail) {
        this.#observedDisclosures.add(unique);
        this.observe(
          detail,
          (current) => {
            if (current) this.#setDisclosure(unique, readDisclosureFromValues(current.values));
          },
          `aiDisclosureDetailObserver:${unique}`,
        );
      }
      return true;
    } finally {
      this.#loadingDisclosures.delete(unique);
    }
  }

  #scheduleDisclosureRetry(card: HTMLElement, unique: string) {
    if (this.#disclosureRetryTimers.has(unique)) return;
    const attempts = (this.#disclosureRetryAttempts.get(unique) ?? 0) + 1;
    this.#disclosureRetryAttempts.set(unique, attempts);
    if (attempts > 1) return;

    const timer = window.setTimeout(() => {
      this.#disclosureRetryTimers.delete(unique);
      if (this.isConnected && card.isConnected && this.#isActiveDisclosureLookup(unique)) {
        this.#visibilityObserver?.observe(card);
      }
    }, 2_000);
    this.#disclosureRetryTimers.set(unique, timer);
  }

  #isActiveDisclosureLookup(unique: string): boolean {
    const item = this.items.find((candidate) => candidate.unique === unique);
    return item !== undefined && requiresDisclosureLookup(item);
  }

  #pruneDisclosureState() {
    const activeUniques = new Set(
      this.items.filter(requiresDisclosureLookup).map((item) => item.unique),
    );

    for (const unique of this.#observedDisclosures) {
      if (activeUniques.has(unique)) continue;
      this.removeUmbControllerByAlias(`aiDisclosureDetailObserver:${unique}`);
      this.#observedDisclosures.delete(unique);
    }

    for (const [unique, timer] of this.#disclosureRetryTimers) {
      if (activeUniques.has(unique)) continue;
      window.clearTimeout(timer);
      this.#disclosureRetryTimers.delete(unique);
    }

    for (const unique of this.#disclosureRetryAttempts.keys()) {
      if (!activeUniques.has(unique)) this.#disclosureRetryAttempts.delete(unique);
    }

    const retainedDisclosures = new Map(
      [...this.disclosureByUnique].filter(([unique]) => activeUniques.has(unique)),
    );
    if (retainedDisclosures.size !== this.disclosureByUnique.size) {
      this.disclosureByUnique = retainedDisclosures;
    }
  }

  #setDisclosure(unique: string, disclosure: string | undefined) {
    const current = this.disclosureByUnique.get(unique);
    if (current === disclosure) return;

    const next = new Map(this.disclosureByUnique);
    if (disclosure) next.set(unique, disclosure);
    else next.delete(unique);
    this.disclosureByUnique = next;
  }

  override render() {
    return html`
      <div id="media-grid">
        ${repeat(
          this.items,
          (item) => item.unique + ("status" in item ? item.status : ""),
          (item) => this.#renderItem(item),
        )}
      </div>
    `;
  }

  #renderItem(item: UmbMediaCollectionItemModel) {
    if (item.entityType === UMB_MEDIA_PLACEHOLDER_ENTITY_TYPE) {
      return this.#renderPlaceholder(item);
    }

    const href = this.itemHrefs.get(item.unique);
    const badge = this.#getDisclosureBadge(item);

    return html`
      <uui-card-media
        name=${ifDefined(item.name)}
        data-mark="${item.entityType}:${item.unique}"
        data-disclosure-unique=${ifDefined(
          requiresDisclosureLookup(item) ? item.unique : undefined,
        )}
        ?selectable=${this.selectable}
        ?select-only=${this.selection.length > 0}
        ?selected=${this.#collectionContext?.selection.isSelected(item.unique) ?? false}
        href=${ifDefined(href)}
        @selected=${() => this.#collectionContext?.selection.select(item.unique)}
        @deselected=${() => this.#collectionContext?.selection.deselect(item.unique)}>
        <div class="thumbnail">
          <umb-imaging-thumbnail
            .unique=${item.unique}
            alt=${ifDefined(item.name)}
            icon=${ifDefined(item.icon)}></umb-imaging-thumbnail>
          ${badge
            ? html`<span class="disclosure-badge" role="img" aria-label=${badge.accessibleLabel}>
                <img src=${badge.url} alt="" />
              </span>`
            : ""}
        </div>
        <umb-entity-actions-bundle
          slot="actions"
          .entityType=${item.entityType}
          .unique=${item.unique}></umb-entity-actions-bundle>
      </uui-card-media>
    `;
  }

  #getDisclosureBadge(item: UmbMediaCollectionItemModel): DisclosureBadge | undefined {
    const disclosure =
      readDisclosureFromValues(item.values) ?? this.disclosureByUnique.get(item.unique);

    if (disclosure === generatedDisclosureValue) {
      return { accessibleLabel: generatedDisclosureValue, url: generatedBadgeUrl };
    }

    if (disclosure === modifiedDisclosureValue) {
      return { accessibleLabel: modifiedDisclosureValue, url: modifiedBadgeUrl };
    }

    return undefined;
  }

  #renderPlaceholder(item: UmbMediaCollectionItemModel) {
    const complete = item.status === UmbFileDropzoneItemStatus.COMPLETE;
    const error = item.status !== UmbFileDropzoneItemStatus.WAITING && !complete;

    return html`
      <uui-card-media disabled class="media-placeholder-item" name=${ifDefined(item.name)}>
        <umb-temporary-file-badge
          .progress=${item.progress ?? 0}
          ?complete=${complete}
          ?error=${error}></umb-temporary-file-badge>
      </uui-card-media>
    `;
  }

  static override styles = [
    UmbTextStyles,
    css`
      :host {
        display: flex;
        flex-direction: column;
      }

      #media-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        grid-auto-rows: 200px;
        gap: var(--uui-size-space-5);
      }

      uui-card-media,
      .thumbnail,
      umb-imaging-thumbnail {
        width: 100%;
        height: 100%;
      }

      .thumbnail {
        position: relative;
        overflow: hidden;
      }

      .disclosure-badge {
        position: absolute;
        inset: auto 0 calc(var(--uui-size-layout-2) + var(--uui-size-space-2)) auto;
        width: min(7rem, 100%);
        pointer-events: none;
      }

      .disclosure-badge img {
        display: block;
        width: 100%;
        height: auto;
      }

      umb-entity-actions-bundle {
        --uui-button-background-color: var(--uui-color-surface);
        --uui-button-background-color-hover: var(--uui-color-surface);
      }
    `,
  ];
}

export default AiDisclosureMediaGridElement;

declare global {
  interface HTMLElementTagNameMap {
    "thebuilder-ai-disclosure-media-grid": AiDisclosureMediaGridElement;
  }
}
