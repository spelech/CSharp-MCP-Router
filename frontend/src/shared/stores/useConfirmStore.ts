import { create } from 'zustand';

export interface ConfirmOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  danger?: boolean;
}

export interface ConfirmState {
  isOpen: boolean;
  options: ConfirmOptions;
  resolve: ((value: boolean) => void) | null;
  confirm: (options: ConfirmOptions | string) => Promise<boolean>;
  handleConfirm: () => void;
  handleCancel: () => void;
}

export const useConfirmStore = create<ConfirmState>((set, get) => ({
  isOpen: false,
  options: { message: '' },
  resolve: null,
  confirm: (options: ConfirmOptions | string) => {
    const { resolve: prevResolve } = get();
    if (prevResolve) {
      prevResolve(false);
    }
    const opts: ConfirmOptions = typeof options === 'string' ? { message: options } : options;
    return new Promise<boolean>((resolve) => {
      set({
        isOpen: true,
        options: {
          title: opts.title || 'Confirm Action',
          message: opts.message,
          confirmText: opts.confirmText || 'Confirm',
          cancelText: opts.cancelText || 'Cancel',
          danger: opts.danger ?? false
        },
        resolve
      });
    });
  },
  handleConfirm: () => {
    const { resolve } = get();
    if (resolve) resolve(true);
    set({ isOpen: false, resolve: null });
  },
  handleCancel: () => {
    const { resolve } = get();
    if (resolve) resolve(false);
    set({ isOpen: false, resolve: null });
  }
}));

export function confirmAction(options: ConfirmOptions | string): Promise<boolean> {
  return useConfirmStore.getState().confirm(options);
}
