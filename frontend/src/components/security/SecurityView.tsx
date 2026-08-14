import React from 'react';
import { AppKeysCard } from '../clients/AppKeysCard';
import { RegisteredClientsCard } from '../clients/RegisteredClientsCard';
import { ClientSetupGuide } from '../clients/ClientSetupGuide';

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
