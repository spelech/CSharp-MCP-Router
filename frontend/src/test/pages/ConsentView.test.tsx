import React from 'react';
import { render, screen } from '@testing-library/react';
import { ConsentView } from '../../pages/ConsentView';
import { describe, it, expect, beforeEach } from 'vitest';

describe('ConsentView Component', () => {
  beforeEach(() => {
    Object.defineProperty(window, 'location', {
      value: {
        search: '?client_id=123&client_name=Slack%20App',
        pathname: '/consent'
      },
      writable: true
    });
  });

  /**
   * @requirement AUTH-109
   * @category AUTH
   * @type PositiveFeature
   * @description ConsentView properly renders the client name from query string and builds correct form action.
   */
  it('renders client name from query string and sets form action', () => {
    const { container } = render(<ConsentView />);
    
    expect(screen.getByText(/Authorize Access/i)).toBeInTheDocument();
    expect(screen.getByText(/Slack App/i)).toBeInTheDocument();
    
    const form = container.querySelector('form');
    expect(form?.getAttribute('action')).toBe('/connect/authorize?client_id=123&client_name=Slack%20App');
  });
});
