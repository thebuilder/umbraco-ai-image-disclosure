const elementName = "thebuilder-ai-disclosure-entity-sign";

class AiDisclosureEntitySignElement extends HTMLElement {
  constructor() {
    super();

    const shadowRoot = this.attachShadow({ mode: "open" });
    shadowRoot.innerHTML = `
      <style>
        :host {
          display: inline-flex;
          vertical-align: top;
        }

        span {
          display: inline-flex;
          box-sizing: border-box;
          min-width: 1rem;
          height: 0.625rem;
          align-items: center;
          justify-content: center;
          padding-inline: 0.125rem;
          border: 1px solid var(--uui-color-surface, #fff);
          border-radius: 999px;
          background: var(--uui-color-text, #1b264f);
          color: var(--uui-color-surface, #fff);
          font: 700 0.5rem/1 var(--uui-font-family, sans-serif);
          letter-spacing: 0.02em;
          transform: translateX(-0.125rem);
        }
      </style>
      <span aria-hidden="true">AI</span>
    `;
  }

  set manifest(value) {
    this._manifest = value;
    this.setAttribute("role", "img");
    this.setAttribute("aria-label", value?.meta?.label ?? "AI image disclosure");
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
