import { test, expect } from '@playwright/test';

test.describe('Interactive OAuth Consent UI Flow', () => {

  /**
   * @requirement AUTH-109
   * @category AUTH
   * @type PositiveFeature
   * @description Renders client name and handles user consent acceptance for multi-tenant OAuth applications.
   */
  test('should render interactive OAuth consent screen and display requesting client name', async ({ page }) => {
    // Navigate directly to consent endpoint with client parameters
    await page.goto('/consent?client_id=slack-integration-101&client_name=Slack%20Integration');

    // Verify header and client identity
    await expect(page.locator('h1')).toContainText('Authorize Access');
    await expect(page.locator('.highlight')).toContainText('Slack Integration');
    await expect(page.locator('p')).toContainText('requesting access to your MCP isolated backend resources');

    // Verify Action buttons
    const authorizeBtn = page.locator('button[name="submit.Accept"]');
    const cancelBtn = page.locator('button[name="submit.Deny"]');
    await expect(authorizeBtn).toBeVisible();
    await expect(cancelBtn).toBeVisible();
  });

});
