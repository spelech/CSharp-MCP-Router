import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { CustomFileModal } from '../../components/settings/CustomFileModal';
import { useSettingsStore } from '../../stores/useSettingsStore';
import { useToastStore } from '../../stores/useToastStore';

describe('CustomFileModal Component', () => {
  beforeEach(() => {
    useToastStore.setState({ toasts: [] });
    useSettingsStore.setState({
      isCustomFileOpen: true,
      editingFileMeta: null,
      editingFileContent: JSON.stringify({
        description: 'Test Prompt Description',
        arguments: [{ name: 'topic', description: 'Topic', required: true }],
        messages: [{ role: 'user', content: { text: 'Hello {{topic}}' } }],
      }),
      activeFileModalTab: 'builder',
      saveCustomFile: vi.fn().mockResolvedValue(true),
      closeCustomFileModal: vi.fn(),
      setActiveFileModalTab: (tab: 'editor' | 'builder') =>
        useSettingsStore.setState({ activeFileModalTab: tab }),
    });
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description renders CustomFileModal in create mode and displays visual builder tabs
   */
  it('renders CustomFileModal in create mode and displays visual builder tabs', () => {
    render(<CustomFileModal />);

    expect(screen.getByText('Create Custom File')).toBeInTheDocument();
    expect(screen.getByText('Raw JSON Editor')).toBeInTheDocument();
    expect(screen.getByText('Visual Prompt Builder')).toBeInTheDocument();
    expect(screen.getByText('Prompt Description')).toBeInTheDocument();
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description allows adding and removing arguments in visual builder
   */
  it('allows adding and removing arguments in visual builder', async () => {
    const { container } = render(<CustomFileModal />);

    const addVarBtn = screen.getByRole('button', { name: /add variable/i });
    await act(async () => {
      fireEvent.click(addVarBtn);
    });

    const varInputs = screen.getAllByPlaceholderText(/variable name/i);
    expect(varInputs.length).toBeGreaterThanOrEqual(2);

    const deleteBtns = container.querySelectorAll('.btn-icon');
    expect(deleteBtns.length).toBeGreaterThan(0);
    await act(async () => {
      fireEvent.click(deleteBtns[0]);
    });

    const remainingVarInputs = screen.getAllByPlaceholderText(/variable name/i);
    expect(remainingVarInputs.length).toBe(varInputs.length - 1);
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description allows adding and removing messages in visual builder
   */
  it('allows adding and removing messages in visual builder', async () => {
    const { container } = render(<CustomFileModal />);

    const addUserMsgBtn = screen.getByRole('button', { name: /user message/i });
    await act(async () => {
      fireEvent.click(addUserMsgBtn);
    });

    const msgAreas = screen.getAllByPlaceholderText(/enter user message/i);
    expect(msgAreas.length).toBeGreaterThanOrEqual(2);

    const deleteBtns = container.querySelectorAll('.btn-icon');
    expect(deleteBtns.length).toBeGreaterThan(0);
    await act(async () => {
      fireEvent.click(deleteBtns[deleteBtns.length - 1]);
    });

    const remainingMsgAreas = screen.getAllByPlaceholderText(/enter user message/i);
    expect(remainingMsgAreas.length).toBe(msgAreas.length - 1);
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description switches between Raw JSON Editor and Visual Prompt Builder with synchronization
   */
  it('switches between Raw JSON Editor and Visual Prompt Builder with synchronization', async () => {
    render(<CustomFileModal />);

    const rawTabBtn = screen.getByRole('button', { name: /raw json editor/i });
    await act(async () => {
      fireEvent.click(rawTabBtn);
    });

    expect(useSettingsStore.getState().activeFileModalTab).toBe('editor');
  });

  /**
   * @requirement UI-TOAST-TRANSITION
   * @category UI
   * @type FailClosedGuardrail
   * @description Displays error toast notification when switching from invalid JSON to Visual Prompt Builder.
   */
  it('shows error toast when switching from invalid JSON to Visual Prompt Builder', async () => {
    useSettingsStore.setState({ activeFileModalTab: 'editor' });
    render(<CustomFileModal />);

    const rawEditor = screen.getByLabelText('File Content');
    fireEvent.change(rawEditor, { target: { value: '{ invalid JSON content' } });

    const builderTabBtn = screen.getByRole('button', { name: /visual prompt builder/i });
    await act(async () => {
      fireEvent.click(builderTabBtn);
    });

    expect(useToastStore.getState().toasts.some((t) => t.message.includes('Cannot switch to Visual Builder') && t.type === 'error')).toBe(true);
  });

  /**
   * @requirement UI-TOAST-TRANSITION
   * @category UI
   * @type FailClosedGuardrail
   * @description Displays error toast notification when saving without a file name.
   */
  it('shows error toast when saving without a file name', async () => {
    render(<CustomFileModal />);

    const fileNameInput = screen.getByLabelText(/file name/i);
    fireEvent.change(fileNameInput, { target: { value: '   ' } });

    const saveBtn = screen.getByRole('button', { name: /save file/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(useToastStore.getState().toasts.some((t) => t.message.includes('Please enter a file name') && t.type === 'error')).toBe(true);
  });

  /**
   * @requirement UI-TOAST-TRANSITION
   * @category UI
   * @type FailClosedGuardrail
   * @description Displays error toast notification when saving prompt with invalid JSON content.
   */
  it('shows error toast when saving prompt with invalid JSON content', async () => {
    useSettingsStore.setState({ activeFileModalTab: 'editor' });
    render(<CustomFileModal />);

    const fileNameInput = screen.getByLabelText(/file name/i);
    fireEvent.change(fileNameInput, { target: { value: 'invalid.json' } });

    const rawEditor = screen.getByLabelText('File Content');
    fireEvent.change(rawEditor, { target: { value: '{ bad json' } });

    const saveBtn = screen.getByRole('button', { name: /save file/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(useToastStore.getState().toasts.some((t) => t.message.includes('Invalid JSON content') && t.type === 'error')).toBe(true);
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description changes file type to resources and adjusts extension
   */
  it('changes file type to resources and adjusts extension', async () => {
    render(<CustomFileModal />);

    const typeSelect = screen.getByLabelText(/file type/i);
    await act(async () => {
      fireEvent.change(typeSelect, { target: { value: 'resources' } });
    });

    expect(screen.getByPlaceholderText('e.g. guide.md')).toBeInTheDocument();
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description submits form and calls saveCustomFile
   */
  it('submits form and calls saveCustomFile', async () => {
    const saveSpy = vi.fn().mockResolvedValue(true);
    const closeSpy = vi.fn();
    useSettingsStore.setState({
      saveCustomFile: saveSpy,
      closeCustomFileModal: closeSpy,
    });

    render(<CustomFileModal />);

    const fileNameInput = screen.getByLabelText(/file name/i);
    fireEvent.change(fileNameInput, { target: { value: 'my-custom-prompt.json' } });

    const saveBtn = screen.getByRole('button', { name: /save file/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(saveSpy).toHaveBeenCalledWith('prompts', 'my-custom-prompt.json', expect.any(String));
    expect(closeSpy).toHaveBeenCalled();
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description renders in edit mode when editingFileMeta is set
   */
  it('renders in edit mode when editingFileMeta is set', () => {
    useSettingsStore.setState({
      editingFileMeta: {
        type: 'resources',
        name: 'test-doc.md',
        sizeBytes: 500,
        lastModified: '2026-08-14T00:00:00Z',
      },
      editingFileContent: '# Sample Documentation',
    });

    render(<CustomFileModal />);

    expect(screen.getByText('Edit test-doc.md')).toBeInTheDocument();
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type Positive
   * @description allows adding assistant messages, modifying argument required checkbox, and rendering empty arguments state
   */
  it('allows adding assistant messages, modifying argument required checkbox, and rendering empty arguments state', async () => {
    useSettingsStore.setState({
      isCustomFileOpen: true,
      editingFileMeta: {
        type: 'prompts',
        name: 'empty-prompt.json',
        sizeBytes: 50,
        lastModified: '2026-08-17T00:00:00Z',
      },
      editingFileContent: JSON.stringify({
        description: 'Empty Args Prompt',
        arguments: [],
        messages: [{ role: 'user', content: { text: 'Hello' } }],
      }),
      activeFileModalTab: 'builder',
    });

    render(<CustomFileModal />);

    expect(screen.getByText('No arguments defined.')).toBeInTheDocument();

    // Add variable and toggle required checkbox
    const addVarBtn = screen.getByRole('button', { name: /add variable/i });
    await act(async () => {
      fireEvent.click(addVarBtn);
    });
    const reqCheckbox = screen.getByRole('checkbox');
    fireEvent.click(reqCheckbox);

    // Add assistant message
    const addAssistantBtn = screen.getByRole('button', { name: /assistant message/i });
    await act(async () => {
      fireEvent.click(addAssistantBtn);
    });

    const assistantAreas = screen.getAllByPlaceholderText(/enter assistant message/i);
    expect(assistantAreas.length).toBe(1);

    // Test changing file type with extension swapping
    const typeSelect = screen.getByLabelText(/file type/i);
    const fileNameInput = screen.getByLabelText(/file name/i);
    fireEvent.change(fileNameInput, { target: { value: 'template.json' } });

    await act(async () => {
      fireEvent.change(typeSelect, { target: { value: 'resources' } });
    });
    expect(fileNameInput).toHaveValue('template.md');

    await act(async () => {
      fireEvent.change(typeSelect, { target: { value: 'prompts' } });
    });
    expect(fileNameInput).toHaveValue('template.json');
  });
});
