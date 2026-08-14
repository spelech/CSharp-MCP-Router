import React, { useState } from 'react';
import { Header, Footer, Toasts } from './components/shared';
import { DashboardView, ServerModal, ServerInspectModal } from './components/servers';
import { SecurityView, PolicyModal, MappingModal } from './components/security';
import { TestBenchView } from './components/testbench';
import { SettingsView, CustomFileModal } from './components/settings';
import { ClientModal, AppKeyModal } from './components/clients';

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
            <i className="fa-solid fa-key"></i> App Keys &amp; Security
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

        <Footer />
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
