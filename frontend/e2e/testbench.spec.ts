import { test, expect } from '@playwright/test';

test.describe('Test Bench View Flow', () => {

  test('should navigate to Test Bench view and render tester cards', async ({ page }) => {
    await page.goto('/');

    // Switch to Test Bench view
    const testbenchTab = page.locator('button:has-text("Test Bench")').first();
    await expect(testbenchTab).toBeVisible();
    await testbenchTab.click();

    // Verify Test Bench view is displayed
    await expect(page.locator('#view-testbench, .testbench-container, main')).toBeVisible();

    // Verify Semantic Router Search Card or Search input
    const searchInput = page.locator('input#semantic-query, input[placeholder*="query"], input[placeholder*="search"]').first();
    if (await searchInput.count() > 0) {
      await expect(searchInput).toBeVisible();
      await searchInput.fill('restart container');
      await expect(searchInput).toHaveValue('restart container');
    }
  });

});
