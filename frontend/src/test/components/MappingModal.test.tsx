/** @requirement UI-108 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { MappingModal } from '../../components/security/MappingModal';
import { useSettingsStore, GroupMapping } from '../../stores/useSettingsStore';

describe('MappingModal component', () => {
  const existingMapping: GroupMapping = {
    id: 'map-200',
    externalId: 'S-1-5-21-99999-500',
    internalGroup: 'Domain Admins'
  };

  it('renders nothing when isMappingModalOpen is false', () => {
    useSettingsStore.setState({ isMappingModalOpen: false, editingMapping: null });
    const { container } = render(<MappingModal />);
    expect(container.firstChild).toBeNull();
  });

  it('renders create mapping form with empty inputs', () => {
    useSettingsStore.setState({ isMappingModalOpen: true, editingMapping: null });
    render(<MappingModal />);

    expect(screen.getByText('Create Group Mapping')).toBeInTheDocument();
    expect(screen.getByLabelText('External AD SID or OIDC Group')).toHaveValue('');
    expect(screen.getByLabelText('Internal Group Name')).toHaveValue('');
  });

  it('renders edit mapping form pre-filled with mapping data', () => {
    useSettingsStore.setState({ isMappingModalOpen: true, editingMapping: existingMapping });
    render(<MappingModal />);

    expect(screen.getByText('Edit Group Mapping')).toBeInTheDocument();
    expect(screen.getByLabelText('External AD SID or OIDC Group')).toHaveValue('S-1-5-21-99999-500');
    expect(screen.getByLabelText('Internal Group Name')).toHaveValue('Domain Admins');
  });

  it('submits form with externalId and internalGroup', async () => {
    const saveSpy = vi.fn().mockResolvedValue(undefined);
    useSettingsStore.setState({ isMappingModalOpen: true, editingMapping: null, saveMapping: saveSpy });
    render(<MappingModal />);

    fireEvent.change(screen.getByLabelText('External AD SID or OIDC Group'), { target: { value: 'oidc_devs' } });
    fireEvent.change(screen.getByLabelText('Internal Group Name'), { target: { value: 'Developers' } });

    const saveBtn = screen.getByRole('button', { name: /save mapping/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(saveSpy).toHaveBeenCalledWith({
      externalId: 'oidc_devs',
      internalGroup: 'Developers'
    });
  });

  it('closes modal on cancel click', () => {
    const closeSpy = vi.fn();
    useSettingsStore.setState({ isMappingModalOpen: true, editingMapping: null, closeMappingModal: closeSpy });
    render(<MappingModal />);

    const cancelBtn = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelBtn);

    expect(closeSpy).toHaveBeenCalled();
  });
});
