import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { LogsTerminalCard } from '../../components/testbench/LogsTerminalCard';
import { useLogStore } from '../../stores/useLogStore';

describe('LogsTerminalCard Component', () => {
  const mockLogs = [
    {
      id: '1',
      timestamp: '2026-08-14T20:00:00Z',
      level: 2, // INFO
      category: 'McpRouter.Core.Routing',
      message: 'Gateway session started',
    },
    {
      id: '2',
      timestamp: '2026-08-14T20:01:00Z',
      level: 4, // ERROR
      category: 'McpRouter.Infrastructure.Secrets',
      message: 'Failed to reach secret provider',
      exception: 'HttpRequestException: Connection refused',
    },
    {
      id: '3',
      timestamp: '2026-08-14T20:02:00Z',
      level: 2,
      category: 'McpRouter.Core.Transport',
      message: '[JSON-RPC Client -> Gateway] {"jsonrpc":"2.0","method":"tools/list","id":1}',
    },
  ];

  it('renders system logs and handles level filter', () => {
    useLogStore.setState({
      logs: mockLogs,
      typeFilter: 'system',
      levelFilter: 'ALL',
      autoScroll: true,
    });

    render(<LogsTerminalCard />);

    expect(screen.getByRole('heading', { name: /System Logs/i })).toBeInTheDocument();
    expect(screen.getByText('Gateway session started')).toBeInTheDocument();
    expect(screen.getByText('Failed to reach secret provider')).toBeInTheDocument();
    expect(screen.getByText('HttpRequestException: Connection refused')).toBeInTheDocument();

    // Filter by ERROR
    const levelSelect = screen.getByDisplayValue('All Levels');
    fireEvent.change(levelSelect, { target: { value: 'ERROR' } });
    expect(useLogStore.getState().levelFilter).toBe('ERROR');
  });

  it('renders RPC message stream with formatted JSON', () => {
    useLogStore.setState({
      logs: mockLogs,
      typeFilter: 'rpc',
      levelFilter: 'ALL',
      autoScroll: true,
    });

    render(<LogsTerminalCard />);

    expect(screen.getByText('Client -> Gateway')).toBeInTheDocument();
    expect(screen.getByText(/"method": "tools\/list"/i)).toBeInTheDocument();
  });

  it('toggles autoscroll and handles clear logs', () => {
    const clearSpy = vi.spyOn(useLogStore.getState(), 'clearLogs');
    useLogStore.setState({
      logs: mockLogs,
      typeFilter: 'system',
      levelFilter: 'ALL',
      autoScroll: true,
    });

    render(<LogsTerminalCard />);

    const autoScrollCheckbox = screen.getByRole('checkbox');
    expect(autoScrollCheckbox).toBeChecked();
    fireEvent.click(autoScrollCheckbox);
    expect(useLogStore.getState().autoScroll).toBe(false);

    const clearBtn = screen.getByRole('button', { name: /Clear Logs/i });
    fireEvent.click(clearBtn);
    expect(clearSpy).toHaveBeenCalled();
  });

  it('shows empty state when no logs match filter', () => {
    useLogStore.setState({
      logs: [],
      typeFilter: 'system',
      levelFilter: 'ALL',
    });

    render(<LogsTerminalCard />);
    expect(screen.getByText('No log entries matching filter.')).toBeInTheDocument();
  });
});
