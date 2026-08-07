const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

(async () => {
    console.log("Starting Puppeteer browser...");
    const browser = await puppeteer.launch({
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-web-security']
    });
    const page = await browser.newPage();
    
    // Set viewport
    await page.setViewport({ width: 1440, height: 900 });
    
    // Set extra headers to fake the SSO session user
    await page.setExtraHTTPHeaders({
        'Remote-User': process.env.SSO_USER || 'admin',
        'Remote-Groups': process.env.SSO_GROUPS || 'admin',
        'Remote-Name': process.env.SSO_NAME || 'Administrator'
    });
    
    const url = process.env.DASHBOARD_URL || 'http://localhost:8080/';
    console.log(`Navigating to ${url}...`);
    
    // Navigate and wait for server list to render
    await page.goto(url, { waitUntil: 'networkidle2' });
    console.log("Page loaded. Waiting for servers to render...");
    await page.waitForSelector('#servers-list .server-item', { timeout: 10000 });
    
    // Wait a brief moment to ensure connectionStatus states render green
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 1500)));
    
    // Take dashboard screenshot
    console.log("Taking dashboard screenshot...");
    await page.screenshot({ path: '/home/pptruser/app/screenshots/dashboard.jpg', quality: 90, type: 'jpeg' });
    
    // Open Add Server modal
    console.log("Opening Add Server modal...");
    await page.evaluate(() => openAddModal());
    await page.waitForSelector('#server-modal', { visible: true });
    
    // Fill in mock details for screenshot
    await page.evaluate(() => {
        document.getElementById('server-name').value = 'Home Assistant';
        document.getElementById('server-url').value = 'http://ha-mcp:8086/mcp';
        document.getElementById('server-type').value = 'http';
        document.getElementById('server-category').value = 'homecontrol';
        document.getElementById('server-key').value = '••••••••••••••••••••••••';
    });
    
    // Wait a brief moment for layout
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 500)));
    console.log("Taking Add Server modal screenshot...");
    await page.screenshot({ path: '/home/pptruser/app/screenshots/add_server_modal.jpg', quality: 90, type: 'jpeg' });
    
    // Close modal
    await page.evaluate(() => closeModal());
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 300)));
    
    // Switch to Settings tab
    console.log("Switching to Settings tab...");
    await page.click('button[data-view="view-settings"]');
    await page.waitForSelector('#view-settings', { visible: true });
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 500)));
    console.log("Taking Settings view screenshot...");
    await page.screenshot({ path: '/home/pptruser/app/screenshots/settings_view.jpg', quality: 90, type: 'jpeg' });
    
    // Switch to Test Bench tab
    console.log("Switching to Test Bench tab...");
    await page.click('button[data-view="view-testbench"]');
    await page.waitForSelector('#view-testbench', { visible: true });
    
    // Let's populate the dynamic form fields with docker restart tool
    await page.evaluate(async () => {
        // Select docker server
        const serverSelect = document.getElementById('tester-server');
        serverSelect.value = 'docker';
        serverSelect.dispatchEvent(new Event('change'));
        
        // Wait for tools to load
        await new Promise(resolve => setTimeout(resolve, 1500));
        
        // Select docker__restart_container tool
        const toolSelect = document.getElementById('tester-tool');
        const dockerOption = Array.from(toolSelect.options).find(o => o.value.includes('restart_container') || o.value.includes('restart'));
        if (dockerOption) {
            toolSelect.value = dockerOption.value;
            toolSelect.dispatchEvent(new Event('change'));
        }
    });
    
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 1500)));
    console.log("Taking Test Bench view screenshot...");
    await page.screenshot({ path: '/home/pptruser/app/screenshots/test_bench_view.jpg', quality: 90, type: 'jpeg' });
    
    console.log("Finished taking all screenshots!");
    await browser.close();
})();
