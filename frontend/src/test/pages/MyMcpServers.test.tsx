import React from 'react';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MyMcpServers } from '../../pages/MyMcpServers';
import * as serverApi from '../../api/serverApi';
import * as userCredentialsApi from '../../api/userCredentialsApi';
import { useToastStore } from '../../stores/useToastStore';

vi.mock('../../api/serverApi', () => ({
  fetchServersApi: vi.fn(),
}));

vi.mock('../../api/userCredentialsApi', () => ({
  fetchUserCredentialsApi: vi.fn(),
  saveUserCredentialApi: vi.fn(),
}));

describe('MyMcpServers Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useToastStore.setState({ toasts: [] });
  });

  /**
   * @requirement REQ-UI-TOAST-TRANSITION
   * @category UI
   * @type FailClosedGuardrail
   * @description Displays error toast notification when saving invalid JSON credentials for user-provided server.
   */
  it('shows error toast when saving invalid JSON credentials', async () => {
    vi.spyOn(serverApi, 'fetchServersApi').mockResolvedValue([
      {
        id: 'user-srv-1',
        displayName: 'My Custom Service',
        secretProvider: 'UserProvided',
        transportType: 'sse',
        targetUrl: 'http://localhost:3000/sse',
      } as any,
    ]);
    vi.spyOn(userCredentialsApi, 'fetchUserCredentialsApi').mockResolvedValue([]);

    await act(async () => {
      render(<MyMcpServers />);
    });

    expect(screen.getByText('My Custom Service')).toBeInTheDocument();
    expect(screen.getByText('Auth Missing')).toBeInTheDocument();

    const editBtn = screen.getByRole('button', { name: /edit auth/i });
    fireEvent.click(editBtn);

    expect(screen.getByText(/Edit Auth for My Custom Service/i)).toBeInTheDocument();

    const textarea = screen.getByRole('textbox');
    fireEvent.change(textarea, { target: { value: '{ invalid JSON' } });

    const saveBtn = screen.getByRole('button', { name: 'Save' });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(useToastStore.getState().toasts.some((t) => t.message.includes('Invalid JSON or failed to save') && t.type === 'error')).toBe(true);
  });

  /**
   * @requirement REQ-UI-TOAST-TRANSITION
   * @category UI
   * @type PositiveFeature
   * @description Successfully saves valid credentials and updates UI state.
   */
  it('saves valid credentials successfully and closes modal', async () => {
    vi.spyOn(serverApi, 'fetchServersApi').mockResolvedValue([
      {
        id: 'user-srv-1',
        displayName: 'My Custom Service',
        secretProvider: 'UserProvided',
        transportType: 'sse',
        targetUrl: 'http://localhost:3000/sse',
      } as any,
    ]);
    vi.spyOn(userCredentialsApi, 'fetchUserCredentialsApi').mockResolvedValue([]);
    const saveSpy = vi.spyOn(userCredentialsApi, 'saveUserCredentialApi').mockResolvedValue({ success: true } as any);

    await act(async () => {
      render(<MyMcpServers />);
    });

    const editBtn = screen.getByRole('button', { name: /edit auth/i });
    fireEvent.click(editBtn);

    const textarea = screen.getByRole('textbox');
    fireEvent.change(textarea, { target: { value: '{"apiKey":"secret123"}' } });

    const saveBtn = screen.getByRole('button', { name: 'Save' });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(saveSpy).toHaveBeenCalledWith('user-srv-1', '{"apiKey":"secret123"}');
    expect(screen.queryByText(/Edit Auth for My Custom Service/i)).not.toBeInTheDocument();
  });
});
