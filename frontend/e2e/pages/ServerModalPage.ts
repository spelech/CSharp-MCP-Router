import { Page, Locator } from '@playwright/test';

export class ServerModalPage {
  readonly page: Page;
  readonly modal: Locator;
  readonly serverIdInput: Locator;
  readonly serverNameInput: Locator;
  readonly transportTypeSelect: Locator;
  readonly serverUrlInput: Locator;
  readonly categorySelect: Locator;
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
    this.categorySelect = page.locator('select#server-category, select[name="category"]');
    this.secretProviderSelect = page.locator('select#secret-provider, select[name="secretProvider"]');
    this.secretMountInput = page.locator('input#secret-mount, input[name="secretMount"]');
    this.secretPathInput = page.locator('input#secret-path, input[name="secretPath"]');
    this.secretFieldInput = page.locator('input#secret-field, input[name="secretField"]');
    this.secretKeyInput = page.locator('input#server-key, input[name="key"], input[name="itemKey"]');
    this.saveBtn = page.getByRole('button', { name: /Save|Add|Create/i }).first();
    this.cancelBtn = page.getByRole('button', { name: /Cancel|Close/i }).first();
  }

  async fillServerForm(details: {
    id: string;
    name: string;
    type?: 'http' | 'sse' | 'stdio';
    url: string;
    secretProvider?: 'None' | 'Env' | 'Vault' | 'Registry';
    vaultMount?: string;
    vaultPath?: string;
    vaultField?: string;
    secretKey?: string;
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

    if (details.secretProvider && await this.secretProviderSelect.isVisible()) {
      await this.secretProviderSelect.selectOption(details.secretProvider);

      if (details.secretProvider === 'Vault') {
        if (details.vaultMount && await this.secretMountInput.isVisible()) {
          await this.secretMountInput.fill(details.vaultMount);
        }
        if (details.vaultPath && await this.secretPathInput.isVisible()) {
          await this.secretPathInput.fill(details.vaultPath);
        }
        if (details.vaultField && await this.secretFieldInput.isVisible()) {
          await this.secretFieldInput.fill(details.vaultField);
        }
      } else if (details.secretKey && await this.secretKeyInput.isVisible()) {
        await this.secretKeyInput.fill(details.secretKey);
      }
    }
  }

  async save() {
    if (await this.saveBtn.isVisible()) {
      await this.saveBtn.click();
    }
  }
}
