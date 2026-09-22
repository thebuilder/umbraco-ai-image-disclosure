import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css } from '@umbraco-cms/backoffice/external/lit';
import { client } from '@umbraco-cms/backoffice/external/backend-api';

const endpoint = '/umbraco/backoffice/api/ai-image-disclosure/openai';
const security = [{ scheme: 'bearer', type: 'http' }];

export default class OpenAiDisclosureSettings extends UmbLitElement {
  static styles = css`
    :host { display: block; padding: var(--uui-size-layout-1); }
    uui-box { max-width: 48rem; }
    p { line-height: 1.5; }
    uui-select { display: block; width: 100%; max-width: 30rem; }
    .field { margin-block: var(--uui-size-space-5); }
    .field-label { display: block; margin-bottom: var(--uui-size-space-2); }
    .actions { display: flex; flex-wrap: wrap; gap: var(--uui-size-space-3); }
    .error { color: var(--uui-color-danger-standalone); }
  `;

  #enabled = false;
  #connectionId = '';
  #connections = [];
  #available = false;
  #busy = true;
  #status = 'Loading settings…';
  #error = '';

  connectedCallback() {
    super.connectedCallback();
    this.#load();
  }

  async #load() {
    try {
      const { data, error, response } = await client.get({ url: `${endpoint}/settings`, security });
      if (response?.status === 404) {
        this.#status = 'Install the OpenAI integration package to configure watermark verification.';
        return;
      }
      if (error || !data || !response.ok) throw new Error();
      this.#enabled = data.enabled;
      this.#connectionId = data.connectionId ?? '';
      this.#connections = data.connections;
      this.#available = data.available;
      this.#status = data.status;
    } catch {
      this.#error = 'Settings could not be loaded. Reload this page to try again.';
    } finally {
      this.#busy = false;
      this.requestUpdate();
    }
  }

  #save = async () => {
    if (this.#busy) return;
    if (this.#enabled && !this.#connectionId) {
      this.#error = 'Select an OpenAI connection to enable watermark verification.';
      this.requestUpdate();
      this.shadowRoot.querySelector('uui-select')?.focus();
      return;
    }
    this.#busy = true;
    this.#error = '';
    this.requestUpdate();
    try {
      const { error, response } = await client.put({
        url: `${endpoint}/settings`, security,
        headers: { 'Content-Type': 'application/json' },
        body: { enabled: this.#enabled, connectionId: this.#connectionId || null },
      });
      if (error || !response.ok) throw new Error();
      this.#status = 'Settings saved.';
    } catch {
      this.#error = 'Settings could not be saved. Check that the selected OpenAI connection is active and uses the OpenAI endpoint.';
    } finally {
      this.#busy = false;
      this.requestUpdate();
    }
  };

  #test = async () => {
    if (this.#busy || !this.#connectionId) return;
    this.#busy = true;
    this.#error = '';
    this.#status = 'Testing verification…';
    this.requestUpdate();
    try {
      const { data, error, response } = await client.post({
        url: `${endpoint}/test`, security,
        headers: { 'Content-Type': 'application/json' },
        body: { connectionId: this.#connectionId },
      });
      if (error || !data || !response.ok) throw new Error();
      this.#status = data.message;
    } catch {
      this.#error = 'Verification could not be tested. Check the connection and try again.';
    } finally {
      this.#busy = false;
      this.requestUpdate();
    }
  };

  render() {
    const options = [{ name: 'Select an OpenAI connection', value: '' },
      ...this.#connections.map(connection => ({ name: connection.name, value: connection.id }))]
      .map(option => ({ ...option, selected: option.value === this.#connectionId }));
    return html`
      <uui-box headline="OpenAI watermark fallback">
        <form method="post" action=${`${endpoint}/settings`} @submit=${event => { event.preventDefault(); this.#save(); }}>
        <p>Check for OpenAI SynthID watermarks only when an image has no C2PA Content Credentials. Manual classifications are preserved.</p>
        <div class="field">
          <uui-toggle name="enabled" label="Enable OpenAI watermark fallback" .checked=${this.#enabled}
            ?disabled=${this.#busy || !this.#available}
            @change=${event => { this.#enabled = event.target.checked; this.requestUpdate(); }}>
            Enable OpenAI watermark fallback
          </uui-toggle>
        </div>
        <div class="field">
          <label class="field-label" id="connection-label" for="connection">OpenAI connection</label>
          <uui-select id="connection" name="connectionId" label="OpenAI connection" aria-labelledby="connection-label" .options=${options}
            ?disabled=${this.#busy || !this.#available}
            @change=${event => { this.#connectionId = event.target.value; this.requestUpdate(); }}></uui-select>
          <p>Manage credentials in Umbraco AI. The key stays on the server.</p>
        </div>
        <p>Enabling this sends eligible images to OpenAI. The verification endpoint is not eligible for Zero Data Retention.</p>
        <p>A detected watermark is evidence of AI use; it does not distinguish a fully generated image from an AI edit. A negative result leaves the origin undetermined.</p>
        ${!this.#available && !this.#busy ? html`<p>Install <code>TheBuilder.AIImageDisclosure.OpenAI</code> and configure <code>Umbraco.AI.OpenAI</code> to use this feature.</p>` : ''}
        <div class="actions">
          <uui-button look="primary" type="submit" label="Save" ?disabled=${this.#busy || !this.#available}>Save</uui-button>
          <uui-button look="secondary" type="button" label="Test verification" ?disabled=${this.#busy || !this.#available || !this.#connectionId} @click=${this.#test}>Test verification</uui-button>
        </div>
        <p>Test verification sends a bundled sample image to OpenAI using the selected connection. It does not change these settings.</p>
        <p role="status" aria-atomic="true">${this.#status}</p>
        ${this.#error ? html`<p class="error" role="alert">${this.#error}</p>` : ''}
        </form>
      </uui-box>`;
  }
}

customElements.define('thebuilder-openai-disclosure-settings', OpenAiDisclosureSettings);
