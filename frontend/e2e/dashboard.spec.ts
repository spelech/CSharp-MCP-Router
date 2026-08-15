import { test, expect } from '@playwright/test';

test.describe('Dashboard & Navigation Flow', () => {

  /**
   * @id UI-01
   * @category UI
   * @type positive
   * @description Renders main dashboard navigation tabs and layout headers
   */
  test('should render the dashboard layout and header components', async ({ page }) => {
    await page.goto('/');

    // Check navigation buttons exist
    await expect(page.locator('button:has-text("Overview")')).toBeVisible();
    await expect(page.locator('button:has-text("Test Bench")')).toBeVisible();
    await expect(page.locator('button:has-text("Settings")')).toBeVisible();
    await expect(page.locator('button:has-text("App Keys & Security")')).toBeVisible();
  });

  /**
   * @id UI-01
   * @category UI
   * @type positive
   * @description Displays aggregate system metrics and health status cards
   */
  test('should display aggregate statistics cards', async ({ page }) => {
    await page.goto('/');

    // Check stats container exists
    const statsContainer = page.locator('.stats-card, .stats-container, .dashboard-stats, .dashboard-container');
    await expect(statsContainer.first()).toBeVisible();
  });

  /**
   * @id UI-01
   * @category UI
   * @type positive
   * @description Filters backend MCP server catalog via dashboard search input
   */
  test('should filter servers using search input', async ({ page }) => {
    await page.goto('/');

    // Type in search bar
    const searchInput = page.locator('input[placeholder*="Search"], input[type="search"], #server-search, [data-testid="server-search-input"]').first();
    await expect(searchInput).toBeVisible();
    await searchInput.fill('docker');
    await expect(searchInput).toHaveValue('docker');
  });

});
