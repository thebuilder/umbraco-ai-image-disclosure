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
    code { overflow-wrap: anywhere; }
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
  #managedByConfiguration = false;
  #apiKeyConfigured = false;
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
      this.#managedByConfiguration = data.managedByConfiguration ?? false;
      this.#apiKeyConfigured = data.apiKeyConfigured ?? false;
      this.#status = {
        ready: 'Watermark fallback is enabled.',
        disabled: 'Watermark fallback is off.',
        unavailable: this.#managedByConfiguration
          ? 'No usable API key is configured. Check the OpenAI settings on the server.'
          : 'Configure an OpenAI API key in app settings, or install the Umbraco.AI adapter to select a connection.',
        invalid: this.#managedByConfiguration
          ? 'No usable API key is configured. Check the OpenAI settings on the server.'
          : 'The selected connection is unavailable. Check its credentials and endpoint in Umbraco AI.',
      }[data.status] ?? 'Settings loaded.';
    } catch {
      this.#error = 'Settings could not be loaded. Reload this page to try again.';
    } finally {
      this.#busy = false;
      this.requestUpdate();
    }
  }

  #save = async () => {
    if (this.#busy || this.#managedByConfiguration) return;
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
    if (this.#busy || !this.#available || (this.#managedByConfiguration ? !this.#apiKeyConfigured : !this.#connectionId)) return;
    this.#busy = true;
    this.#error = '';
    this.#status = 'Testing verification…';
    this.requestUpdate();
    try {
      const { data, error, response } = await client.post({
        url: `${endpoint}/test`, security,
        headers: { 'Content-Type': 'application/json' },
        body: { connectionId: this.#managedByConfiguration ? null : this.#connectionId },
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
            ?disabled=${this.#busy || !this.#available || this.#managedByConfiguration}
            @change=${event => { this.#enabled = event.target.checked; this.requestUpdate(); }}>
            Enable OpenAI watermark fallback
          </uui-toggle>
        </div>
        ${this.#managedByConfiguration ? html`
        <div class="field">
          <p>${this.#apiKeyConfigured ? 'API key configured.' : 'No API key configured.'} The key stays on the server.</p>
          <p>Managed through app settings. Change <code>TheBuilder:AIImageDisclosure:OpenAI:Enabled</code> on the server to turn automatic checks on or off.</p>
        </div>` : html`
        <div class="field">
          <label class="field-label" id="connection-label" for="connection">OpenAI connection</label>
          <uui-select id="connection" name="connectionId" label="OpenAI connection" aria-labelledby="connection-label" .options=${options}
            ?disabled=${this.#busy || !this.#available || this.#managedByConfiguration}
            @change=${event => { this.#connectionId = event.target.value; this.requestUpdate(); }}></uui-select>
          <p>Manage credentials in Umbraco AI. The key stays on the server.</p>
          ${this.#available && !this.#busy && this.#connections.length === 0
            ? html`<p>Create an active OpenAI connection in the AI section, then reload this page.</p>` : ''}
        </div>`}
        <p>Enabling this sends eligible images to OpenAI. The verification endpoint is not eligible for Zero Data Retention.</p>
        <p>A detected watermark is evidence of AI use; it does not distinguish a fully generated image from an AI edit. A negative result leaves the origin undetermined.</p>
        ${!this.#available && !this.#busy && !this.#managedByConfiguration ? html`<p>Install <code>TheBuilder.AIImageDisclosure.OpenAI</code> and set <code>TheBuilder:AIImageDisclosure:OpenAI:ApiKey</code> in app settings. To reuse an Umbraco.AI connection, also install <code>TheBuilder.AIImageDisclosure.OpenAI.UmbracoAI</code>.</p>` : ''}
        <div class="actions">
          <uui-button look="primary" type="submit" label="Save" ?disabled=${this.#busy || !this.#available || this.#managedByConfiguration}>Save</uui-button>
          <uui-button look="secondary" type="button" label="Test verification" ?disabled=${this.#busy || !this.#available || (this.#managedByConfiguration ? !this.#apiKeyConfigured : !this.#connectionId)} @click=${this.#test}>Test verification</uui-button>
        </div>
        <p>Test verification sends a bundled sample image to OpenAI using ${this.#managedByConfiguration ? 'the configured key' : 'the selected connection'}. It does not change these settings.</p>
        <p role="status" aria-atomic="true">${this.#status}</p>
        ${this.#error ? html`<p class="error" role="alert">${this.#error}</p>` : ''}
        </form>
      </uui-box>`;
  }
}

customElements.define('thebuilder-openai-disclosure-settings', OpenAiDisclosureSettings);
