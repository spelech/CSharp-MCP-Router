import { test, expect } from '@playwright/test';

test.describe('Vault AppRole Configuration Flow', () => {
  test('should configure Vault AppRole credentials and test connection in settings', async ({ page }) => {
    await page.goto('/');

    // Navigate to Settings -> Providers
    const settingsTab = page.locator('button:has-text("Settings")').first();
    await expect(settingsTab).toBeVisible();
    await settingsTab.click();

    const providersTab = page.locator('button:has-text("Providers"), .nav-tab:has-text("Providers")').first();
    if (await providersTab.isVisible()) {
      await providersTab.click();
    }

    // Check Vault card
    await expect(page.locator('text=Secret Providers')).toBeVisible();

    const vaultSwitch = page.locator('#sec-vault-enabled');
    await expect(vaultSwitch).toBeVisible();
    if (!(await vaultSwitch.isChecked())) {
      await page.locator('label:has(#sec-vault-enabled) .slider').click();
    }
    await expect(vaultSwitch).toBeChecked();

    // Select AppRole radio
    const appRoleRadio = page.locator('input[value="approle"]');
    await expect(appRoleRadio).toBeVisible();
    await appRoleRadio.click();

    // Fill Role ID and Secret ID
    const roleIdInput = page.locator('input[placeholder="Role ID"]');
    await expect(roleIdInput).toBeVisible();
    await roleIdInput.fill('test-role-id');

    const secretIdInput = page.locator('input[placeholder="Secret ID"]');
    await expect(secretIdInput).toBeVisible();
    await secretIdInput.fill('test-secret-id');

    // Click Test Vault button
    const testVaultBtn = page.locator('#btn-test-vault');
    await expect(testVaultBtn).toBeVisible();
    await testVaultBtn.click();

    await page.waitForTimeout(500);

    // Save Secret config
    const saveSecretsBtn = page.locator('#btn-save-secrets');
    await expect(saveSecretsBtn).toBeVisible();
    await saveSecretsBtn.click();
  });
});
