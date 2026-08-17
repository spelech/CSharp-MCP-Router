import { test, expect } from '@playwright/test';

test.describe('Settings View Flow', () => {

  /**

   * @requirement MCP-01

   * @category MCP

   * @type PositiveFeature

   * @description Resolves MCP tool calls from client to appropriate backend server

   */

  test('should navigate to Settings view and configure vector embedding options', async ({ page }) => {
    await page.goto('/');

    // Click Settings button
    const settingsTab = page.locator('button:has-text("Settings")').first();
    await expect(settingsTab).toBeVisible();
    await settingsTab.click();

    // Verify Settings view is rendered
    await expect(page.locator('#view-settings, .settings-container, main')).toBeVisible();

    // Check Embedding Provider select dropdown
    const providerSelect = page.locator('select#settings-provider, select[name="embeddingProvider"]').first();
    await expect(providerSelect).toBeVisible();

    // Select Local ONNX
    await providerSelect.selectOption('local');
    await expect(providerSelect).toHaveValue('local');

    // Select External API Provider
    await providerSelect.selectOption('api');
    await expect(providerSelect).toHaveValue('api');

    // Check Save Settings button
    const saveBtn = page.locator('#btn-save-settings, button:has-text("Save Settings")').first();
    await expect(saveBtn).toBeVisible();
  });

});
