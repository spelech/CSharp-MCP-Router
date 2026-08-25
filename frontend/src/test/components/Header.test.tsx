/** @requirement UI-110 */

import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { Header } from '../../components/shared/Header';
import { mockApiResponse } from '../setup';

describe('Header component', () => {
  beforeEach(() => {
    mockApiResponse('/health', { version: '4.5.6', status: 'healthy' });
  });

  it('renders title, MCG badge, subtitle, and version badge', async () => {
    mockApiResponse('/api/me', {
      authenticated: true,
      username: 'admin',
      groups: ['full_admin']
    });

    await act(async () => {
      render(<Header />);
    });

    expect(screen.getByText('Model Context Gateway')).toBeInTheDocument();
    expect(screen.getByText('MCG')).toBeInTheDocument();
    expect(screen.getByText(/High-Performance MCP Aggregator & Semantic Gateway/i)).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText('v4.5.6')).toBeInTheDocument();
    });
  });

  it('renders admin badge and shield icon for full_admin users', async () => {
    mockApiResponse('/api/me', {
      authenticated: true,
      name: 'Admin User',
      username: 'admin',
      groups: ['full_admin', 'engineering']
    });

    await act(async () => {
      render(<Header />);
    });

    await waitFor(() => {
      expect(screen.getByText(/Admin User \(Admin\)/i)).toBeInTheDocument();
    });

    const userStatus = document.getElementById('user-status-item');
    expect(userStatus?.querySelector('.fa-user-shield')).toBeInTheDocument();
  });

  it('renders standard user badge for non-admin users', async () => {
    mockApiResponse('/api/me', {
      authenticated: true,
      username: 'guest_user',
      groups: ['house_member']
    });

    await act(async () => {
      render(<Header />);
    });

    await waitFor(() => {
      expect(screen.getByText(/guest_user \(User\)/i)).toBeInTheDocument();
    });

    const userStatus = document.getElementById('user-status-item');
    expect(userStatus?.querySelector('.fa-user')).toBeInTheDocument();
    expect(userStatus?.querySelector('.fa-user-shield')).toBeNull();
  });

  it('does not render user status item when unauthenticated', async () => {
    mockApiResponse('/api/me', {
      authenticated: false
    });

    await act(async () => {
      render(<Header />);
    });

    await waitFor(() => {
      expect(document.getElementById('user-status-item')).toBeNull();
    });
  });

  it('displays gateway status and SSE endpoint', async () => {
    await act(async () => {
      render(<Header />);
    });

    expect(screen.getByText('API Gateway Status')).toBeInTheDocument();
    expect(screen.getByText('Online')).toBeInTheDocument();
    expect(screen.getByText(`${window.location.origin}/sse`)).toBeInTheDocument();
  });

  it('toggles light and dark theme on button click and updates document attribute', async () => {
    await act(async () => {
      render(<Header />);
    });

    const toggleBtn = document.getElementById('theme-toggle');
    expect(toggleBtn).toBeInTheDocument();

    // Default theme is dark
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');

    // Click to switch to light
    await act(async () => {
      fireEvent.click(toggleBtn!);
    });

    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(localStorage.getItem('mcp-theme')).toBe('light');

    // Click again to switch back to dark
    await act(async () => {
      fireEvent.click(toggleBtn!);
    });

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('mcp-theme')).toBe('dark');
  });
});
