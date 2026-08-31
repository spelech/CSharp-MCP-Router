/** @requirement UI-107 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { PromptTesterCard } from '../../components/testbench/PromptTesterCard';

describe('PromptTesterCard Component', () => {
  const mockPrompts = [
    {
      name: 'router__summarize',
      description: 'Summarize text content',
      arguments: [
        { name: 'text', description: 'Text to summarize', required: true },
        { name: 'length', description: 'Target length', required: false },
      ],
    },
    {
      name: 'notes__search',
      description: 'Search personal notes',
      arguments: [{ name: 'query', description: 'Search query', required: true }],
    },
  ];

  /**
   * @requirement MCP-06
   * @category MCP
   * @type Positive
   * @description renders prompt dropdown and filters by selected server
   */
  it('renders prompt dropdown and filters by selected server', () => {
    const onServerChange = vi.fn();
    const onPromptChange = vi.fn();
    const onArgChange = vi.fn();
    const onSubmit = vi.fn();

    render(
      <PromptTesterCard
        prompts={mockPrompts}
        selectedServer="router"
        selectedPromptName="router__summarize"
        promptArguments={{ text: 'Sample text' }}
        onServerChange={onServerChange}
        onPromptChange={onPromptChange}
        onArgChange={onArgChange}
        onSubmit={onSubmit}
      />
    );

    expect(screen.getByText('Interactive Prompt Tester')).toBeInTheDocument();
    expect(screen.getByText('Text to summarize')).toBeInTheDocument();
  });

  /**
   * @requirement MCP-06
   * @category MCP
   * @type Positive
   * @description triggers arg change and form submit
   */
  it('triggers arg change and form submit', () => {
    const onServerChange = vi.fn();
    const onPromptChange = vi.fn();
    const onArgChange = vi.fn();
    const onSubmit = vi.fn((e) => e.preventDefault());

    const { container } = render(
      <PromptTesterCard
        prompts={mockPrompts}
        selectedServer="router"
        selectedPromptName="router__summarize"
        promptArguments={{ text: '' }}
        onServerChange={onServerChange}
        onPromptChange={onPromptChange}
        onArgChange={onArgChange}
        onSubmit={onSubmit}
      />
    );

    const inputs = container.querySelectorAll('.param-field input');
    expect(inputs.length).toBeGreaterThan(0);
    fireEvent.change(inputs[0], { target: { value: 'New text content' } });
    expect(onArgChange).toHaveBeenCalledWith('text', 'New text content');

    const form = container.querySelector('form')!;
    fireEvent.submit(form);
    expect(onSubmit).toHaveBeenCalled();
  });
});
