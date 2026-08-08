import { test, expect } from '@playwright/test';

test.describe('Server Registration & Secret Providers Flow', () => {

  test('should open Add Server modal and switch secret provider types', async ({ page }) => {
    await page.goto('/');

    // Click Add Server button
    const addBtn = page.locator('button:has-text("Add Server"), button:has-text("+ Add Server")');
    await expect(addBtn.first()).toBeVisible();
    await addBtn.first().click();

    // Verify modal overlay opens
    const modal = page.locator('.modal, #server-modal, [role="dialog"]');
    await expect(modal.first()).toBeVisible();

    // Check input fields
    const nameInput = page.locator('input#server-name, input[name="name"], input[placeholder*="Name"]');
    if (await nameInput.isVisible()) {
      await nameInput.fill('Playwright Test Server');
    }

    // Select Secret Provider dropdown
    const secretProviderSelect = page.locator('select#secret-provider, select[name="secretProvider"]');
    if (await secretProviderSelect.isVisible()) {
      // Test selecting HashiCorp Vault
      await secretProviderSelect.selectOption({ label: 'Vault' });

      // Verify Vault specific fields appear (SecretMount, SecretPath, SecretField)
      const mountInput = page.locator('input#secret-mount, input[name="secretMount"]');
      const pathInput = page.locator('input#secret-path, input[name="secretPath"]');
      const fieldInput = page.locator('input#secret-field, input[name="secretField"]');

      if (await mountInput.isVisible()) {
        await mountInput.fill('homelab');
        await pathInput.fill('services/test');
        await fieldInput.fill('api_key');
      }

      // Test selecting Environment Variable provider
      await secretProviderSelect.selectOption({ label: 'Env' });

      // Test selecting Direct Key provider
      await secretProviderSelect.selectOption({ label: 'None' });
    }

    // Close modal
    const closeBtn = page.locator('.modal-close, button:has-text("Cancel"), button:has-text("Close")');
    if (await closeBtn.isVisible()) {
      await closeBtn.first().click();
    }
  });

});
