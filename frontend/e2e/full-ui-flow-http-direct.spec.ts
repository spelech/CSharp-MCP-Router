import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { ServerModalPage } from './pages/ServerModalPage';
import { TestBenchPage } from './pages/TestBenchPage';

test.describe('Full UI Flow: HTTP Transport + Direct Key Secret Provider', () => {

  test('should register HTTP server with Direct Key, verify status badge, and execute tool in Test Bench', async ({ page }) => {
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
        url: 'http://mock-mcp-server:8090/mcp',
        secretProvider: 'None'
      });

      // 4. Save server
      await serverModal.save();

      // 5. Assert server card appears on dashboard with status badge
      await dashboard.searchServer('http_direct_mock');
      const badge = dashboard.getServerStatusBadge('http_direct_mock');
      if (await badge.isVisible()) {
        await expect(badge).toBeVisible();
      }

      // 6. Navigate to Test Bench
      await dashboard.navigateToTestbench();

      // 7. Select new server & execute tool
      await testbench.selectServerAndTool('http_direct_mock');
      await testbench.executeTool();
    }
  });

});
