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
      try {
        await this.serverSelect.selectOption(serverId, { timeout: 8000 });
      } catch {
        const options = await this.serverSelect.locator('option').all();
        if (options.length > 1) {
          await this.serverSelect.selectOption({ index: 1 });
        }
      }
    }
    if (toolName && await this.toolSelect.isVisible()) {
      try {
        await this.toolSelect.selectOption({ label: toolName }, { timeout: 8000 });
      } catch {
        const toolOptions = await this.toolSelect.locator('option').all();
        if (toolOptions.length > 1) {
          await this.toolSelect.selectOption({ index: 1 });
        }
      }
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
