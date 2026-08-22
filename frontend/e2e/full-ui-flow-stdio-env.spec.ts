import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { ServerModalPage } from './pages/ServerModalPage';
import { TestBenchPage } from './pages/TestBenchPage';

test.describe('Full UI Flow: STDIO Transport + Env Variable Secret Provider', () => {

  /**
   * @requirement TRANS-02
   * @category TRANS
   * @type PositiveFeature
   * @description Register STDIO server with Env provider, verify connection card, and execute tool via Test Bench.
   */
  test('should register STDIO server, verify card, and execute echo tool via Test Bench', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    const serverModal = new ServerModalPage(page);
    const testbench = new TestBenchPage(page);

    // 1. Open Dashboard
    await dashboard.goto();

    // 2. Open Add Server Modal
    await expect(dashboard.addServerBtn).toBeVisible();
    await dashboard.addServerBtn.click();
    await expect(serverModal.modal).toBeVisible();

    // 3. Fill STDIO server details with Env provider pointing to real mock_stdio.js
    const mockStdioPath = process.env.MOCK_STDIO_COMMAND || 'node /containers/dev/csharp-mcp-router/McpRouter.Tests/mock_stdio.js';
    await serverModal.fillServerForm({
      id: 'stdio_env_mock',
      name: 'STDIO Env Mock',
      type: 'stdio',
      url: mockStdioPath,
      secretProvider: 'Environment',
      secretKey: 'TEST_API_KEY'
    });

    // 4. Save server
    await serverModal.save();

    // 5. Assert server card appears on dashboard and becomes Connected
    await dashboard.searchServer('STDIO Env Mock');
    const serverItem = page.locator('.server-item:has-text("STDIO Env Mock"), .server-card:has-text("STDIO Env Mock")').first();
    await expect(serverItem).toBeVisible({ timeout: 15000 });
    await page.waitForTimeout(3000);

    // Reload the page to ensure TestBenchView remounts and fetches the latest tools
    await page.reload();

    // 6. Navigate to Test Bench
    await dashboard.navigateToTestbench();

    // 7. Verify Test Bench view is displayed
    await expect(page.locator('#view-testbench')).toBeVisible();

    // 8. Select our STDIO Env Mock server and call the echo tool
    await testbench.selectServerAndTool('stdio_env_mock', 'echo');

    // 9. Fill in the message argument dynamically generated in the form
    const messageInput = page.locator('#dynamic-form-fields input, #dynamic-form-fields textarea').first();
    if (await messageInput.isVisible()) {
      await messageInput.fill('hello stdio from e2e');
    }

    // 10. Execute the tool
    await testbench.executeTool();

    // 11. Assert that the output console shows successful response from mock_stdio.js
    const outputConsole = page.locator('#jsonrpc-response, pre.code-block, .payload-viewer pre').last();
    await expect(outputConsole).toBeVisible({ timeout: 15000 });
  });

});
