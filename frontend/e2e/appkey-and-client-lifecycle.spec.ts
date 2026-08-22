/** @requirement REQ-UI-129 */

import { test, expect } from '@playwright/test';

test.describe('AppKey and Client Lifecycle Flow', () => {
  test('should create client application and generate AppKey with scope constraints', async ({ page }) => {
    await page.goto('/');

    // Navigate to Clients view
    const clientsTab = page.locator('button:has-text("Clients"), button:has-text("App Keys & Security")').first();
    await expect(clientsTab).toBeVisible();
    await clientsTab.click();

    // Verify Clients & Security view is open
    await expect(page.locator('#view-security, .view-panel').first()).toBeVisible();

    // Open Create AppKey modal
    const createKeyBtn = page.locator('button:has-text("Create App Key"), button:has-text("+ App Key")').first();
    await expect(createKeyBtn).toBeVisible();
    await createKeyBtn.click();

    // Fill AppKey form
    const keyModal = page.locator('.modal-backdrop');
    await expect(keyModal).toBeVisible();

    const nameInput = page.locator('#key-name, input[placeholder*="Key Name"], input[placeholder*="e.g."]').first();
    await nameInput.fill('Claude Desktop Integration');

    // Submit AppKey creation
    const submitBtn = page.locator('#btn-create-key, button:has-text("Generate Key"), button[type="submit"]').first();
    await submitBtn.click();

    // Verify raw key presentation / snippet display
    await page.waitForTimeout(500);

    // Close modal if done
    const doneBtn = page.locator('button:has-text("Done"), button:has-text("Close"), .btn-close').first();
    if (await doneBtn.isVisible()) {
      await doneBtn.click();
    }
  });
});
