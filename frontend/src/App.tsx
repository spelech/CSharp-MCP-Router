import React, { useState } from 'react';
import { Header } from './components/Header';
import { DashboardView } from './components/DashboardView';
import { SecurityView } from './components/SecurityView';
import { TestBenchView } from './components/TestBenchView';
import { SettingsView } from './components/SettingsView';

import { ServerModal } from './components/ServerModal';
import { ServerInspectModal } from './components/ServerInspectModal';
import { ClientModal } from './components/ClientModal';
import { AppKeyModal } from './components/AppKeyModal';
import { CustomFileModal } from './components/CustomFileModal';
import { PolicyModal } from './components/PolicyModal';
import { MappingModal } from './components/MappingModal';
import { Toasts } from './components/Toasts';

const App: React.FC = () => {
  const [currentView, setCurrentView] = useState<'dashboard' | 'security' | 'testbench' | 'settings'>('dashboard');

  return (
    <>
      <div className="background-decor">
        <div className="circle circle-1"></div>
        <div className="circle circle-2"></div>
      </div>

      <div className="dashboard-container">
        <Header />

        <nav className="tabs-nav">
          <button
            className={`tab-btn ${currentView === 'dashboard' ? 'active' : ''}`}
            onClick={() => setCurrentView('dashboard')}
          >
            <i className="fa-solid fa-gauge"></i> Overview
          </button>
          <button
            className={`tab-btn ${currentView === 'security' ? 'active' : ''}`}
            onClick={() => setCurrentView('security')}
          >
            <i className="fa-solid fa-key"></i> App Keys & Security
          </button>
          <button
            className={`tab-btn ${currentView === 'testbench' ? 'active' : ''}`}
            onClick={() => setCurrentView('testbench')}
          >
            <i className="fa-solid fa-vial"></i> Test Bench
          </button>
          <button
            className={`tab-btn ${currentView === 'settings' ? 'active' : ''}`}
            onClick={() => setCurrentView('settings')}
          >
            <i className="fa-solid fa-gear"></i> Settings
          </button>
        </nav>

        {currentView === 'dashboard' && <DashboardView />}
        {currentView === 'security' && <SecurityView />}
        {currentView === 'testbench' && <TestBenchView />}
        {currentView === 'settings' && <SettingsView />}

        <footer className="dashboard-footer">
          <p>WileyRiley Infrastructure &bull; Protected by TinyAuth Forward Auth &bull; 2026</p>
        </footer>
      </div>

      {/* Modals */}
      <ServerModal />
      <ServerInspectModal />
      <ClientModal />
      <AppKeyModal />
      <CustomFileModal />
      <PolicyModal />
      <MappingModal />

      {/* Toast Manager */}
      <Toasts />
    </>
  );
};

export default App;
