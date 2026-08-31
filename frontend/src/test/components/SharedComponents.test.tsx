/** @requirement UI-117 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Modal } from '../../components/shared/Modal';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { PaginationToolbar } from '../../components/shared/PaginationToolbar';

describe('Shared Components Suite', () => {
  describe('Modal Component', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description returns null when isOpen is false
     */
    it('returns null when isOpen is false', () => {
      const { container } = render(
        <Modal isOpen={false} onClose={vi.fn()} title="Test Modal">
          <div>Modal Body</div>
        </Modal>
      );
      expect(container).toBeEmptyDOMElement();
    });

    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description renders title, children, and handles close button click
     */
    it('renders title, children, and handles close button click', () => {
      const onClose = vi.fn();
      render(
        <Modal isOpen={true} onClose={onClose} title="Test Modal" maxWidth="600px">
          <div>Modal Body Content</div>
        </Modal>
      );

      expect(screen.getByText('Test Modal')).toBeInTheDocument();
      expect(screen.getByText('Modal Body Content')).toBeInTheDocument();

      const closeBtn = screen.getByRole('button', { name: '×' });
      fireEvent.click(closeBtn);
      expect(onClose).toHaveBeenCalled();
    });
  });

  describe('StatusBadge Component', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description renders various statuses correctly with indicators
     */
    it('renders various statuses correctly with indicators', () => {
      const { rerender } = render(<StatusBadge status="connected" showIndicator={true} />);
      expect(screen.getByText(/connected/i)).toHaveClass('badge-success');

      rerender(<StatusBadge status="connecting" />);
      expect(screen.getByText(/connecting/i)).toHaveClass('badge-warning');

      rerender(<StatusBadge status="failed" title="Failed to connect" />);
      expect(screen.getByText(/failed/i)).toHaveClass('badge-danger');

      rerender(<StatusBadge status="disabled" label="Custom Disabled Label" />);
      expect(screen.getByText('Custom Disabled Label')).toHaveClass('badge-secondary');
    });
  });

  describe('PaginationToolbar Component', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description returns null when totalItems is 0
     */
    it('returns null when totalItems is 0', () => {
      const { container } = render(
        <PaginationToolbar
          currentPage={1}
          pageSize={10}
          totalItems={0}
          onPageChange={vi.fn()}
          onPageSizeChange={vi.fn()}
        />
      );
      expect(container).toBeEmptyDOMElement();
    });

    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description renders page info and navigation controls
     */
    it('renders page info and navigation controls', () => {
      const onPageChange = vi.fn();
      const onPageSizeChange = vi.fn();

      render(
        <PaginationToolbar
          currentPage={2}
          pageSize={12}
          totalItems={30}
          onPageChange={onPageChange}
          onPageSizeChange={onPageSizeChange}
        />
      );

      expect(screen.getByText(/Showing/i)).toBeInTheDocument();
      expect(screen.getByText('13-24')).toBeInTheDocument();
      expect(screen.getByText('30')).toBeInTheDocument();

      // Page size change
      const pageSizeSelect = screen.getByLabelText('Per Page:');
      fireEvent.change(pageSizeSelect, { target: { value: '24' } });
      expect(onPageSizeChange).toHaveBeenCalledWith(24);

      // Previous button click
      const buttons = screen.getAllByRole('button');
      fireEvent.click(buttons[0]);
      expect(onPageChange).toHaveBeenCalledWith(1);

      // Next button click
      fireEvent.click(buttons[1]);
      expect(onPageChange).toHaveBeenCalledWith(3);
    });

    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description handles pageSize all
     */
    it('handles pageSize all', () => {
      const onPageSizeChange = vi.fn();

      render(
        <PaginationToolbar
          currentPage={1}
          pageSize="all"
          totalItems={25}
          onPageChange={vi.fn()}
          onPageSizeChange={onPageSizeChange}
        />
      );

      expect(screen.getByText('1-25')).toBeInTheDocument();
    });
  });
});
