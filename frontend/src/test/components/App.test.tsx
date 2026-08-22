import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import App from '../../App';
import { useUserStore } from '../../stores/useUserStore';
import { mockApiResponse, defaultMockData } from '../setup';

describe('App component', () => {
  beforeEach(() => {
    mockApiResponse('/api/me', defaultMockData.me);
    useUserStore.setState({
      user: defaultMockData.me
    });
  });

  /**
   * @requirement AUTH-SYSTEM-APPKEY-SEPARATION
   * @category AUTH
   * @type PositiveFeature
   * @description Renders header, navigation tabs including Settings, and default overview dashboard for admin.
   */
  it('renders header, navigation tabs, and default overview dashboard for admin user', async () => {
    await act(async () => {
      render(<App />);
    });

    expect(screen.getByText('MCP Gateway')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /overview/i })).toHaveClass('active');
    expect(screen.getByRole('button', { name: /app keys & security/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /settings/i })).toBeInTheDocument();

    // Footer is rendered
    expect(screen.getByText(/Protected by OIDC \/ Reverse Proxy Auth/i)).toBeInTheDocument();
  });

  /**
   * @requirement AUTH-SYSTEM-APPKEY-SEPARATION
   * @category AUTH
   * @type PositiveFeature
   * @description Switches between tabs on navigation click including Settings for admin.
   */
  it('switches between tabs on navigation click', async () => {
    await act(async () => {
      render(<App />);
    });

    const secTab = screen.getByRole('button', { name: /app keys & security/i });
    const benchTab = screen.getByRole('button', { name: /test bench/i });
    const setTab = screen.getByRole('button', { name: /settings/i });
    const overTab = screen.getByRole('button', { name: /overview/i });

    // Switch to App Keys & Security
    await act(async () => {
      fireEvent.click(secTab);
    });
    expect(secTab).toHaveClass('active');
    expect(overTab).not.toHaveClass('active');
    // Admin security view includes Registered Clients Card
    expect(screen.getByText(/Dynamic Client Registration \(RFC 7591\)/i)).toBeInTheDocument();

    // Switch to Test Bench
    await act(async () => {
      fireEvent.click(benchTab);
    });
    expect(benchTab).toHaveClass('active');

    // Switch to Settings
    await act(async () => {
      fireEvent.click(setTab);
    });
    expect(setTab).toHaveClass('active');

    // Switch back to Overview
    await act(async () => {
      fireEvent.click(overTab);
    });
    expect(overTab).toHaveClass('active');
  });

  /**
   * @requirement AUTH-PERSONAL-APPKEY-LIST
   * @category AUTH
   * @type PositiveFeature
   * @description Adapts navigation for non-admin users: shows My App Keys, hides Settings tab, and hides RegisteredClientsCard.
   */
  it('renders role-adaptive UI for non-admin user', async () => {
    const nonAdminUser = {
      authenticated: true,
      username: 'alice',
      name: 'Alice Cooper',
      email: 'alice@example.com',
      groups: ['developer']
    };
    mockApiResponse('/api/me', nonAdminUser);
    useUserStore.setState({ user: nonAdminUser });

    await act(async () => {
      render(<App />);
    });

    // Non-admin sees 'My App Keys' instead of 'App Keys & Security'
    const myKeysTab = screen.getByRole('button', { name: /my app keys/i });
    expect(myKeysTab).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /app keys & security/i })).toBeNull();

    // Non-admin does NOT see Settings tab
    expect(screen.queryByRole('button', { name: /settings/i })).toBeNull();

    // Click My App Keys tab
    await act(async () => {
      fireEvent.click(myKeysTab);
    });
    expect(myKeysTab).toHaveClass('active');

    // Security view for non-admin renders AppKeysCard and ClientSetupGuide, but NOT RegisteredClientsCard
    expect(screen.getByRole('heading', { name: /my app keys/i })).toBeInTheDocument();
    expect(screen.getByText(/Client Connection Guide/i)).toBeInTheDocument();
    expect(screen.queryByText(/Dynamic Client Registration \(RFC 7591\)/i)).toBeNull();
  });
});
