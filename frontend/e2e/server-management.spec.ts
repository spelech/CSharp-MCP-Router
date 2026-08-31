/** @requirement UI-121 */

import { test, expect } from '@playwright/test';

test.describe('Server Registration & Secret Providers Flow', () => {

  /**
   * @requirement MCP-01
   * @category MCP
   * @type Positive
   * @description should open Add Server modal and switch secret provider types
   */
  test('should open Add Server modal and switch secret provider types', async ({ page }) => {
    await page.goto('/');

    // Click Add Server button
    const addBtn = page.locator('button:has-text("Add Server"), button:has-text("+ Add Server")').first();
    await expect(addBtn).toBeVisible();
    await addBtn.click();

    // Verify modal overlay opens
    const modal = page.locator('.modal-backdrop, #server-modal, [role="dialog"]').first();
    await expect(modal).toBeVisible();

    // Check name input
    const nameInput = page.locator('input#server-name, input[name="name"]');
    await expect(nameInput).toBeVisible();
    await nameInput.fill('Playwright Test Server');

    // Select Secret Provider dropdown
    const secretProviderSelect = page.locator('select#server-secret-provider, select[name="secretProvider"]');
    await expect(secretProviderSelect).toBeVisible();

    // Select Vault
    await secretProviderSelect.selectOption('Vault');
    await expect(secretProviderSelect).toHaveValue('Vault');

    // Select Environment
    await secretProviderSelect.selectOption('Environment');
    await expect(secretProviderSelect).toHaveValue('Environment');

    // Select None
    await secretProviderSelect.selectOption('None');
    await expect(secretProviderSelect).toHaveValue('None');

    // Close modal
    const closeBtn = page.locator('.btn-close, button:has-text("Cancel")').first();
    await closeBtn.click();
    await expect(modal).toBeHidden();
  });

});
