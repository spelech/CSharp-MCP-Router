import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ClientSetupGuide } from '../../components/clients/ClientSetupGuide';

describe('ClientSetupGuide Component', () => {
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders Cursor setup by default and switches between clients', () => {
    render(<ClientSetupGuide />);

    expect(screen.getByText('Client Connection Guide')).toBeInTheDocument();
    expect(screen.getByText(/In Cursor IDE Settings/i)).toBeInTheDocument();

    // Switch to Claude Desktop
    const claudeBtn = screen.getByRole('button', { name: /Claude Desktop/i });
    fireEvent.click(claudeBtn);
    expect(screen.getByText(/claude_desktop_config\.json/i)).toBeInTheDocument();

    // Switch to Cline / Roo
    const clineBtn = screen.getByRole('button', { name: /Cline \/ Roo/i });
    fireEvent.click(clineBtn);
    expect(screen.getByText(/cline_mcp_settings\.json/i)).toBeInTheDocument();

    // Switch to Generic SSE
    const genericBtn = screen.getByRole('button', { name: /Generic SSE/i });
    fireEvent.click(genericBtn);
    expect(screen.getByText(/Direct connection via SSE transport/i)).toBeInTheDocument();
  });
});
