import { loadServers, toggleServer, openAddModal, openEditModal, closeModal, saveServer, deleteServer, reconnectServer } from './servers.js';
import { loadClients, openAddClientModal, closeClientModal, handleClientSubmit, deleteClient } from './clients.js';
import { initTester } from './tester.js';
import { initLogs, stopLogging } from './logs.js';
import { initSettings } from './settings.js';
import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

document.addEventListener('DOMContentLoaded', () => {
    // 1. Initial Page Load
    loadUser();
    loadServers();
    loadClients();
    setupGlobalNavigation();
    initTheme();
    
    // 2. Poll servers status every 10s
    setInterval(loadServers, 10000);

    // 3. Expose CRUD/Modal methods to window object so inline onclick attributes continue to work
    window.openAddModal = openAddModal;
    window.openEditModal = openEditModal;
    window.closeModal = closeModal;
    window.saveServer = saveServer;
    window.deleteServer = deleteServer;
    window.toggleServer = toggleServer;
    window.reconnectServer = reconnectServer;

    window.openAddClientModal = openAddClientModal;
    window.closeClientModal = closeClientModal;
    window.handleClientSubmit = handleClientSubmit;
    window.deleteClient = deleteClient;

    // Attach form submit listeners
    document.getElementById('server-form').addEventListener('submit', saveServer);
    document.getElementById('client-form').addEventListener('submit', handleClientSubmit);
});

function initTheme() {
    const toggleBtn = document.getElementById('theme-toggle');
    if (!toggleBtn) return;

    const savedTheme = localStorage.getItem('mcp-theme') || 'dark';
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeIcon(savedTheme);

    toggleBtn.addEventListener('click', () => {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', newTheme);
        localStorage.setItem('mcp-theme', newTheme);
        updateThemeIcon(newTheme);
    });
}

function updateThemeIcon(theme) {
    const toggleIcon = document.querySelector('#theme-toggle i');
    if (!toggleIcon) return;
    if (theme === 'light') {
        toggleIcon.className = 'fa-solid fa-sun';
    } else {
        toggleIcon.className = 'fa-solid fa-moon';
    }
}

async function loadUser() {
    try {
        const data = await apiRequest('/api/me');
        if (data.authenticated) {
            const display = document.getElementById('user-display');
            const container = document.getElementById('user-status-item');
            
            const isAdmin = data.groups && data.groups.includes('full_admin');
            const roleIcon = isAdmin 
                ? '<i class="fa-solid fa-user-shield" style="color: var(--accent); margin-right:4px;"></i>' 
                : '<i class="fa-solid fa-user" style="margin-right:4px;"></i>';
            const groupText = isAdmin ? 'Admin' : 'User';
            
            display.innerHTML = `${roleIcon} ${escapeHtml(data.name)} (${groupText})`;
            container.style.display = 'block';
        }
    } catch (error) {
        console.error('Error loading user profile:', error);
    }
}

function setupGlobalNavigation() {
    // Top-level Navigation Tabs (Overview vs Test Bench vs Settings)
    const tabBtns = document.querySelectorAll('.tab-btn');
    const viewPanels = document.querySelectorAll('.view-panel');

    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetView = btn.dataset.view;

            tabBtns.forEach(b => b.classList.remove('active'));
            viewPanels.forEach(p => p.classList.remove('active'));

            btn.classList.add('active');
            document.getElementById(targetView).classList.add('active');

            // Initialize or stop modules on view transitions
            if (targetView === 'view-testbench') {
                initTester();
                initLogs();
            } else {
                stopLogging();
            }

            if (targetView === 'view-settings') {
                initSettings();
            }
        });
    });

    // Sub-Tabs inside Test Bench (Form Input vs Raw JSON Input)
    const subTabBtns = document.querySelectorAll('.tester-tab-btn');
    const subTabContents = document.querySelectorAll('.tester-tab-content');

    subTabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetTab = btn.dataset.tab;

            subTabBtns.forEach(b => b.classList.remove('active'));
            subTabContents.forEach(c => c.classList.remove('active'));

            btn.classList.add('active');
            document.getElementById(`tester-tab-${targetTab}`).classList.add('active');
        });
    });
}
