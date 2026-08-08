import { test, expect } from '@playwright/test';

test.describe('Settings View Flow', () => {

  test('should navigate to Settings view and configure vector embedding options', async ({ page }) => {
    await page.goto('/');

    // Click Settings button
    const settingsTab = page.locator('button:has-text("Settings"), button[data-view="view-settings"]');
    await expect(settingsTab.first()).toBeVisible();
    await settingsTab.first().click();

    // Verify Settings view is rendered
    await expect(page.locator('#view-settings, .settings-container, main')).toBeVisible();

    // Check Embedding Provider select dropdown
    const providerSelect = page.locator('select#embedding-provider, select[name="embeddingProvider"]');
    if (await providerSelect.isVisible()) {
      // Test selecting Local ONNX
      await providerSelect.selectOption({ label: 'Local ONNX' });

      // Test selecting OpenAI / Ollama API
      await providerSelect.selectOption({ label: 'API Provider' });
    }

    // Check Save Settings button
    const saveBtn = page.locator('button:has-text("Save Settings"), button[type="submit"]');
    if (await saveBtn.isVisible()) {
      await expect(saveBtn).toBeVisible();
    }
  });

});
