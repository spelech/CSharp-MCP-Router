import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { ClientModal } from '../../components/clients/ClientModal';
import { useClientStore } from '../../stores/useClientStore';

describe('ClientModal component', () => {
  beforeEach(() => {
    Object.assign(navigator, {
      clipboard: {
        writeText: vi.fn().mockResolvedValue(undefined),
      },
    });
  });

  it('renders nothing when isAddClientOpen is false', () => {
    useClientStore.setState({ isAddClientOpen: false, createdClientResult: null });
    const { container } = render(<ClientModal />);
    expect(container.firstChild).toBeNull();
  });

  /**
   * @requirement UI-30
   * @category UI
   * @type PositiveFeature
   * @description Renders client registration form with inputs for name, client type, redirect URIs, grant types, scopes, and expiration.
   */
  it('renders client registration form with rich OAuth fields and cancel button', () => {
    const closeSpy = vi.fn();
    useClientStore.setState({ isAddClientOpen: true, createdClientResult: null, closeClientModal: closeSpy });
    render(<ClientModal />);

    expect(screen.getByText(/Register New Client/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Client Name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Client Type/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Redirect URIs/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Roles \/ Scopes/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Expiration/i)).toBeInTheDocument();

    // Check grant types checkboxes
    expect(screen.getByLabelText(/authorization_code/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/refresh_token/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/client_credentials/i)).toBeInTheDocument();

    const cancelBtn = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelBtn);
    expect(closeSpy).toHaveBeenCalled();
  });

  /**
   * @requirement UI-30
   * @category UI
   * @type PositiveFeature
   * @description Submits registration form with parsed scopes, redirect URIs, grant types, and client metadata.
   */
  it('submits registration form with parsed scopes array and OAuth metadata', async () => {
    const registerSpy = vi.fn().mockResolvedValue(undefined);
    useClientStore.setState({ isAddClientOpen: true, createdClientResult: null, registerClient: registerSpy });
    render(<ClientModal />);

    fireEvent.change(screen.getByLabelText(/Client Name/i), {
      target: { value: 'VSCode Dev Agent' }
    });
    fireEvent.change(screen.getByLabelText(/Client Type/i), {
      target: { value: 'confidential' }
    });
    fireEvent.change(screen.getByLabelText(/Redirect URIs/i), {
      target: { value: 'https://oauth.pstmn.io/v1/callback, http://localhost:3000/callback' }
    });
    fireEvent.change(screen.getByLabelText(/Roles \/ Scopes/i), {
      target: { value: 'mcp_client, admin, ha_write' }
    });

    const submitBtn = screen.getByRole('button', { name: /generate client/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(registerSpy).toHaveBeenCalledWith(
      'VSCode Dev Agent',
      ['mcp_client', 'admin', 'ha_write'],
      ['https://oauth.pstmn.io/v1/callback', 'http://localhost:3000/callback'],
      ['authorization_code', 'refresh_token', 'client_credentials'],
      'confidential',
      undefined
    );
  });

  /**
   * @requirement UI-30
   * @category UI
   * @type PositiveFeature
   * @description Renders one-time secret display result card when createdClientResult is populated with copy buttons.
   */
  it('renders one-time secret display result card with copy buttons when createdClientResult is populated', () => {
    const closeSpy = vi.fn();
    useClientStore.setState({
      isAddClientOpen: true,
      createdClientResult: {
        id: 'client-uuid-123',
        clientId: 'generated-client-id-xyz',
        clientSecret: 'mcp_secret_token_abcdef123456',
        displayName: 'Test App',
        scopes: ['mcp_client']
      },
      closeClientModal: closeSpy
    });

    render(<ClientModal />);

    expect(screen.getByText('Client Created Successfully!')).toBeInTheDocument();
    expect(screen.getByText('generated-client-id-xyz')).toBeInTheDocument();
    expect(screen.getByText('mcp_secret_token_abcdef123456')).toBeInTheDocument();
    expect(screen.getByText(/save this secret now\. it will not be shown again\./i)).toBeInTheDocument();

    const copyBtns = screen.getAllByRole('button', { name: /copy/i });
    expect(copyBtns.length).toBeGreaterThanOrEqual(2);
    fireEvent.click(copyBtns[0]);
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('generated-client-id-xyz');

    fireEvent.click(copyBtns[1]);
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('mcp_secret_token_abcdef123456');

    const closeBtn = screen.getByRole('button', { name: /close/i });
    fireEvent.click(closeBtn);
    expect(closeSpy).toHaveBeenCalled();
  });
});
