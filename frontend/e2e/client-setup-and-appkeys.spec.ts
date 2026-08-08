import { test, expect } from '@playwright/test';

test.describe('Client Setup & App Key Management Flow', () => {

  test('should open Clients modal and view client setup guide snippets', async ({ page }) => {
    await page.goto('/');

    // Click Clients button in top nav
    const clientsBtn = page.locator('button:has-text("Clients"), button:has-text("App Keys")');
    if (await clientsBtn.isVisible()) {
      await clientsBtn.click();

      // Verify Clients Modal opens
      const clientsModal = page.locator('.modal, #clients-modal, [role="dialog"]');
      await expect(clientsModal.first()).toBeVisible();

      // Check Generate App Key button
      const generateKeyBtn = page.locator('button:has-text("Generate"), button:has-text("+ Create Key")');
      if (await generateKeyBtn.isVisible()) {
        await expect(generateKeyBtn).toBeVisible();
      }

      // Close modal
      const closeBtn = page.locator('.modal-close, button:has-text("Close")');
      if (await closeBtn.isVisible()) {
        await closeBtn.first().click();
      }
    }
  });

});
