import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { ConfirmModal } from '../../components/shared/ConfirmModal';
import { useConfirmStore } from '../../stores/useConfirmStore';

/**
 * @requirement REQ-UI-CONFIRM-MODAL
 * @category UI
 * @type PositiveFeature
 * @description Renders confirmation dialog with title, message, and trigger buttons for confirm and cancel.
 */
describe('ConfirmModal', () => {
  beforeEach(() => {
    useConfirmStore.setState({
      isOpen: false,
      options: { message: '' },
      resolve: null
    });
  });

  it('renders nothing when closed', () => {
    const { container } = render(<ConfirmModal />);
    expect(container.firstChild).toBeNull();
  });

  it('renders title, message, and action buttons when open', () => {
    useConfirmStore.setState({
      isOpen: true,
      options: {
        title: 'Revoke App Key',
        message: 'Are you sure you want to revoke this key?',
        confirmText: 'Revoke',
        cancelText: 'Keep Key',
        danger: true
      },
      resolve: () => {}
    });

    render(<ConfirmModal />);
    expect(screen.getByText('Revoke App Key')).toBeInTheDocument();
    expect(screen.getByText('Are you sure you want to revoke this key?')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Revoke' })).toHaveClass('btn-danger');
    expect(screen.getByRole('button', { name: 'Keep Key' })).toBeInTheDocument();
  });

  it('calls handleConfirm when confirm button clicked', () => {
    let resolvedValue: boolean | null = null;
    useConfirmStore.setState({
      isOpen: true,
      options: {
        title: 'Delete Server',
        message: 'Delete server docker?',
        confirmText: 'Delete'
      },
      resolve: (val) => { resolvedValue = val; }
    });

    render(<ConfirmModal />);
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    expect(resolvedValue).toBe(true);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });

  it('calls handleCancel when cancel button clicked', () => {
    let resolvedValue: boolean | null = null;
    useConfirmStore.setState({
      isOpen: true,
      options: {
        title: 'Delete Server',
        message: 'Delete server docker?',
        confirmText: 'Delete',
        cancelText: 'Cancel'
      },
      resolve: (val) => { resolvedValue = val; }
    });

    render(<ConfirmModal />);
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(resolvedValue).toBe(false);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });
});
