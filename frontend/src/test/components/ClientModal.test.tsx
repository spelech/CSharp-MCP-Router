import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { ClientModal } from '../../components/clients/ClientModal';
import { useClientStore } from '../../stores/useClientStore';

describe('ClientModal component', () => {
  it('renders nothing when isAddClientOpen is false', () => {
    useClientStore.setState({ isAddClientOpen: false, createdClientResult: null });
    const { container } = render(<ClientModal />);
    expect(container.firstChild).toBeNull();
  });

  it('renders client registration form with inputs and cancel button', () => {
    const closeSpy = vi.fn();
    useClientStore.setState({ isAddClientOpen: true, createdClientResult: null, closeClientModal: closeSpy });
    render(<ClientModal />);

    expect(screen.getByText('Register New Client')).toBeInTheDocument();
    expect(screen.getByLabelText('Client Name (Display Name)')).toBeInTheDocument();
    expect(screen.getByLabelText('Roles / Scopes (Comma-separated)')).toBeInTheDocument();

    const cancelBtn = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelBtn);
    expect(closeSpy).toHaveBeenCalled();
  });

  it('submits registration form with parsed scopes array', async () => {
    const registerSpy = vi.fn().mockResolvedValue(undefined);
    useClientStore.setState({ isAddClientOpen: true, createdClientResult: null, registerClient: registerSpy });
    render(<ClientModal />);

    fireEvent.change(screen.getByLabelText('Client Name (Display Name)'), {
      target: { value: 'VSCode Dev Agent' }
    });
    fireEvent.change(screen.getByLabelText('Roles / Scopes (Comma-separated)'), {
      target: { value: 'mcp_client, admin, ha_write' }
    });

    const submitBtn = screen.getByRole('button', { name: /generate client/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(registerSpy).toHaveBeenCalledWith('VSCode Dev Agent', ['mcp_client', 'admin', 'ha_write']);
  });

  it('renders one-time secret display result card when createdClientResult is populated', () => {
    const closeSpy = vi.fn();
    useClientStore.setState({
      isAddClientOpen: true,
      createdClientResult: {
        clientId: 'generated-client-id-xyz',
        clientSecret: 'mcp_secret_token_abcdef123456'
      },
      closeClientModal: closeSpy
    });

    render(<ClientModal />);

    expect(screen.getByText('Client Created Successfully!')).toBeInTheDocument();
    expect(screen.getByText('generated-client-id-xyz')).toBeInTheDocument();
    expect(screen.getByText('mcp_secret_token_abcdef123456')).toBeInTheDocument();
    expect(screen.getByText(/save this secret now\. it will not be shown again\./i)).toBeInTheDocument();

    const closeBtn = screen.getByRole('button', { name: /close/i });
    fireEvent.click(closeBtn);
    expect(closeSpy).toHaveBeenCalled();
  });
});
