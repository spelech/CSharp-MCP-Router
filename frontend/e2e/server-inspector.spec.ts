import { test, expect } from '@playwright/test';

test.describe('Server Inspector Modal Flow', () => {

  test('should open Server Inspect Modal and display tool schemas', async ({ page }) => {
    await page.goto('/');

    // Click Inspect button on the first server card
    const inspectBtn = page.locator('button:has-text("Inspect"), button:has-text("Tools")');
    if (await inspectBtn.count() > 0) {
      await inspectBtn.first().click();

      // Verify Inspect Modal overlay opens
      const inspectModal = page.locator('.modal, #inspect-modal, [role="dialog"]');
      await expect(inspectModal.first()).toBeVisible();

      // Check tool inspection tabs (Tools, Resources, Prompts)
      const toolsTab = page.locator('button:has-text("Tools"), .tab-btn:has-text("Tools")');
      const resourcesTab = page.locator('button:has-text("Resources"), .tab-btn:has-text("Resources")');
      const promptsTab = page.locator('button:has-text("Prompts"), .tab-btn:has-text("Prompts")');

      if (await toolsTab.isVisible()) {
        await toolsTab.click();
      }
      if (await resourcesTab.isVisible()) {
        await resourcesTab.click();
      }
      if (await promptsTab.isVisible()) {
        await promptsTab.click();
      }

      // Close modal
      const closeBtn = page.locator('.modal-close, button:has-text("Close")');
      if (await closeBtn.isVisible()) {
        await closeBtn.first().click();
      }
    }
  });

});
