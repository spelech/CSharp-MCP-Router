import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { TestBenchView } from '../../components/testbench/TestBenchView';
import * as testbenchApi from '../../api/testbenchApi';
import * as api from '../../shared/api/api';

describe('TestBenchView Component', () => {
  beforeEach(() => {
    vi.spyOn(testbenchApi, 'fetchTestToolsApi').mockResolvedValue([
      {
        name: 'docker__list_containers',
        description: 'List containers',
        inputSchema: { type: 'object', properties: { all: { type: 'boolean' } } },
      },
    ]);
    vi.spyOn(testbenchApi, 'fetchTestPromptsApi').mockResolvedValue([
      {
        name: 'docker__diagnose',
        description: 'Diagnose container',
        arguments: [{ name: 'container_id', required: true }],
      },
    ]);
    vi.spyOn(testbenchApi, 'fetchTestResourcesApi').mockResolvedValue({
      resources: [
        {
          name: 'Docker Status',
          uri: 'mcp://docker/status',
          description: 'Docker engine status',
        },
      ],
      templates: [
        {
          name: 'Container Log Template',
          uriTemplate: 'mcp://docker/logs/{id}',
        },
      ],
    });
  });

  it('renders test bench cards and switches tabs', async () => {
    render(<TestBenchView />);

    await waitFor(() => {
      expect(screen.getByText('Interactive Tool Tester')).toBeInTheDocument();
    });

    // Switch to Prompts tab
    const promptTab = screen.getByRole('button', { name: /Prompts/i });
    fireEvent.click(promptTab);
    expect(screen.getByText('Interactive Prompt Tester')).toBeInTheDocument();

    // Switch to Resources tab
    const resourceTab = screen.getByRole('button', { name: /Resources/i });
    fireEvent.click(resourceTab);
    expect(screen.getByText('Interactive Resource Tester')).toBeInTheDocument();

    // Switch back to Tools tab
    const toolTab = screen.getByRole('button', { name: /Tools/i });
    fireEvent.click(toolTab);
    expect(screen.getByText('Interactive Tool Tester')).toBeInTheDocument();
  });

  it('handles semantic search queries in SemanticRouterCard', async () => {
    vi.spyOn(api, 'apiRequest').mockResolvedValue([
      { name: 'docker__list_containers', score: 0.95, description: 'List containers' },
    ]);

    render(<TestBenchView />);

    await waitFor(() => {
      expect(screen.getByPlaceholderText(/e\.g\. search matrix in plex/i)).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/e\.g\. search matrix in plex/i);
    fireEvent.change(searchInput, { target: { value: 'list all docker containers' } });

    const searchBtn = screen.getByRole('button', { name: /Test Filter Score/i });
    fireEvent.click(searchBtn);

    await waitFor(() => {
      expect(api.apiRequest).toHaveBeenCalledWith('/api/test/semantic-search', expect.anything());
    });
  });

  it('executes tool and updates console', async () => {
    vi.spyOn(api, 'apiRequest').mockResolvedValue({
      content: [{ type: 'text', text: '{"status":"ok"}' }],
    });

    render(<TestBenchView />);

    await waitFor(() => {
      expect(screen.getByLabelText('Server')).toBeInTheDocument();
    });

    const serverSelect = screen.getByLabelText('Server');
    fireEvent.change(serverSelect, { target: { value: 'docker' } });

    const toolSelect = screen.getByLabelText('Tool');
    fireEvent.change(toolSelect, { target: { value: 'docker__list_containers' } });

    const forms = document.querySelectorAll('form');
    fireEvent.submit(forms[0]);

    await waitFor(() => {
      expect(api.apiRequest).toHaveBeenCalledWith('/api/test/call-tool', expect.anything());
    });
  });

  it('executes prompt get in prompt tester tab', async () => {
    vi.spyOn(api, 'apiRequest').mockResolvedValue({
      messages: [{ role: 'user', content: { type: 'text', text: 'Diagnose stopped container' } }],
    });

    render(<TestBenchView />);

    const promptTab = screen.getByRole('button', { name: /Prompts/i });
    fireEvent.click(promptTab);

    await waitFor(() => {
      expect(screen.getByLabelText('Server')).toBeInTheDocument();
    });

    const serverSelect = screen.getByLabelText('Server');
    fireEvent.change(serverSelect, { target: { value: 'docker' } });

    const promptSelect = screen.getByLabelText('Prompt');
    fireEvent.change(promptSelect, { target: { value: 'docker__diagnose' } });

    const forms = document.querySelectorAll('form');
    fireEvent.submit(forms[0]);

    await waitFor(() => {
      expect(api.apiRequest).toHaveBeenCalledWith('/api/test/get-prompt', expect.anything());
    });
  });

  it('executes resource read in resource inspector tab', async () => {
    vi.spyOn(api, 'apiRequest').mockResolvedValue({
      contents: [{ uri: 'mcp://docker/status', text: 'running' }],
    });

    render(<TestBenchView />);

    const resourceTab = screen.getByRole('button', { name: /Resources/i });
    fireEvent.click(resourceTab);

    await waitFor(() => {
      expect(screen.getByLabelText('Server')).toBeInTheDocument();
    });

    const serverSelect = screen.getByLabelText('Server');
    fireEvent.change(serverSelect, { target: { value: 'docker' } });

    const uriInput = screen.getByLabelText('Resource URI');
    fireEvent.change(uriInput, { target: { value: 'mcp://docker/status' } });

    const forms = document.querySelectorAll('form');
    fireEvent.submit(forms[0]);

    await waitFor(() => {
      expect(api.apiRequest).toHaveBeenCalledWith('/api/test/read-resource', expect.anything());
    });
  });
});
