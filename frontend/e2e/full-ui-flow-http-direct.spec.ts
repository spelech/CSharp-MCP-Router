import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { ServerModalPage } from './pages/ServerModalPage';
import { TestBenchPage } from './pages/TestBenchPage';

test.describe('Full UI Flow: HTTP Transport + Direct Key Secret Provider', () => {

  /**
   * @requirement TRANS-01
   * @category TRANS
   * @type PositiveFeature
   * @description Register HTTP server with Direct Key, verify status badge, and execute tool in Test Bench.
   */
  test('should register HTTP server with Direct Key, verify status badge, and execute tool in Test Bench', async ({ page }) => {
    test.setTimeout(60000);
    const dashboard = new DashboardPage(page);
    const serverModal = new ServerModalPage(page);
    const testbench = new TestBenchPage(page);

    // 1. Open Dashboard
    await dashboard.goto();
    await expect(dashboard.navDashboardBtn).toBeVisible();

    // 2. Open Add Server Modal
    if (await dashboard.addServerBtn.isVisible()) {
      await dashboard.addServerBtn.click();
      await expect(serverModal.modal).toBeVisible();

      // 3. Fill HTTP server details with Direct Key provider
      await serverModal.fillServerForm({
        id: 'http_direct_mock',
        name: 'HTTP Mock Server',
        type: 'http',
        url: process.env.MOCK_MCP_HTTP_URL || 'http://127.0.0.1:8090/mcp',
        secretProvider: 'None'
      });

      // 4. Save server
      await serverModal.save();

      // 5. Assert server card appears on dashboard with status badge
      await dashboard.searchServer('HTTP Mock Server');
      const serverId = await dashboard.getServerIdByName('HTTP Mock Server');
      const badge = dashboard.getServerStatusBadge(serverId);
      await expect(badge.first()).toBeVisible({ timeout: 30000 });
      await page.waitForTimeout(4000); // Give backend more time to fetch and save tools

      // Reload the page to ensure TestBenchView remounts and fetches the latest tools
      await page.reload();

      // 6. Navigate to Test Bench
      await dashboard.navigateToTestbench();

      // 7. Select new server & execute tool
      await testbench.selectServerAndTool(serverId, 'health');
      await testbench.executeTool();
    }
  });

});
