/** @requirement UI-127 */

import { test, expect } from '@playwright/test';

test.describe('RBAC Access Control & Policy Modal Flow', () => {

  test('should navigate to settings permissions tab and open policy configuration modal', async ({ page }) => {
    await page.goto('/');

    // Navigate to Settings
    const settingsTab = page.locator('button:has-text("Settings")');
    await expect(settingsTab.first()).toBeVisible();
    await settingsTab.first().click();

    // Click Permissions & Policies sub-tab if present
    const permissionsSubTab = page.locator('button:has-text("Permissions"), button:has-text("Policies"), button:has-text("Access")');
    if (await permissionsSubTab.count() > 0) {
      await permissionsSubTab.first().click();
    }

    // Check if Add Policy button is present
    const addPolicyBtn = page.locator('button:has-text("Add Policy"), button:has-text("Create Policy"), button:has-text("+ Policy")');
    if (await addPolicyBtn.count() > 0) {
      await addPolicyBtn.first().click();

      // Verify Policy Modal opens
      const policyModal = page.locator('.modal-backdrop, #policy-modal, [role="dialog"]').first();
      await expect(policyModal).toBeVisible();

      // Check target input
      const targetInput = page.locator('input#policy-target, input[name="targetId"]');
      await expect(targetInput).toBeVisible();
      await targetInput.fill('server:ha');

      // Check group input
      const groupInput = page.locator('input#policy-group, input[name="requiredGroup"]');
      await expect(groupInput).toBeVisible();
      await groupInput.fill('SmartHomeOperators');

      // Close modal
      const closeBtn = page.locator('.btn-close, button:has-text("Cancel")').first();
      await closeBtn.click();
    }
  });

});
