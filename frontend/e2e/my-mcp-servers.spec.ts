import { test, expect } from '@playwright/test';

test.describe('My MCP Servers & User Credentials Flow', () => {

  /**
   * @requirement REQ-UI-MY-SERVERS-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the My MCP Servers view and allows editing user-provided authentication credentials.
   */
  test('should render user provided servers and allow editing credentials', async ({ page }) => {
    // Mock the API responses
    await page.route('**/api/servers', async route => {
      const json = [
        { id: 'server-1', displayName: 'Mock Server 1', secretProvider: 'UserProvided' },
        { id: 'server-2', displayName: 'Mock Server 2', secretProvider: 'Vault' } // Should be filtered out
      ];
      await route.fulfill({ json });
    });

    let mockCredentials: any[] = [];

    await page.route('**/api/user/credentials', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ json: mockCredentials });
      } else {
        await route.continue();
      }
    });

    await page.route('**/api/user/credentials/server-1', async route => {
      if (route.request().method() === 'POST' || route.request().method() === 'PUT') {
        // Mock successful save
        mockCredentials = [{ serverId: 'server-1', hasCredential: true }];
        await route.fulfill({ status: 200, json: { success: true } });
      } else {
        await route.continue();
      }
    });

    await page.goto('/');

    // Navigate to My MCP Servers
    const tabBtn = page.locator('button:has-text("My MCP Servers")');
    await expect(tabBtn).toBeVisible();
    await tabBtn.click();

    // Verify it only shows the UserProvided server
    const tableRows = page.locator('table.data-table tbody tr');
    await expect(tableRows).toHaveCount(1);
    
    // Verify row content and Auth Missing status
    const firstRow = tableRows.first();
    await expect(firstRow).toContainText('Mock Server 1');
    await expect(firstRow).toContainText('Auth Missing');

    // Click Edit Auth
    const editBtn = firstRow.locator('button:has-text("Edit Auth")');
    await editBtn.click();

    // Verify modal overlay opens
    const modal = page.locator('.modal-backdrop, .modal-card').first();
    await expect(modal).toBeVisible();
    await expect(modal).toContainText('Edit Auth for Mock Server 1');

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
