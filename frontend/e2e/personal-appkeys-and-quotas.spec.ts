import { test, expect } from './fixtures/userContexts';

test.describe('Self-Service Personal AppKeys, System Keys & User Quotas Flow', () => {

  /**
   * @requirement AUTH-PERSONAL-APPKEY-LIST
   * @category AUTH
   * @type PositiveFeature
   * @description Regular non-admin users see role-adaptive 'My App Keys' view and personal quota indicator
   */
  test('Non-Admin Context: displays My App Keys navigation and personal quota indicator', async ({ operatorPage }) => {
    await operatorPage.goto('/');

    // Verify non-admin sees 'My App Keys' and does NOT see 'Settings'
    const myAppKeysTab = operatorPage.locator('button:has-text("My App Keys")');
    await expect(myAppKeysTab).toBeVisible();

    const settingsTab = operatorPage.locator('button:has-text("Settings")');
    await expect(settingsTab).not.toBeVisible();

    // Navigate to My App Keys
    await myAppKeysTab.click();
    await expect(operatorPage.locator('#view-security, .view-panel').first()).toBeVisible();

    // Verify card header and quota badge
    const headerTitle = operatorPage.locator('#appkeys-card-title, h2:has-text("My App Keys")');
    await expect(headerTitle.first()).toBeVisible();

    const quotaBadge = operatorPage.locator('.quota-badge, text=/Personal Quota:/i');
    await expect(quotaBadge.first()).toBeVisible();
  });

  /**
   * @requirement AUTH-PERSONAL-APPKEY-CREATE
   * @category AUTH
   * @type PositiveFeature
   * @description Regular users can self-mint a personal AppKey and copy config snippets
   */
  test('Non-Admin Context: mints personal key, views snippet, and revokes key', async ({ operatorPage }) => {
    await operatorPage.goto('/');

    const myAppKeysTab = operatorPage.locator('button:has-text("My App Keys")');
    await myAppKeysTab.click();

    // Open Create AppKey modal
    const createKeyBtn = operatorPage.locator('#btn-open-add-key-modal, button:has-text("Create App Key"), button:has-text("+ App Key")').first();
    await expect(createKeyBtn).toBeVisible();
    await createKeyBtn.click();

    // Verify modal is displayed and locked to Personal Key
    const modal = operatorPage.locator('#add-appkey-modal, .modal-backdrop');
    await expect(modal).toBeVisible();

    // Fill key name
    const nameInput = operatorPage.locator('#key-name');
    await nameInput.fill('Personal VS Code Agent');

    // Submit form
    const submitBtn = operatorPage.locator('button:has-text("Generate App Key"), button[type="submit"]');
    await submitBtn.click();

    // Verify key created display and snippet
    await expect(operatorPage.locator('text=App Key Created!')).toBeVisible();
    await expect(operatorPage.locator('text=Ready-to-Use mcp_config.json Snippet:')).toBeVisible();

    // Close modal
    const doneBtn = operatorPage.locator('button:has-text("Done")');
    await doneBtn.click();
    await expect(modal).not.toBeVisible();

    // Verify key appears in table
    const keyRow = operatorPage.locator('tr:has-text("Personal VS Code Agent")');
    await expect(keyRow.first()).toBeVisible();

    // Revoke key with custom confirmation modal
    const revokeBtn = keyRow.first().locator('button:has-text("Revoke"), .btn-danger, button:has(.fa-trash)');
    await revokeBtn.click();

    // Confirm dialog in custom ConfirmModal
    const confirmModal = operatorPage.locator('#custom-confirm-modal, .confirm-modal-card');
    if (await confirmModal.isVisible()) {
      const confirmActionBtn = operatorPage.locator('#confirm-modal-ok-btn, button:has-text("Revoke Key"), button:has-text("Confirm")');
      await confirmActionBtn.click();
    }
  });

  /**
   * @requirement AUTH-SYSTEM-APPKEY-SEPARATION
   * @category AUTH
   * @type PositiveFeature
   * @description Administrators manage segmented views for User Personal Keys and App-Level System Keys
   */
  test('Admin Context: manages segmented App-Level Keys and User Personal Keys', async ({ adminPage }) => {
    await adminPage.goto('/');

    // Navigate to App Keys & Security
    const securityTab = adminPage.locator('button:has-text("App Keys & Security")');
    await expect(securityTab).toBeVisible();
    await securityTab.click();

    // Verify sub-tabs exist for Admin
    const personalSubTab = adminPage.locator('button:has-text("User Personal Keys")');
    const systemSubTab = adminPage.locator('button:has-text("App-Level Keys")');
    const quotasSubTab = adminPage.locator('button:has-text("Custom User Quotas")');

    await expect(personalSubTab).toBeVisible();
    await expect(systemSubTab).toBeVisible();
    await expect(quotasSubTab).toBeVisible();

    // Switch to App-Level Keys
    await systemSubTab.click();
    await expect(adminPage.locator('text=App-Level / System Keys (Shared Integrations)')).toBeVisible();

    // Open create modal as Admin
    const createKeyBtn = adminPage.locator('#btn-open-add-key-modal, button:has-text("Create App Key")').first();
    await createKeyBtn.click();

    // Select App-Level / System Key type
    const keyTypeSelect = adminPage.locator('#key-type');
    if (await keyTypeSelect.isVisible()) {
      await keyTypeSelect.selectOption('system');
    }

    const nameInput = adminPage.locator('#key-name');
    await nameInput.fill('CI/CD Pipeline Daemon');

    const submitBtn = adminPage.locator('button:has-text("Generate App Key"), button[type="submit"]');
    await submitBtn.click();

    await expect(adminPage.locator('text=App Key Created!')).toBeVisible();
    const doneBtn = adminPage.locator('button:has-text("Done")');
    await doneBtn.click();
  });

  /**
   * @requirement AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE
   * @category AUTH
   * @type PositiveFeature
   * @description Administrators can view, set, and delete custom per-user quota overrides
   */
  test('Admin Context: configures custom user quota override', async ({ adminPage }) => {
    await adminPage.goto('/');

    const securityTab = adminPage.locator('button:has-text("App Keys & Security")');
    await securityTab.click();

    // Switch to Custom User Quotas sub-tab
    const quotasSubTab = adminPage.locator('button:has-text("Custom User Quotas")');
    await quotasSubTab.click();

    // Fill quota form
    const usernameInput = adminPage.locator('#quota-username');
    const maxKeysInput = adminPage.locator('#quota-max-keys');
    const saveQuotaBtn = adminPage.locator('#btn-save-quota, button:has-text("Set / Update Quota")');

    await usernameInput.fill('power_developer');
    await maxKeysInput.fill('15');
    await saveQuotaBtn.click();

    // Verify quota entry appears in table
    const quotaRow = adminPage.locator('tr:has-text("power_developer")');
    await expect(quotaRow.first()).toBeVisible();
    await expect(quotaRow.first().locator('text=15')).toBeVisible();

    // Reset quota
    const resetBtn = quotaRow.first().locator('button:has-text("Reset to Default"), button:has(.fa-rotate-left)');
    await resetBtn.click();

    const confirmModal = adminPage.locator('#custom-confirm-modal, .confirm-modal-card');
    if (await confirmModal.isVisible()) {
      const confirmActionBtn = adminPage.locator('#confirm-modal-ok-btn, button:has-text("Reset Quota"), button:has-text("Confirm")');
      await confirmActionBtn.click();
    }
  });

});
