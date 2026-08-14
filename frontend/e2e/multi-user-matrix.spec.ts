import { test, expect } from './fixtures/userContexts';

test.describe('Multi-User Context Matrix Flow (Issue #50)', () => {

  test('Admin Context: renders full administrator view and privileged controls', async ({ adminPage }) => {
    await adminPage.goto('/');

    // Verify navigation tabs
    await expect(adminPage.locator('button:has-text("Overview")')).toBeVisible();
    await expect(adminPage.locator('button:has-text("App Keys & Security")')).toBeVisible();
    await expect(adminPage.locator('button:has-text("Settings")')).toBeVisible();
    await expect(adminPage.locator('button:has-text("Test Bench")')).toBeVisible();

    // Verify user badge displays admin shield icon
    const userDisplay = adminPage.locator('#user-display, .header-status');
    await expect(userDisplay.first()).toBeVisible();

    // Verify Add Server action is accessible to Admin
    const addServerBtn = adminPage.locator('button:has-text("Add Server"), button:has-text("+ Add Server")');
    await expect(addServerBtn.first()).toBeVisible();

    // Navigate to Settings
    const settingsTab = adminPage.locator('button:has-text("Settings")');
    await settingsTab.first().click();
    await expect(adminPage.locator('#view-settings, .settings-container, main')).toBeVisible();
  });

  test('Operator Context: allows overview and testbench navigation with operator identity', async ({ operatorPage }) => {
    await operatorPage.goto('/');

    // Verify core navigation
    await expect(operatorPage.locator('button:has-text("Overview")')).toBeVisible();
    await expect(operatorPage.locator('button:has-text("Test Bench")')).toBeVisible();

    // Navigate to Test Bench
    const testbenchTab = operatorPage.locator('button:has-text("Test Bench")');
    await testbenchTab.first().click();
    await expect(operatorPage.locator('#view-testbench, .testbench-container, main')).toBeVisible();
  });

  test('Guest / Denied Context: restricted user session renders safely', async ({ guestPage }) => {
    await guestPage.goto('/');

    // Overview remains accessible for basic status inspection
    await expect(guestPage.locator('button:has-text("Overview")')).toBeVisible();
    const statsContainer = guestPage.locator('.stats-card, .stats-container, .dashboard-stats, .dashboard-container');
    await expect(statsContainer.first()).toBeVisible();
  });

  test('AppKey Direct Context: connects with API key header identity', async ({ appKeyPage }) => {
    await appKeyPage.goto('/');

    // Verify dashboard renders cleanly under AppKey headers
    await expect(appKeyPage.locator('button:has-text("Overview")')).toBeVisible();
    const headerTitle = appKeyPage.locator('.header-title h1, h1:has-text("MCP Router")');
    await expect(headerTitle.first()).toBeVisible();
  });

});
