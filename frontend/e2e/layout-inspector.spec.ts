import { test, expect } from '@playwright/test';
import { LayoutInspector, getDevicePreset } from 'playwright-layout-inspector';

test.describe('Dashboard Layout & UX Audit', () => {

  test.beforeEach(async ({ page }) => {
    await page.route('**/api/config/branding', async (route) => {
      await route.fulfill({ json: { title: 'MCP Router Gateway', logoUrl: '' } });
    });
    await page.route('**/api/user/me', async (route) => {
      await route.fulfill({
        json: {
          username: 'admin',
          displayName: 'Administrator',
          groups: ['full_admin', 'house_member'],
          isAdmin: true
        }
      });
    });
    await page.route('**/api/servers', async (route) => {
      await route.fulfill({
        json: [
          {
            id: 'mock-docker',
            displayName: 'Docker MCP Server',
            type: 'sse',
            url: 'http://localhost:8022/sse',
            enabled: true,
            connectionStatus: 'Connected',
            categories: ['Containerization'],
            toolsCount: 5,
            promptsCount: 2,
            resourcesCount: 1
          }
        ]
      });
    });
    await page.route('**/api/**', async (route) => {
      await route.fulfill({ json: {} });
    });
  });

  /**
   * @requirement UI-07
   * @category UI
   * @type PositiveFeature
   * @description Audits desktop viewport layout for zero horizontal overflow and high UX score.
   */
  test('should pass layout audit on desktop 1080p viewport', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const inspector = new LayoutInspector(page);
    const result = await inspector.audit({
      device: getDevicePreset('Desktop 1080p'),
      includeScreenshot: false,
    });

    expect(result.overflowIssues.length).toBe(0);
    expect(result.uxScore.totalScore).toBeGreaterThanOrEqual(85);
  });

  /**
   * @requirement UI-07
   * @category UI
   * @type PositiveFeature
   * @description Audits mobile viewport layout (Samsung Galaxy S25+) for zero horizontal overflow and high UX score.
   */
  test('should pass layout audit on Samsung Galaxy S25+ mobile viewport', async ({ page }) => {
    const s25plus = getDevicePreset('Samsung Galaxy S25+');
    await page.setViewportSize({ width: s25plus.width, height: s25plus.height });
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const inspector = new LayoutInspector(page);
    const result = await inspector.audit({
      device: s25plus,
      includeScreenshot: false,
    });

    expect(result.overflowIssues.length).toBe(0);
    expect(result.uxScore.totalScore).toBeGreaterThanOrEqual(80);
  });

});
