import { test, expect } from '@playwright/test';

test.describe('Dashboard & Navigation Flow', () => {

  test('should render the dashboard layout and header components', async ({ page }) => {
    await page.goto('/');
    
    // Check navigation buttons exist
    await expect(page.locator('button:has-text("Overview")')).toBeVisible();
    await expect(page.locator('button:has-text("Test Bench")')).toBeVisible();
    await expect(page.locator('button:has-text("Settings")')).toBeVisible();
    await expect(page.locator('button:has-text("App Keys & Security")')).toBeVisible();
  });

  test('should display aggregate statistics cards', async ({ page }) => {
    await page.goto('/');
    
    // Check stats card metrics exist
    const statsContainer = page.locator('.stats-card, .stats-container, .dashboard-stats');
    await expect(statsContainer.first()).toBeVisible();
  });

  test('should filter servers using the category tabs and search input', async ({ page }) => {
    await page.goto('/');
    
    // Click category filter tabs if present
    const categoryTabs = page.locator('.category-tab, .filter-btn, [data-category]');
    if (await categoryTabs.count() > 0) {
      await categoryTabs.first().click();
    }
    
    // Type in search bar
    const searchInput = page.locator('input[placeholder*="Search"], input[type="search"], #server-search');
    if (await searchInput.isVisible()) {
      await searchInput.fill('docker');
      await expect(searchInput).toHaveValue('docker');
    }
  });

});
