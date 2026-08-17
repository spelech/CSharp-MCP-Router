import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { ServerModalPage } from './pages/ServerModalPage';
import { TestBenchPage } from './pages/TestBenchPage';

test.describe('Full UI Flow: SSE Transport + HashiCorp Vault Secret Provider', () => {

  test('should register SSE server with Vault provider (Mount/Path/Field), verify badge, and run semantic search', async ({ page }) => {
    test.setTimeout(60000);
    const dashboard = new DashboardPage(page);
    const serverModal = new ServerModalPage(page);
    const testbench = new TestBenchPage(page);

    // 1. Open Dashboard
    await dashboard.goto();

    // 2. Open Add Server Modal
    if (await dashboard.addServerBtn.isVisible()) {
      await dashboard.addServerBtn.click();
      await expect(serverModal.modal).toBeVisible();

      // 3. Fill SSE server details with Vault provider
      await serverModal.fillServerForm({
        id: 'sse_vault_mock',
        name: 'SSE Vault Mock',
        type: 'sse',
        url: 'http://mock-mcp-server:8090/sse',
        secretProvider: 'Vault',
        vaultMount: 'secret',
        vaultPath: 'services/vault-test',
        vaultField: 'token'
      });

      // 4. Save server
      await serverModal.save();

      // 5. Assert server card appears on dashboard
      await dashboard.searchServer('SSE Vault Mock');
      const serverId = await dashboard.getServerIdByName('SSE Vault Mock');
      const badge = dashboard.getServerStatusBadge(serverId);
      await expect(badge).toHaveClass(/online/, { timeout: 30000 });
      await page.waitForTimeout(4000); // Give backend more time to fetch and save tools

      // Reload the page to ensure TestBenchView remounts and fetches the latest tools
      await page.reload();

      // 6. Navigate to Test Bench & test semantic search
      await dashboard.navigateToTestbench();
      await testbench.searchTools('health status check');

      // 7. Select new server & execute health tool
      await testbench.selectServerAndTool(serverId, 'health');
    }
  });

});
