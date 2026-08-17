import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { ServerModalPage } from './pages/ServerModalPage';
import { TestBenchPage } from './pages/TestBenchPage';

test.describe('Full UI Flow: STDIO Transport + Env Variable Secret Provider', () => {

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
    await serverModal.fillServerForm({
      id: 'stdio_env_mock',
      name: 'STDIO Env Mock',
      type: 'stdio',
      url: 'node /app/McpRouter.Tests/mock_stdio.js',
      secretProvider: 'Environment',
      secretKey: 'TEST_API_KEY'
    });

    // 4. Save server
    await serverModal.save();

    // 5. Assert server card appears on dashboard and becomes Connected
    await dashboard.searchServer('STDIO Env Mock');
    const statusText = page.locator('.server-card:has-text("STDIO Env Mock") .status-badge');
    await expect(statusText).toBeVisible({ timeout: 15000 });

    // 6. Navigate to Test Bench
    const testbenchTab = page.locator('button:has-text("Test Bench"), button[data-view="view-testbench"]');
    await expect(testbenchTab.first()).toBeVisible();
    await testbenchTab.first().click();

    // 7. Verify Test Bench view is displayed
    await expect(page.locator('#view-testbench')).toBeVisible();

    // 8. Select our STDIO Env Mock server and call the echo tool
    await testbench.selectServerAndTool('stdio_env_mock', 'echo');

    // 9. Fill in the message argument dynamically generated in the form
    const messageInput = page.locator('#dynamic-form-fields input[type="text"]');
    await expect(messageInput).toBeVisible();
    await messageInput.fill('hello stdio from e2e');

    // 10. Execute the tool
    await testbench.executeTool();

    // 11. Assert that the output console shows successful response from mock_stdio.js
    const outputConsole = page.locator('.output-console, #tool-output, pre.output, .console-box pre');
    await expect(outputConsole).toBeVisible();
    await expect(outputConsole).toContainText('hello stdio from e2e', { timeout: 15000 });
  });

});
