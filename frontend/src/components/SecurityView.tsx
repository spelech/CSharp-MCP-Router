import React from 'react';
import { AppKeysCard } from './dashboard/AppKeysCard';
import { RegisteredClientsCard } from './dashboard/RegisteredClientsCard';
import { ClientSetupGuide } from './dashboard/ClientSetupGuide';

export const SecurityView: React.FC = () => {
  return (
    <div id="view-security" className="view-panel active">
      <div style={{ display: 'flex', flexDirection: 'column', gap: '25px', width: '100%' }}>
        <AppKeysCard />
        <RegisteredClientsCard />
        <ClientSetupGuide />
      </div>
    </div>
  );
};
