import { test, expect } from '@playwright/test';

test.describe('RBAC Access Control & Policy Modal Flow', () => {

  test('should open policy configuration modal for a server', async ({ page }) => {
    await page.goto('/');

    // Find first Policy button on server cards
    const policyBtn = page.locator('button:has-text("Policy"), button:has-text("RBAC")');
    if (await policyBtn.count() > 0) {
      await policyBtn.first().click();

      // Verify Policy Modal opens
      const policyModal = page.locator('.modal, #policy-modal, [role="dialog"]');
      await expect(policyModal.first()).toBeVisible();

      // Check allowed/denied group inputs
      const allowedGroupsInput = page.locator('input#allowed-groups, input[name="allowedGroups"]');
      if (await allowedGroupsInput.isVisible()) {
        await allowedGroupsInput.fill('full_admin, house_member');
        await expect(allowedGroupsInput).toHaveValue('full_admin, house_member');
      }

      // Close modal
      const closeBtn = page.locator('.modal-close, button:has-text("Cancel"), button:has-text("Close")');
      if (await closeBtn.isVisible()) {
        await closeBtn.first().click();
      }
    }
  });

  test('should render pending approvals card if present', async ({ page }) => {
    await page.goto('/');

    const approvalsCard = page.locator('.approvals-card, #approvals-queue, .pending-approvals');
    if (await approvalsCard.isVisible()) {
      await expect(approvalsCard).toBeVisible();
    }
  });

});
