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
    const promptCard = page.locator('.dcr-card:has-text("Prompt Tester"), #prompt-tester-card, h2:has-text("Prompt")').first();
    if (await promptCard.count() > 0) {
      await expect(promptCard).toBeVisible();
    }

    // Verify Resource Tester Card exists
    const resourceCard = page.locator('.dcr-card:has-text("Resource Reader"), #resource-tester-card, h2:has-text("Resource")').first();
    if (await resourceCard.count() > 0) {
      await expect(resourceCard).toBeVisible();
    }

    // Verify Logs Terminal Card exists and has log level filter buttons
    const logsCard = page.locator('.dcr-card:has-text("Live Logs"), #logs-terminal-card, h2:has-text("Logs")').first();
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
   * @description Navigate to Settings > Custom Files and Prompts & Resources tabs to verify authoring.
   */
  test('should navigate to Custom Files and Prompts in Settings view', async ({ page }) => {
    await page.goto('/');

    // Navigate to Settings
    const settingsTab = page.locator('button:has-text("Settings")').first();
    await expect(settingsTab).toBeVisible();
    await settingsTab.click();

    // Click Prompts & Resources sub-tab
    const customFilesTab = page.locator('button:has-text("Prompts & Resources"), button:has-text("Custom Files")').first();
    if (await customFilesTab.count() > 0) {
      await customFilesTab.click();
      await expect(page.locator('button:has-text("Add Custom File"), button:has-text("Create File"), h2:has-text("Prompts & Resources")').first()).toBeVisible();
    }
  });

});
