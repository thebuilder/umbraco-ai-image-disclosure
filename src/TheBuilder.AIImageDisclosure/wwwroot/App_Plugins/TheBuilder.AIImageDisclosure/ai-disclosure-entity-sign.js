import { euIconMarkup, euIconStyles } from "./eu-ai-icons.js";

const elementName = "thebuilder-ai-disclosure-entity-sign";

class AiDisclosureEntitySignElement extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: "open" });
  }

  set manifest(value) {
    this._manifest = value;
    const label = value?.meta?.label ?? "AI image disclosure";

    // Tree signs are too small for the worded labels, so every flag uses the basic EU icon.
    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: inline-flex;
          vertical-align: top;
          --eu-ai-icon-height: 0.75rem;
        }

        ${euIconStyles}

        .eu-ai-icon {
          margin-inline-start: -0.125rem;
        }
      </style>
      ${euIconMarkup("ai", label, "solid")}
    `;
  }

  get manifest() {
    return this._manifest;
  }
}

if (!customElements.get(elementName)) {
  customElements.define(elementName, AiDisclosureEntitySignElement);
}

class AiDisclosureEntitySignApi {
  constructor(_host, args) {
    this._label = args.meta.label;
  }

  getLabel() {
    return this._label;
  }

  destroy() {}
}

export const element = AiDisclosureEntitySignElement;
export const api = AiDisclosureEntitySignApi;
