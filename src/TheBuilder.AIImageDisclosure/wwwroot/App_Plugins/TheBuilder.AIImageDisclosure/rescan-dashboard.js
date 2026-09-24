import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css } from '@umbraco-cms/backoffice/external/lit';
import { client } from '@umbraco-cms/backoffice/external/backend-api';

export default class AiDisclosureRescanDashboard extends UmbLitElement {
  static styles = css`
    :host { display: block; padding: var(--uui-size-layout-1); }
    uui-box { max-width: 48rem; }
    p { line-height: 1.5; }
    .actions { display: flex; flex-wrap: wrap; gap: var(--uui-size-space-3); margin-top: var(--uui-size-space-5); }
    .status { margin-top: var(--uui-size-space-5); }
    .error { color: var(--uui-color-danger-standalone); }
  `;

  #cursor = null;
  #scanned = 0;
  #skipped = 0;
  #failed = 0;
  #phase = 'ready';
  #error = '';

  get #running() { return this.#phase === 'running' || this.#phase === 'stopping'; }

  disconnectedCallback() {
    if (this.#running) this.#phase = 'stopping';
    super.disconnectedCallback();
  }

  #start = async () => {
    if (this.#running) return;
    if (this.#phase === 'complete') {
      this.#cursor = null;
      this.#scanned = this.#skipped = this.#failed = 0;
    }
    this.#phase = 'running';
    this.#error = '';
    this.requestUpdate();
    try {
      while (this.#phase === 'running' && this.isConnected) {
        const { data, error, response } = await client.post({
          security: [{ scheme: 'bearer', type: 'http' }],
          url: '/umbraco/backoffice/api/ai-image-disclosure/rescan',
          headers: { 'Content-Type': 'application/json' },
          body: { cursor: this.#cursor, limit: 50 },
        });
        if (error || !data || !response.ok) throw new Error('The scan request failed. Resume to retry this batch.');
        const next = data.nextCursor;
        if (!next || !Number.isInteger(next.lastId) || !Number.isInteger(next.maximumId)
          || next.lastId < 0 || next.maximumId < next.lastId
          || (this.#cursor && next.maximumId !== this.#cursor.maximumId)
          || (!data.done && next.lastId <= (this.#cursor?.lastId ?? 0))) {
          throw new Error('The scan did not advance. Resume to retry this batch.');
        }
        this.#cursor = next;
        this.#scanned += data.scanned;
        this.#skipped += data.skippedManual;
        this.#failed += data.failed;
        if (data.done) this.#phase = 'complete';
        this.requestUpdate();
      }
      if (this.#running) this.#phase = 'paused';
    } catch (error) {
      this.#phase = 'error';
      this.#error = error instanceof Error ? error.message : 'The scan request failed. Resume to retry this batch.';
    } finally {
      this.requestUpdate();
    }
  };

  #stop = () => {
    this.#phase = 'stopping';
    this.requestUpdate();
  };

  render() {
    const status = {
      ready: 'Ready to scan.',
      running: 'Scanning images…',
      stopping: 'Stopping after the current batch…',
      paused: 'Scan paused. You can resume from the next batch.',
      complete: 'Scan complete.',
      error: 'Scan interrupted.',
    }[this.#phase];
    const label = this.#phase === 'complete' ? 'Scan again'
      : ['paused', 'error'].includes(this.#phase) ? 'Resume scan' : 'Start scan';
    return html`
      <uui-box headline="AI disclosure scan">
        <p>Check existing images for signed AI generation and editing metadata. Manual classifications are preserved.</p>
        <p>Keep this page open while scanning. Leaving the page stops the scan after the current batch.</p>
        <div class="actions">
          <uui-button look="primary" label=${label} ?disabled=${this.#running} @click=${this.#start}>${label}</uui-button>
          ${this.#running ? html`
            <uui-button look="secondary" label="Stop after current batch" ?disabled=${this.#phase === 'stopping'} @click=${this.#stop}>
              Stop after current batch
            </uui-button>` : ''}
        </div>
        <div class="status" role="status" aria-atomic="true">
          <p>${status}</p>
          ${this.#phase !== 'ready' ? html`
            <p>${this.#scanned} images scanned · ${this.#skipped} manual classifications preserved · ${this.#failed} saves failed</p>` : ''}
        </div>
        ${this.#failed ? html`<p class="error">Some images could not be saved. Check the application logs and run another scan after resolving the errors.</p>` : ''}
        ${this.#error ? html`<p class="error" role="alert">${this.#error}</p>` : ''}
      </uui-box>`;
  }
}

customElements.define('thebuilder-ai-disclosure-rescan-dashboard', AiDisclosureRescanDashboard);
