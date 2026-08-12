import { Page, Locator, expect } from '@playwright/test';

export class ServerModalPage {
  readonly page: Page;
  readonly modal: Locator;
  readonly serverIdInput: Locator;
  readonly serverNameInput: Locator;
  readonly transportTypeSelect: Locator;
  readonly serverUrlInput: Locator;
  readonly categoryInput: Locator;
  readonly secretProviderSelect: Locator;
  readonly secretMountInput: Locator;
  readonly secretPathInput: Locator;
  readonly secretFieldInput: Locator;
  readonly secretKeyInput: Locator;
  readonly saveBtn: Locator;
  readonly cancelBtn: Locator;

  constructor(page: Page) {
    this.page = page;
    this.modal = page.locator('#server-modal, .modal, [role="dialog"]').first();
    this.serverIdInput = page.locator('input#server-id, input[name="id"]');
    this.serverNameInput = page.locator('input#server-name, input[name="name"]');
    this.transportTypeSelect = page.locator('select#server-type, select[name="type"]');
    this.serverUrlInput = page.locator('input#server-url, input[name="url"]');
    this.categoryInput = page.locator('input#server-category, input[name="category"]');
    this.secretProviderSelect = page.locator('select#server-secret-provider, select[name="secretProvider"]');
    this.secretMountInput = page.locator('input#secret-mount, input[name="secretMount"]');
    this.secretPathInput = page.locator('input#secret-path, input[name="secretPath"]');
    this.secretFieldInput = page.locator('input#secret-field, input[name="secretField"]');
    this.secretKeyInput = page.locator('input#server-secret-key, input#server-key, input[name="key"], input[name="itemKey"]').first();
    this.saveBtn = this.modal.getByRole('button', { name: /Save|Add|Create/i }).first();
    this.cancelBtn = page.getByRole('button', { name: /Cancel|Close/i }).first();
  }

  async fillServerForm(details: {
    id: string;
    name: string;
    type?: 'http' | 'sse' | 'stdio';
    url: string;
    secretProvider?: 'None' | 'Environment' | 'Vault' | 'WindowsRegistry';
    vaultMount?: string;
    vaultPath?: string;
    vaultField?: string;
    secretKey?: string;
    category?: string;
  }) {
    if (await this.serverIdInput.isVisible()) {
      await this.serverIdInput.fill(details.id);
    }
    if (await this.serverNameInput.isVisible()) {
      await this.serverNameInput.fill(details.name);
    }
    if (details.type && await this.transportTypeSelect.isVisible()) {
      await this.transportTypeSelect.selectOption(details.type);
    }
    if (await this.serverUrlInput.isVisible()) {
      await this.serverUrlInput.fill(details.url);
    }
    if (await this.categoryInput.isVisible()) {
      await this.categoryInput.fill(details.category || 'test');
    }

    if (details.secretProvider && await this.secretProviderSelect.isVisible()) {
      await this.secretProviderSelect.selectOption(details.secretProvider);

      if (details.secretProvider === 'Vault') {
        const vaultKey = `${details.vaultMount || ''}:${details.vaultPath || ''}:${details.vaultField || ''}`;
        if (await this.secretKeyInput.isVisible()) {
          await this.secretKeyInput.fill(vaultKey);
        }
      } else if (details.secretKey && await this.secretKeyInput.isVisible()) {
        await this.secretKeyInput.fill(details.secretKey);
      }
    }
  }

  async save() {
    if (await this.saveBtn.isVisible()) {
      await this.saveBtn.click();
      await expect(this.modal).toBeHidden({ timeout: 15000 });
    }
  }
}
