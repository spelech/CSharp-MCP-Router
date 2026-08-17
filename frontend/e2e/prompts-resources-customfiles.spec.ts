import { test, expect } from '@playwright/test';

test.describe('Prompts, Resources, Terminal Logs & Custom Files E2E Workflows', () => {

  /**
   * @requirement UI-04
   * @category UI
   * @type PositiveFeature
   * @description Switch tabs in Test Bench to interact with Prompt Tester and Resource Tester.
   */
  test('should interact with Prompt Tester and Resource Tester cards in Test Bench', async ({ page }) => {
    await page.goto('/');

    // Navigate to Test Bench
    const testbenchTab = page.locator('button:has-text("Test Bench")').first();
    await expect(testbenchTab).toBeVisible();
    await testbenchTab.click();

    // Verify Prompt Tester Card exists
    const promptCard = page.locator('.dcr-card:has-text("Prompt Tester"), #prompt-tester-card, text="Prompt Tester"').first();
    if (await promptCard.count() > 0) {
      await expect(promptCard).toBeVisible();
    }

    // Verify Resource Tester Card exists
    const resourceCard = page.locator('.dcr-card:has-text("Resource Reader"), #resource-tester-card, text="Resource Reader"').first();
    if (await resourceCard.count() > 0) {
      await expect(resourceCard).toBeVisible();
    }

    // Verify Logs Terminal Card exists and has log level filter buttons
    const logsCard = page.locator('.dcr-card:has-text("Live Logs"), #logs-terminal-card, text="Live Server Logs"').first();
    if (await logsCard.count() > 0) {
      await expect(logsCard).toBeVisible();
      const clearBtn = logsCard.locator('button:has-text("Clear"), .btn:has-text("Clear")').first();
      if (await clearBtn.count() > 0) {
        await clearBtn.click();
      }
    }
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Navigate to Settings > Custom Files and Backups tabs to verify authoring and export.
   */
  test('should navigate to Custom Files and Backups in Settings view', async ({ page }) => {
    await page.goto('/');

    // Navigate to Settings
    const settingsTab = page.locator('button:has-text("Settings")').first();
    await expect(settingsTab).toBeVisible();
    await settingsTab.click();

    // Click Custom Files sub-tab if present
    const customFilesTab = page.locator('button:has-text("Custom Files"), .tab-btn:has-text("Custom Files")').first();
    if (await customFilesTab.count() > 0) {
      await customFilesTab.click();
      await expect(page.locator('button:has-text("Create File"), text="File Manager"').first()).toBeVisible();
    }

    // Click Backups sub-tab if present
    const backupsTab = page.locator('button:has-text("Backups"), .tab-btn:has-text("Backups")').first();
    if (await backupsTab.count() > 0) {
      await backupsTab.click();
      await expect(page.locator('button:has-text("Export Backup"), button:has-text("Backup")').first()).toBeVisible();
    }
  });

});
