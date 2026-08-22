/** @requirement REQ-UI-104 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ResourceTesterCard } from '../../components/testbench/ResourceTesterCard';

describe('ResourceTesterCard Component', () => {
  const mockResourcesData = {
    resources: [
      { uri: 'router://catalog', name: 'Catalog', mimeType: 'application/json', description: 'System service catalog' },
      { uri: 'mcp://notes/recent', name: 'Recent Notes', mimeType: 'text/markdown', description: 'Recent markdown notes' },
    ],
    templates: [
      { uriTemplate: 'mcp://notes/{id}', name: 'Note by ID', description: 'Fetch note content by ID' },
    ],
  };

  it('renders resource tester with servers and resources', () => {
    const onServerChange = vi.fn();
    const onSelectChange = vi.fn();
    const onUriChange = vi.fn();
    const onSubmit = vi.fn();

    render(
      <ResourceTesterCard
        resourcesData={mockResourcesData}
        selectedServer="router"
        selectedResourceUri="router://catalog"
        selectedResourceValue="router://catalog"
        onServerChange={onServerChange}
        onSelectChange={onSelectChange}
        onUriChange={onUriChange}
        onSubmit={onSubmit}
      />
    );

    expect(screen.getByText('Interactive Resource Tester')).toBeInTheDocument();
    expect(screen.getByDisplayValue('router://catalog')).toBeInTheDocument();
  });

  it('handles custom URI input and submit', () => {
    const onServerChange = vi.fn();
    const onSelectChange = vi.fn();
    const onUriChange = vi.fn();
    const onSubmit = vi.fn((e) => e.preventDefault());

    const { container } = render(
      <ResourceTesterCard
        resourcesData={mockResourcesData}
        selectedServer="router"
        selectedResourceUri="router://catalog"
        selectedResourceValue="router://catalog"
        onServerChange={onServerChange}
        onSelectChange={onSelectChange}
        onUriChange={onUriChange}
        onSubmit={onSubmit}
      />
    );

    const uriInput = screen.getByDisplayValue('router://catalog');
    fireEvent.change(uriInput, { target: { value: 'router://metrics' } });
    expect(onUriChange).toHaveBeenCalledWith('router://metrics');

    const form = container.querySelector('form')!;
    fireEvent.submit(form);
    expect(onSubmit).toHaveBeenCalled();
  });
});
