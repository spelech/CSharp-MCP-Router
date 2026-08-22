import { describe, it, expect, beforeEach } from 'vitest';
import { useConfirmStore, confirmAction } from '../../stores/useConfirmStore';

/**
 * @requirement UI-CONFIRM-MODAL
 * @category UI
 * @type PositiveFeature
 * @description Centralized promise-based confirmation store resolves true on confirmation and false on cancellation.
 */
describe('useConfirmStore', () => {
  beforeEach(() => {
    useConfirmStore.setState({
      isOpen: false,
      options: { message: '' },
      resolve: null
    });
  });

  it('initializes in closed state', () => {
    const state = useConfirmStore.getState();
    expect(state.isOpen).toBe(false);
    expect(state.resolve).toBeNull();
  });

  it('opens confirmation modal and resolves true when confirmed', async () => {
    const confirmPromise = confirmAction({
      title: 'Delete Item',
      message: 'Are you sure?',
      confirmText: 'Delete',
      danger: true
    });

    const state = useConfirmStore.getState();
    expect(state.isOpen).toBe(true);
    expect(state.options.title).toBe('Delete Item');
    expect(state.options.message).toBe('Are you sure?');
    expect(state.options.confirmText).toBe('Delete');
    expect(state.options.danger).toBe(true);

    state.handleConfirm();
    const result = await confirmPromise;
    expect(result).toBe(true);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });

  it('resolves false when cancelled', async () => {
    const confirmPromise = confirmAction('Delete this file?');
    const state = useConfirmStore.getState();
    expect(state.isOpen).toBe(true);
    expect(state.options.message).toBe('Delete this file?');

    state.handleCancel();
    const result = await confirmPromise;
    expect(result).toBe(false);
    expect(useConfirmStore.getState().isOpen).toBe(false);
  });

  it('settles existing pending promise with false when a new confirmation is opened', async () => {
    const firstPromise = confirmAction('First confirmation');
    const secondPromise = confirmAction('Second confirmation');

    const firstResult = await firstPromise;
    expect(firstResult).toBe(false);

    useConfirmStore.getState().handleConfirm();
    const secondResult = await secondPromise;
    expect(secondResult).toBe(true);
  });
});
