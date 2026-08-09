const puppeteer = require('puppeteer');

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
        'Remote-User': process.env.SSO_USER || 'steve',
        'Remote-Groups': process.env.SSO_GROUPS || 'full_admin,house_member',
        'Remote-Name': process.env.SSO_NAME || 'Steve Pelech'
    });
    
    const url = process.env.DASHBOARD_URL || 'http://localhost:8080/';
    console.log(`Navigating to ${url}...`);
    
    // Navigate and wait for dashboard container to render
    await page.goto(url, { waitUntil: 'networkidle2' });
    console.log("Page loaded. Waiting for dashboard container...");
    await page.waitForSelector('.dashboard-container', { timeout: 15000 });
    
    // Wait a brief moment for dynamic data to load
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 2000)));
    
    // Take dashboard overview screenshot
    console.log("Taking dashboard overview screenshot...");
    await page.screenshot({ path: '/home/pptruser/app/screenshots/dashboard.jpg', quality: 90, type: 'jpeg' });
    
    // Switch to Security tab (2nd button in nav.tabs-nav)
    console.log("Switching to App Keys & Security tab...");
    const tabButtons = await page.$$('nav.tabs-nav button');
    if (tabButtons.length >= 2) {
        await tabButtons[1].click();
        await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 1500)));
        console.log("Taking Security View screenshot...");
        await page.screenshot({ path: '/home/pptruser/app/screenshots/security_view.jpg', quality: 90, type: 'jpeg' });
        
        // Open Create App Key modal
        console.log("Opening Create App Key modal...");
        const createKeyBtn = await page.$('.dcr-card button');
        if (createKeyBtn) {
            await createKeyBtn.click();
            await page.waitForSelector('#add-appkey-modal', { visible: true });
            await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 500)));
            console.log("Taking App Key Modal screenshot...");
            await page.screenshot({ path: '/home/pptruser/app/screenshots/add_appkey_modal.jpg', quality: 90, type: 'jpeg' });
            
            // Close modal
            const closeBtn = await page.$('#add-appkey-modal .btn-close');
            if (closeBtn) await closeBtn.click();
            await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 500)));
        }
    }
    
    // Switch to Test Bench tab (3rd button)
    console.log("Switching to Test Bench tab...");
    const refreshedTabs = await page.$$('nav.tabs-nav button');
    if (refreshedTabs.length >= 3) {
        await refreshedTabs[2].click();
        await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 1500)));
        console.log("Taking Test Bench screenshot...");
        await page.screenshot({ path: '/home/pptruser/app/screenshots/test_bench_view.jpg', quality: 90, type: 'jpeg' });
    }
    
    console.log("Finished taking all screenshots!");
    await browser.close();
})();
