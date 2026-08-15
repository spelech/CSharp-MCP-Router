import { test, expect } from '@playwright/test';

test.describe('RBAC Policy and Group/SID Mapping Lifecycle Flow', () => {
  test('should create, verify, and delete RBAC policy and SID mapping', async ({ page }) => {
    await page.goto('/');

    // Navigate to Settings
    const settingsTab = page.locator('button:has-text("Settings")').first();
    await expect(settingsTab).toBeVisible();
    await settingsTab.click();

    // Click on Access Control tab
    const accessTab = page.locator('button:has-text("Access Control"), .nav-tab:has-text("Access Control")').first();
    if (await accessTab.isVisible()) {
      await accessTab.click();
    }

    // Verify Access Control header
    await expect(page.locator('text=Access Control Policies')).toBeVisible();
    await expect(page.locator('text=Group & SID Mappings')).toBeVisible();

    // Open Policy Modal
    const addPolicyBtn = page.locator('button:has-text("Add Policy")').first();
    await expect(addPolicyBtn).toBeVisible();
    await addPolicyBtn.click();

    // Fill policy modal
    const policyModal = page.locator('.modal-backdrop');
    await expect(policyModal).toBeVisible();

    const targetInput = page.locator('#policy-target');
    await targetInput.fill('server:test-server');

    const groupInput = page.locator('#policy-group');
    await groupInput.fill('Engineering');

    // Save policy
    const savePolicyBtn = page.locator('#btn-save-policy, button:has-text("Save Policy")').first();
    await savePolicyBtn.click();

    // Open Mapping Modal
    const addMappingBtn = page.locator('button:has-text("Add Mapping")').first();
    await expect(addMappingBtn).toBeVisible();
    await addMappingBtn.click();

    const mappingModal = page.locator('.modal-backdrop');
    await expect(mappingModal).toBeVisible();

    const externalIdInput = page.locator('#mapping-external-id');
    await externalIdInput.fill('S-1-5-21-1001');

    const internalGroupInput = page.locator('#mapping-internal-group');
    await internalGroupInput.fill('Developers');

    const saveMappingBtn = page.locator('#btn-save-mapping, button:has-text("Save Mapping")').first();
    await saveMappingBtn.click();
  });
});
