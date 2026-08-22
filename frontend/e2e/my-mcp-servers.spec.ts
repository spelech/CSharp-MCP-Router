import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { ServerModalPage } from './pages/ServerModalPage';

test.describe('My MCP Servers & User Credentials Flow', () => {

  /**
   * @requirement AUTH-05
   * @category AUTH
   * @type PositiveFeature
   * @description Render user-provided servers and configure per-user credentials in My MCP Servers view.
   */
  test('should render user provided servers and allow editing credentials with SQLite schema', async ({ page }) => {
    test.setTimeout(60000);
    const dashboard = new DashboardPage(page);
    const serverModal = new ServerModalPage(page);
    const testServerId = `sqlite_auth_${Date.now()}`;
    const testServerName = `SQLite Auth ${Date.now()}`;

    await dashboard.goto();

    if (await dashboard.addServerBtn.isVisible()) {
      await dashboard.addServerBtn.click();
      await expect(serverModal.modal).toBeVisible();

      await serverModal.fillServerForm({
        id: testServerId,
        name: testServerName,
        type: 'sse',
        url: process.env.MOCK_MCP_SSE_URL || 'http://127.0.0.1:8090/sse',
        secretProvider: 'UserProvided'
      });

      await serverModal.save();
    }

    // Wait for save
    await page.waitForTimeout(2000);

    // Navigate to My MCP Servers
    const tabBtn = page.locator('button:has-text("My MCP Servers")');
    await expect(tabBtn).toBeVisible();
    await tabBtn.click();

    // Verify row content and Auth Missing status
    const firstRow = page.locator('table.data-table tbody tr').filter({ hasText: testServerName }).first();
    await expect(firstRow).toBeVisible();
    await expect(firstRow).toContainText('Auth Missing');

    // Click Edit Auth
    const editBtn = firstRow.locator('button:has-text("Edit Auth")');
    await editBtn.click();

    // Verify modal overlay opens
    const modal = page.locator('.modal-backdrop, .modal-card').first();
    await expect(modal).toBeVisible();
    await expect(modal).toContainText(`Edit Auth for ${testServerName}`);

    // Type JSON into textarea
    const textarea = modal.locator('textarea');
    await expect(textarea).toBeVisible();
    await textarea.fill('{\n  "apiKey": "test-key-123"\n}');

    // Save
    const saveBtn = modal.locator('button:has-text("Save")');
    await saveBtn.click();

    // Verify modal closes and status updates to Auth Configured
    await expect(modal).toBeHidden();
    await expect(firstRow).toContainText('Auth Configured');
  });

});
