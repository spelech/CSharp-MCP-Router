import React from 'react';
import { AppKeysCard } from '../clients/AppKeysCard';
import { RegisteredClientsCard } from '../clients/RegisteredClientsCard';
import { ClientSetupGuide } from '../clients/ClientSetupGuide';
import { useUserStore } from '../../stores/useUserStore';

export const SecurityView: React.FC = () => {
  const { user } = useUserStore();
  const isAdmin = !!(user?.groups && user.groups.includes('full_admin'));

  return (
    <div id="view-security" className="view-panel active">
      <div style={{ display: 'flex', flexDirection: 'column', gap: '25px', width: '100%' }}>
        <AppKeysCard />
        {isAdmin && <RegisteredClientsCard />}
        <ClientSetupGuide />
      </div>
    </div>
  );
};
