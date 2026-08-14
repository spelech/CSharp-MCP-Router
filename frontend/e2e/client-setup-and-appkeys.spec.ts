import { test, expect } from '@playwright/test';

test.describe('Client Setup & App Key Management Flow', () => {

  test('should open App Keys & Security view and display client setup controls', async ({ page }) => {
    await page.goto('/');

    // Click App Keys & Security button in top nav
    const securityTab = page.locator('button:has-text("App Keys & Security"), button:has-text("Clients")').first();
    await expect(securityTab).toBeVisible();
    await securityTab.click();

    // Verify Security View opens
    const securityView = page.locator('#view-security, .view-panel');
    await expect(securityView.first()).toBeVisible();

    // Check Create App Key button
    const generateKeyBtn = page.locator('button:has-text("Create App Key")').first();
    await expect(generateKeyBtn).toBeVisible();
  });

});
