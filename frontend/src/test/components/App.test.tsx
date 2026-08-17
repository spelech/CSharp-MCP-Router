import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import App from '../../App';

describe('App component', () => {
  it('renders header, navigation tabs, and default overview dashboard', async () => {
    await act(async () => {
      render(<App />);
    });

    expect(screen.getByText('MCP Router Gateway')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /overview/i })).toHaveClass('active');

    // Footer is rendered
    expect(screen.getByText(/Protected by OIDC \/ Reverse Proxy Auth/i)).toBeInTheDocument();
  });

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
});
