import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Header } from '../components/Header';

describe('Header component', () => {
  it('renders title and version badge', () => {
    render(<Header />);
    expect(screen.getByText('MCP Router Gateway')).toBeInTheDocument();
  });
});
