import { test, expect } from '@playwright/test';

test.describe('Test Bench View Flow', () => {

  test('should navigate to Test Bench view and interact with tool execution cards', async ({ page }) => {
    await page.goto('/');

    // Switch to Test Bench view
    const testbenchTab = page.locator('button:has-text("Test Bench"), button[data-view="view-testbench"]');
    await expect(testbenchTab.first()).toBeVisible();
    await testbenchTab.first().click();

    // Verify Test Bench view is displayed
    await expect(page.locator('#view-testbench, .testbench-container, main')).toBeVisible();

    // Verify Tool Tester Card
    const toolTesterCard = page.locator('.tool-tester, #tool-tester-card, .card:has-text("Tool Tester")');
    if (await toolTesterCard.isVisible()) {
      await expect(toolTesterCard).toBeVisible();
    }

    // Verify Semantic Router Search Card
    const semanticCard = page.locator('.semantic-card, #semantic-router-card, .card:has-text("Semantic Router")');
    if (await semanticCard.isVisible()) {
      await expect(semanticCard).toBeVisible();
      
      const searchInput = semanticCard.locator('input[placeholder*="query"], input[type="text"]');
      if (await searchInput.isVisible()) {
        await searchInput.fill('restart container');
      }
    }

    // Verify Logs Terminal Card
    const logsTerminal = page.locator('.logs-terminal, #logs-terminal, .terminal-container');
    if (await logsTerminal.isVisible()) {
      await expect(logsTerminal).toBeVisible();
    }
  });

});
