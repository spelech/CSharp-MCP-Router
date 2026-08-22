import { Page, Locator } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;
  readonly navDashboardBtn: Locator;
  readonly navTestbenchBtn: Locator;
  readonly navSettingsBtn: Locator;
  readonly navClientsBtn: Locator;
  readonly addServerBtn: Locator;
  readonly searchInput: Locator;
  readonly sortBySelect: Locator;
  readonly groupBySelect: Locator;

  constructor(page: Page) {
    this.page = page;
    this.navDashboardBtn = page.getByRole('button', { name: /Overview/i });
    this.navTestbenchBtn = page.getByRole('button', { name: /Test Bench/i });
    this.navSettingsBtn = page.getByRole('button', { name: /Settings/i });
    this.navClientsBtn = page.getByRole('button', { name: /Clients|App Keys/i });
    this.addServerBtn = page.getByRole('button', { name: /Add Server/i });
    this.searchInput = page.locator('#server-search, [data-testid="server-search-input"]');
    this.sortBySelect = page.locator('#server-sort-by, [data-testid="sort-by-select"]');
    this.groupBySelect = page.locator('#server-group-by, [data-testid="group-by-select"]');
  }

  async goto() {
    await this.page.goto('/');
  }

  async navigateToTestbench() {
    await this.navTestbenchBtn.click();
  }

  async navigateToSettings() {
    await this.navSettingsBtn.click();
  }

  async navigateToClients() {
    await this.navClientsBtn.click();
  }

  async searchServer(query: string) {
    if (await this.searchInput.isVisible()) {
      await this.searchInput.fill(query);
    }
  }

  getServerCard(serverId: string): Locator {
    return this.page.locator(`[data-server-id="${serverId}"], .server-item:has-text("${serverId}")`);
  }

  getServerStatusBadge(serverId: string): Locator {
    return this.page.locator(`[data-server-id="${serverId}"] .indicator, [data-server-id="${serverId}"] .server-badge, [data-server-id="${serverId}"] .status-badge`);
  }

  getServerStatusBadgeByName(name: string): Locator {
    return this.page.locator(`.server-item:has-text("${name}"), .server-card:has-text("${name}")`).locator('.server-badge, .indicator, .status-badge').first();
  }

  async getServerIdByName(name: string): Promise<string> {
    const card = this.page.locator(`.server-item:has-text("${name}"), .server-card:has-text("${name}")`).first();
    await card.waitFor({ state: 'visible' });
    const id = await card.getAttribute('data-server-id');
    return id || '';
  }
}
