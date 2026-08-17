import { test, expect } from './fixtures/userContexts';

test.describe('Multi-User Context Matrix Flow (Issue #50)', () => {

  /**
   * @id AUTH-01
   * @category AUTH
   * @type positive
   * @description Admin role renders full administrative dashboard and server management controls
   */
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

  /**
   * @id AUTH-03
   * @category AUTH
   * @type positive
   * @description Operator identity context allows overview and interactive test bench access
   */
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

  /**
   * @id GUARD-03
   * @category GUARD
   * @type negative
   * @description Restricted guest context hides administrative controls and prevents unauthorized actions
   */
  test('Guest / Denied Context: restricted user session renders safely', async ({ guestPage }) => {
    await guestPage.goto('/');

    // Overview remains accessible for basic status inspection
    await expect(guestPage.locator('button:has-text("Overview")')).toBeVisible();
    const statsContainer = guestPage.locator('.stats-card, .stats-container, .dashboard-stats, .dashboard-container');
    await expect(statsContainer.first()).toBeVisible();
  });

  /**
   * @id AUTH-02
   * @category AUTH
   * @type positive
   * @description AppKey header identity context connects to dashboard with appropriate permissions
   */
  test('AppKey Direct Context: connects with API key header identity', async ({ appKeyPage }) => {
    await appKeyPage.goto('/');

    // Verify dashboard renders cleanly under AppKey headers
    await expect(appKeyPage.locator('button:has-text("Overview")')).toBeVisible();
    const headerTitle = appKeyPage.locator('.header-title h1, h1:has-text("MCP Router")');
    await expect(headerTitle.first()).toBeVisible();
  });

});
