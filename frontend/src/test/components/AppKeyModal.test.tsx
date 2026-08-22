import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { AppKeyModal } from '../../components/clients/AppKeyModal';
import { useAppKeyStore } from '../../stores/useAppKeyStore';
import { useUserStore } from '../../stores/useUserStore';

describe('AppKeyModal component', () => {
  beforeEach(() => {
    useUserStore.setState({
      user: {
        authenticated: true,
        username: 'steve',
        name: 'Steve Pelech',
        groups: ['full_admin']
      }
    });
  });

  /**
   * @requirement REQ-AUTH-PERSONAL-APPKEY-CREATE
   * @category AUTH
   * @type FailClosedGuardrail
   * @description Renders nothing when modal is closed.
   */
  it('renders nothing when isCreateModalOpen is false', () => {
    useAppKeyStore.setState({ isCreateModalOpen: false, createdResult: null });
    const { container } = render(<AppKeyModal />);
    expect(container.firstChild).toBeNull();
  });

  /**
   * @requirement REQ-AUTH-SYSTEM-APPKEY-SEPARATION
   * @category AUTH
   * @type PositiveFeature
   * @description Allows admin to create system-level app key.
   */
  it('allows admin to select key type and create system app key', async () => {
    const createSpy = vi.fn().mockResolvedValue(undefined);
    useAppKeyStore.setState({ isCreateModalOpen: true, createdResult: null, createAppKey: createSpy, keyTypeTab: 'system' });
    render(<AppKeyModal />);

    expect(screen.getByText('Create New App Key')).toBeInTheDocument();
    expect(screen.getByLabelText(/Key Type/i)).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText('e.g. My Laptop CLI'), { target: { value: 'CI Runner Key' } });

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(createSpy).toHaveBeenCalledWith({
      name: 'CI Runner Key',
      keyType: 'system',
      username: undefined,
      scopes: ['all'],
      expiresInDays: undefined
    });
  });

  /**
   * @requirement REQ-AUTH-PERSONAL-APPKEY-CREATE
   * @category AUTH
   * @type PositiveFeature
   * @description Non-admin user is locked to personal key with quota feedback.
   */
  it('locks key type to personal key for non-admin and shows quota feedback', async () => {
    useUserStore.setState({
      user: {
        authenticated: true,
        username: 'bob',
        name: 'Bob Smith',
        groups: ['developer']
      }
    });

    const createSpy = vi.fn().mockResolvedValue(undefined);
    useAppKeyStore.setState({
      isCreateModalOpen: true,
      createdResult: null,
      createAppKey: createSpy,
      limits: {
        userMax: 5,
        userActiveKeys: 2,
        globalMax: 50,
        totalActiveKeys: 10,
        isLimitReached: false
      }
    });

    render(<AppKeyModal />);

    expect(screen.getByText('Create Personal App Key')).toBeInTheDocument();
    // Key type dropdown is NOT rendered for non-admins
    expect(screen.queryByLabelText(/Key Type/i)).toBeNull();
    expect(screen.getByText(/Personal Key/i)).toBeInTheDocument();
    expect(screen.getByText(/Remaining Quota:/i)).toHaveTextContent('Remaining Quota: 3 keys left (2 / 5 used)');

    fireEvent.change(screen.getByPlaceholderText('e.g. My Laptop CLI'), { target: { value: 'Bob Laptop' } });

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(createSpy).toHaveBeenCalledWith({
      name: 'Bob Laptop',
      keyType: 'personal',
      username: undefined,
      scopes: ['all'],
      expiresInDays: undefined
    });
  });

  /**
   * @requirement REQ-AUTH-PERSONAL-APPKEY-CREATE
   * @category AUTH
   * @type PositiveFeature
   * @description Handles scope serialization for server scope and custom username for admin.
   */
  it('handles scope serialization for server scope and target username for admin', async () => {
    const createSpy = vi.fn().mockResolvedValue(undefined);
    useAppKeyStore.setState({ isCreateModalOpen: true, createdResult: null, createAppKey: createSpy, keyTypeTab: 'personal' });
    render(<AppKeyModal />);

    fireEvent.change(screen.getByPlaceholderText('e.g. My Laptop CLI'), { target: { value: 'Notes Assistant' } });

    // Target username
    const usernameInput = screen.getByLabelText(/Target Username/i);
    fireEvent.change(usernameInput, { target: { value: 'charlie' } });

    // Select Server scope
    const scopeSelect = screen.getByLabelText(/Scope \/ Access Level/i);
    fireEvent.change(scopeSelect, { target: { value: 'server' } });

    // Enter server target name
    const targetInput = screen.getByPlaceholderText('e.g. ha, docker');
    fireEvent.change(targetInput, { target: { value: 'notes-rag' } });

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(createSpy).toHaveBeenCalledWith({
      name: 'Notes Assistant',
      keyType: 'personal',
      username: 'charlie',
      scopes: ['server:notes-rag'],
      expiresInDays: undefined
    });
  });

  /**
   * @requirement REQ-AUTH-PERSONAL-APPKEY-CREATE
   * @category AUTH
   * @type PositiveFeature
   * @description Handles scope serialization for category scope and expiration days.
   */
  it('handles scope serialization for category scope and expiration days', async () => {
    const createSpy = vi.fn().mockResolvedValue(undefined);
    useAppKeyStore.setState({ isCreateModalOpen: true, createdResult: null, createAppKey: createSpy });
    render(<AppKeyModal />);

    fireEvent.change(screen.getByPlaceholderText('e.g. My Laptop CLI'), { target: { value: 'Media Tools' } });

    // Select Category scope
    const scopeSelect = screen.getByLabelText(/Scope \/ Access Level/i);
    fireEvent.change(scopeSelect, { target: { value: 'category' } });

    const targetInput = screen.getByPlaceholderText('e.g. smarthome, media');
    fireEvent.change(targetInput, { target: { value: 'media' } });

    // Select 90 days expiration
    const expSelect = screen.getByLabelText(/Expiration/i);
    fireEvent.change(expSelect, { target: { value: '90' } });

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(createSpy).toHaveBeenCalledWith({
      name: 'Media Tools',
      keyType: 'personal',
      username: undefined,
      scopes: ['category:media'],
      expiresInDays: 90
    });
  });

  /**
   * @requirement REQ-AUTH-PERSONAL-APPKEY-CREATE
   * @category AUTH
   * @type FailClosedGuardrail
   * @description Disables submit button when quota limit is reached.
   */
  it('disables submit button when quota limit is reached', () => {
    useAppKeyStore.setState({
      isCreateModalOpen: true,
      createdResult: null,
      limits: {
        globalMax: 50,
        userMax: 10,
        totalActiveKeys: 50,
        userActiveKeys: 10,
        isLimitReached: true
      }
    });

    render(<AppKeyModal />);

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    expect(submitBtn).toBeDisabled();
  });

  /**
   * @requirement REQ-AUTH-PERSONAL-APPKEY-CREATE
   * @category AUTH
   * @type PositiveFeature
   * @description Displays one-time secret result and copies plaintext key to clipboard.
   */
  it('displays one-time secret result and copies plaintext key to clipboard', async () => {
    const closeSpy = vi.fn();
    useAppKeyStore.setState({
      isCreateModalOpen: true,
      createdResult: {
        id: 'key-99',
        name: 'CLI Agent',
        username: 'admin',
        keyPrefix: 'mcp_live_9999',
        plaintextKey: 'mcp_live_9999_secret_token_123456789',
        scopes: ['all'],
        createdAt: '2026-08-14T00:00:00Z'
      },
      closeModal: closeSpy
    });

    render(<AppKeyModal />);

    expect(screen.getByText('App Key Created!')).toBeInTheDocument();
    expect(screen.getByText('mcp_live_9999_secret_token_123456789')).toBeInTheDocument();
    expect(screen.getByText(/Ready-to-Use mcp_config\.json Snippet:/i)).toBeInTheDocument();

    // Copy to clipboard
    const copyBtn = document.querySelector('.fa-copy')?.closest('button');
    expect(copyBtn).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(copyBtn!);
    });

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('mcp_live_9999_secret_token_123456789');

    // Done button closes modal
    const doneBtn = screen.getByRole('button', { name: /done/i });
    fireEvent.click(doneBtn);
    expect(closeSpy).toHaveBeenCalled();
  });
});
