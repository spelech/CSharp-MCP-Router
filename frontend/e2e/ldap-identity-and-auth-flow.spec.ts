import { test, expect } from '@playwright/test';

test.describe('LDAP and Active Directory Identity Flow', () => {
  test('should configure LDAP identity provider, test connection, and save settings', async ({ page }) => {
    // Navigate to Settings
    await page.goto('/');
    const settingsTab = page.locator('button:has-text("Settings")').first();
    await expect(settingsTab).toBeVisible();
    await settingsTab.click();

    // Click on Providers tab
    const providersTab = page.locator('button:has-text("Providers"), .nav-tab:has-text("Providers")').first();
    if (await providersTab.isVisible()) {
      await providersTab.click();
    }

    // Verify Identity & Auth card
    await expect(page.locator('text=Identity & Auth Providers')).toBeVisible();

    // Toggle Active Directory switch on if not enabled
    const adSwitch = page.locator('#auth-ad-enabled');
    await expect(adSwitch).toBeVisible();
    if (!(await adSwitch.isChecked())) {
      await page.locator('label:has(#auth-ad-enabled) .slider').click();
    }
    await expect(adSwitch).toBeChecked();

    // Fill in LDAP Server details
    const serverInput = page.locator('#ad-server');
    await expect(serverInput).toBeVisible();
    await serverInput.fill('ldap-test');

    const portInput = page.locator('#ad-port');
    await portInput.fill('636');

    const domainInput = page.locator('#ad-domain');
    await domainInput.fill('corp.local');

    const baseDnInput = page.locator('#ad-base-dn');
    await baseDnInput.fill('DC=corp,DC=local');

    const bindDnInput = page.locator('#ad-bind-dn');
    await bindDnInput.fill('CN=admin,DC=corp,DC=local');

    const bindPasswordInput = page.locator('#ad-bind-password');
    await bindPasswordInput.fill('adminpassword');

    // Click Test Connection button
    const testLdapBtn = page.locator('#btn-test-ldap');
    await expect(testLdapBtn).toBeVisible();
    await testLdapBtn.click();

    // Wait for feedback or response
    await page.waitForTimeout(500);

    // Save auth config
    const saveBtn = page.locator('#btn-save-auth');
    await expect(saveBtn).toBeVisible();
    await saveBtn.click();
  });
});
