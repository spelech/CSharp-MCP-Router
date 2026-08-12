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
    this.searchInput = page.getByTestId('server-search-input');
    this.sortBySelect = page.getByTestId('sort-by-select');
    this.groupBySelect = page.getByTestId('group-by-select');
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
    return this.page.locator(`.server-card[data-server-id="${serverId}"], #server-card-${serverId}, .server-item:has-text("${serverId}")`);
  }

  getServerStatusBadge(serverId: string): Locator {
    return this.page.locator(`[data-server-id="${serverId}"] .indicator, [data-server-id="${serverId}"] .status-badge`);
  }

  getServerStatusBadgeByName(name: string): Locator {
    return this.page.locator(`.server-card:has-text("${name}"), .server-item:has-text("${name}")`).locator('.status-badge, .indicator').first();
  }

  async getServerIdByName(name: string): Promise<string> {
    const card = this.page.locator(`.server-card:has-text("${name}"), .server-item:has-text("${name}")`).first();
    await card.waitFor({ state: 'visible' });
    const id = await card.getAttribute('data-server-id');
    return id || '';
  }
}
