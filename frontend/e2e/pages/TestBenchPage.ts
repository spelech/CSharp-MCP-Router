import { Page, Locator } from '@playwright/test';

export class TestBenchPage {
  readonly page: Page;
  readonly serverSelect: Locator;
  readonly toolSelect: Locator;
  readonly executeBtn: Locator;
  readonly outputConsole: Locator;
  readonly semanticQueryInput: Locator;
  readonly searchToolsBtn: Locator;

  constructor(page: Page) {
    this.page = page;
    this.serverSelect = page.locator('select#tester-server, select[name="server"]');
    this.toolSelect = page.locator('select#tester-tool, select[name="tool"]');
    this.executeBtn = page.getByRole('button', { name: /Execute|Run/i }).first();
    this.outputConsole = page.locator('.output-console, #tool-output, pre.output');
    this.semanticQueryInput = page.locator('input#semantic-query, input[placeholder*="query"]');
    this.searchToolsBtn = page.getByRole('button', { name: /Search Tools/i }).first();
  }

  async selectServerAndTool(serverId: string, toolName?: string) {
    if (await this.serverSelect.isVisible()) {
      await this.serverSelect.selectOption(serverId);
    }
    if (toolName && await this.toolSelect.isVisible()) {
      await this.toolSelect.selectOption({ label: toolName });
    }
  }

  async executeTool() {
    if (await this.executeBtn.isVisible()) {
      await this.executeBtn.click();
    }
  }

  async searchTools(query: string) {
    if (await this.semanticQueryInput.isVisible()) {
      await this.semanticQueryInput.fill(query);
      if (await this.searchToolsBtn.isVisible()) {
        await this.searchToolsBtn.click();
      }
    }
  }
}
