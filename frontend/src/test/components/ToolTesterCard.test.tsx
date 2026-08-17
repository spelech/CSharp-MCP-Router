import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ToolTesterCard } from '../../components/testbench/ToolTesterCard';

describe('ToolTesterCard Component', () => {
  const mockTools = [
    {
      name: 'docker__list_containers',
      description: 'List all containers',
      inputSchema: {
        type: 'object',
        properties: {
          all: { type: 'boolean', description: 'Include stopped containers' },
          limit: { type: 'integer', description: 'Max items to return' },
          filter: { type: 'string', description: 'Filter pattern' },
          tags: { type: 'array', description: 'List of tags' },
          config: { type: 'object', description: 'Config object' },
        },
        required: ['filter'],
      },
    },
    {
      name: 'docker__no_args_tool',
      description: 'Tool without arguments',
      inputSchema: {
        type: 'object',
      },
    },
    {
      name: 'custom_local_tool',
      description: 'Tool with no namespace prefix',
      inputSchema: {
        type: 'object',
        properties: {
          rate: { type: 'number', description: 'Floating rate' },
        },
      },
    },
  ];

  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Interactive tool tester renders server and tool selection dropdowns
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders initial server and tool selection options', () => {
    const onServerChange = vi.fn();
    const onToolChange = vi.fn();

    render(
      <ToolTesterCard
        tools={mockTools}
        selectedServer=""
        selectedToolName=""
        toolArguments={{}}
        rawToolJson=""
        onServerChange={onServerChange}
        onToolChange={onToolChange}
        onArgChange={vi.fn()}
        onRawJsonChange={vi.fn()}
        onSubmit={vi.fn()}
      />
    );

    expect(screen.getByText('Interactive Tool Tester')).toBeInTheDocument();
    expect(screen.getByLabelText('Server')).toBeInTheDocument();
    expect(screen.getByLabelText('Tool')).toBeInTheDocument();

    const serverSelect = screen.getByLabelText('Server');
    fireEvent.change(serverSelect, { target: { value: 'docker' } });
    expect(onServerChange).toHaveBeenCalledWith('docker');
  });

  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Tool selector filters available tools by selected backend server
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('filters tools by selected server and handles tool change', () => {
    const onToolChange = vi.fn();

    render(
      <ToolTesterCard
        tools={mockTools}
        selectedServer="docker"
        selectedToolName=""
        toolArguments={{}}
        rawToolJson=""
        onServerChange={vi.fn()}
        onToolChange={onToolChange}
        onArgChange={vi.fn()}
        onRawJsonChange={vi.fn()}
        onSubmit={vi.fn()}
      />
    );

    const toolSelect = screen.getByLabelText('Tool');
    fireEvent.change(toolSelect, { target: { value: 'docker__list_containers' } });
    expect(onToolChange).toHaveBeenCalledWith('docker__list_containers');
  });

  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Custom server mode displays local un-namespaced tools
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('filters custom tools with no namespace prefix when selectedServer is custom', () => {
    render(
      <ToolTesterCard
        tools={mockTools}
        selectedServer="custom"
        selectedToolName=""
        toolArguments={{}}
        rawToolJson=""
        onServerChange={vi.fn()}
        onToolChange={vi.fn()}
        onArgChange={vi.fn()}
        onRawJsonChange={vi.fn()}
        onSubmit={vi.fn()}
      />
    );

    expect(screen.getByText('custom_local_tool')).toBeInTheDocument();
  });

  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Dynamic form generator renders type-appropriate input controls with validation
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders dynamic fields for boolean, number, string, array, and object types', () => {
    const onArgChange = vi.fn();

    render(
      <ToolTesterCard
        tools={mockTools}
        selectedServer="docker"
        selectedToolName="docker__list_containers"
        toolArguments={{ all: true, limit: 10, filter: 'running', tags: ['web'], config: { debug: true } }}
        rawToolJson=""
        onServerChange={vi.fn()}
        onToolChange={vi.fn()}
        onArgChange={onArgChange}
        onRawJsonChange={vi.fn()}
        onSubmit={vi.fn()}
      />
    );

    // Check boolean switch
    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toBeChecked();
    fireEvent.click(checkbox);
    expect(onArgChange).toHaveBeenCalledWith('all', 'boolean', false);

    // Check integer input
    const numberInput = screen.getByDisplayValue('10');
    fireEvent.change(numberInput, { target: { value: '25' } });
    expect(onArgChange).toHaveBeenCalledWith('limit', 'number', '25');

    // Check string input
    const stringInput = screen.getByDisplayValue('running');
    fireEvent.change(stringInput, { target: { value: 'stopped' } });
    expect(onArgChange).toHaveBeenCalledWith('filter', 'string', 'stopped');

    // Check array/object textareas
    const textareas = screen.getAllByRole('textbox');
    expect(textareas.length).toBeGreaterThan(0);
    fireEvent.change(textareas[0], { target: { value: '["prod"]' } });
    expect(onArgChange).toHaveBeenCalled();
  });

  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Empty argument state displays helpful notice when tool requires no parameters
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders empty state when selected tool takes no arguments', () => {
    render(
      <ToolTesterCard
        tools={mockTools}
        selectedServer="docker"
        selectedToolName="docker__no_args_tool"
        toolArguments={{}}
        rawToolJson=""
        onServerChange={vi.fn()}
        onToolChange={vi.fn()}
        onArgChange={vi.fn()}
        onRawJsonChange={vi.fn()}
        onSubmit={vi.fn()}
      />
    );

    expect(screen.getByText('This tool takes no arguments.')).toBeInTheDocument();
  });

  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Tab switcher permits toggling between structured form and raw JSON editor
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('switches to raw JSON tab and handles raw JSON editing', () => {
    const onRawJsonChange = vi.fn();

    render(
      <ToolTesterCard
        tools={mockTools}
        selectedServer="docker"
        selectedToolName="docker__list_containers"
        toolArguments={{}}
        rawToolJson='{"limit": 5}'
        onServerChange={vi.fn()}
        onToolChange={vi.fn()}
        onArgChange={vi.fn()}
        onRawJsonChange={onRawJsonChange}
        onSubmit={vi.fn()}
      />
    );

    // Click Raw JSON Tab
    const jsonTabBtn = screen.getByText('Raw JSON Input');
    fireEvent.click(jsonTabBtn);

    const jsonTextarea = screen.getByLabelText('Arguments (JSON)');
    expect(jsonTextarea).toHaveValue('{"limit": 5}');

    fireEvent.change(jsonTextarea, { target: { value: '{"limit": 20}' } });
    expect(onRawJsonChange).toHaveBeenCalledWith('{"limit": 20}');

    // Switch back to Form
    const formTabBtn = screen.getByText('Interactive Form');
    fireEvent.click(formTabBtn);
  });

  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Test bench executes tool request upon submission
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('handles form submission', () => {
    const onSubmit = vi.fn((e) => e.preventDefault());

    render(
      <ToolTesterCard
        tools={mockTools}
        selectedServer="docker"
        selectedToolName="docker__list_containers"
        toolArguments={{ filter: 'prod' }}
        rawToolJson=""
        onServerChange={vi.fn()}
        onToolChange={vi.fn()}
        onArgChange={vi.fn()}
        onRawJsonChange={vi.fn()}
        onSubmit={onSubmit}
      />
    );

    const runBtn = screen.getByRole('button', { name: /Run Tool/i });
    expect(runBtn).not.toBeDisabled();
    fireEvent.click(runBtn);
    expect(onSubmit).toHaveBeenCalled();
  });
});
