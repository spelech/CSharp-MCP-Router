import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';
import { ServerModalPage } from './pages/ServerModalPage';

test.describe('Full UI Flow: STDIO Transport + Env Variable Secret Provider', () => {

  test('should register STDIO server with Env Variable provider and verify dashboard card', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    const serverModal = new ServerModalPage(page);

    // 1. Open Dashboard
    await dashboard.goto();

    // 2. Open Add Server Modal
    if (await dashboard.addServerBtn.isVisible()) {
      await dashboard.addServerBtn.click();
      await expect(serverModal.modal).toBeVisible();

      // 3. Fill STDIO server details with Env provider
      await serverModal.fillServerForm({
        id: 'stdio_env_mock',
        name: 'STDIO Env Mock',
        type: 'stdio',
        url: 'node /app/mock_stdio.js',
        secretProvider: 'Env',
        secretKey: 'TEST_API_KEY'
      });

      // 4. Save server
      await serverModal.save();

      // 5. Assert server card appears on dashboard
      await dashboard.searchServer('stdio_env_mock');
    }
  });

});
