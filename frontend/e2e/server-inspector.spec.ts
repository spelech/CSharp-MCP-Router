/** @requirement UI-126 */

import { test, expect } from '@playwright/test';

test.describe('Server Inspector Modal Flow', () => {

  /**
   * @requirement MCP-01
   * @category MCP
   * @type Positive
   * @description should open Server Inspect Modal if servers are present on dashboard
   */
  test('should open Server Inspect Modal if servers are present on dashboard', async ({ page }) => {
    await page.goto('/');

    // Check if server cards exist
    const inspectBtn = page.locator('button:has-text("Inspect"), button:has-text("Tools"), .btn-inspect').first();
    if (await inspectBtn.count() > 0) {
      await inspectBtn.click();

      // Verify Inspect Modal overlay opens
      const inspectModal = page.locator('.modal-backdrop, #inspect-modal, [role="dialog"]').first();
      await expect(inspectModal).toBeVisible();

      // Close modal
      const closeBtn = page.locator('.btn-close, button:has-text("Close")').first();
      await closeBtn.click();
    }
  });

});
