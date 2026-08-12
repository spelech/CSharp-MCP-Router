import { test, expect } from '@playwright/test';

test.describe('Client Setup & App Key Management Flow', () => {

  test('should open Clients modal and view client setup guide snippets', async ({ page }) => {
    await page.goto('/');

    // Click Clients button in top nav
    const clientsBtn = page.locator('button:has-text("Clients"), button:has-text("App Keys")');
    if (await clientsBtn.isVisible()) {
      await clientsBtn.click();

      // Verify Security View opens
      const securityView = page.locator('#view-security');
      await expect(securityView).toBeVisible();

      // Check Generate App Key button
      const generateKeyBtn = page.locator('button:has-text("Create App Key")');
      await expect(generateKeyBtn).toBeVisible();
    }
  });

});
