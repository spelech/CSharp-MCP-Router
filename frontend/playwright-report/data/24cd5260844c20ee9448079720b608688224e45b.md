# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: full-ui-flow-stdio-env.spec.ts >> Full UI Flow: STDIO Transport + Env Variable Secret Provider >> should register STDIO server, verify card, and execute echo tool via Test Bench
- Location: e2e/full-ui-flow-stdio-env.spec.ts:8:3

# Error details

```
Error: browserType.launch: Executable doesn't exist at /home/jules/.cache/ms-playwright/chromium_headless_shell-1234/chrome-headless-shell-linux64/chrome-headless-shell
╔════════════════════════════════════════════════════════════╗
║ Looks like Playwright was just installed or updated.       ║
║ Please run the following command to download new browsers: ║
║                                                            ║
║     npx playwright install                                 ║
║                                                            ║
║ <3 Playwright Team                                         ║
╚════════════════════════════════════════════════════════════╝
```