import { css, html, nothing, unsafeCSS, unsafeHTML } from "@umbraco-cms/backoffice/external/lit";
import { loadManifestElement } from "@umbraco-cms/backoffice/extension-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { euIconForFlags, euIconMarkup, euIconStyles } from "./eu-ai-icons.js";

const ownAlias = "TheBuilder.AIImageDisclosure.MediaCollectionItemCard";
const coreCardAlias = "Umb.EntityCollectionItemCard.Media";
const elementName = "thebuilder-ai-disclosure-media-card";

// Our manifest overwrites Umbraco's media card, but the core manifest stays registered.
// Extending its element keeps the thumbnail, selection, actions and upload placeholders.
async function loadCoreCard() {
  const coreCard = umbExtensionsRegistry.getByAlias(coreCardAlias);
  // Retry once, so a single failed chunk request doesn't switch the badges off for the session.
  const load = () => loadManifestElement(coreCard.element);
  try {
    if (!coreCard) throw new Error(`${coreCardAlias} is not registered.`);
    const element = await load().catch(() => new Promise((resolve) => setTimeout(resolve, 1000)).then(load));
    if (!element) throw new Error(`${coreCardAlias} did not provide an element.`);
    return element;
  } catch (error) {
    // Nothing to extend, so step aside and let Umbraco render its own card again.
    console.warn(`AI Image Disclosure: Media Grid badges are off because ${coreCardAlias} could not be loaded.`, error);
    umbExtensionsRegistry.unregister(ownAlias);
    throw error;
  }
}

const UmbMediaCollectionItemCardElement = await loadCoreCard();

class AiDisclosureMediaCardElement extends UmbMediaCollectionItemCardElement {
  #disclosure;
  #nameBar;
  #resizeObserver = new ResizeObserver(() => this.#placeBadge());

  render() {
    this.#disclosure = euIconForFlags(this.item?.flags);
    return html`${super.render()}${this.#disclosure
      ? html`<div id="ai-disclosure" aria-hidden="true">
          ${unsafeHTML(euIconMarkup(this.#disclosure.icon, this.#disclosure.label))}
        </div>`
      : nothing}`;
  }

  async updated(changedProperties) {
    super.updated?.(changedProperties);
    const card = this.shadowRoot.querySelector("uui-card-media");
    await card?.updateComplete;
    this.#labelCard(card);
    this.#observeNameBar(card?.shadowRoot?.getElementById("content"));
  }

  connectedCallback() {
    super.connectedCallback();
    if (this.hasUpdated) this.requestUpdate();
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    this.#resizeObserver.disconnect();
    this.#nameBar = undefined;
  }

  // The badge is decorative; the card's link carries the label, so it is part of its accessible name.
  #labelCard(card) {
    let label = card?.querySelector(":scope > .ai-disclosure-label");
    if (!card || !this.#disclosure) {
      label?.remove();
      return;
    }

    if (!label) {
      label = document.createElement("span");
      label.className = "ai-disclosure-label";
      label.slot = "detail";
      card.append(label);
    }
    label.textContent = this.#disclosure.label;
  }

  // Keeps the badge just above the card's name bar, whatever its height.
  #observeNameBar(nameBar) {
    if (nameBar === this.#nameBar) return;

    this.#resizeObserver.disconnect();
    this.#nameBar = nameBar;
    if (nameBar) {
      this.#resizeObserver.observe(this);
      this.#resizeObserver.observe(nameBar);
    }
    this.#placeBadge();
  }

  #placeBadge() {
    if (!this.#nameBar) return;
    const offset = this.getBoundingClientRect().bottom - this.#nameBar.getBoundingClientRect().top;
    if (offset > 0) this.style.setProperty("--ai-disclosure-name-bar", `${offset}px`);
  }

  static styles = [
    ...[UmbMediaCollectionItemCardElement.styles ?? []].flat(),
    unsafeCSS(euIconStyles),
    css`
      :host {
        position: relative;
        display: block;
        height: 100%;
      }

      /* Bottom left of the image, clear of the selection checkbox, the actions and the name bar. */
      #ai-disclosure {
        position: absolute;
        bottom: calc(var(--ai-disclosure-name-bar, 2.5rem) + var(--uui-size-space-3));
        left: var(--uui-size-space-4);
        display: flex;
        pointer-events: none;
        --eu-ai-icon-height: 1.25rem;
      }

      .ai-disclosure-label {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
      }
    `,
  ];
}

if (!customElements.get(elementName)) {
  customElements.define(elementName, AiDisclosureMediaCardElement);
}

export default customElements.get(elementName);
