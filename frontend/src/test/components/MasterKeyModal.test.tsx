import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { MasterKeyModal } from '../../components/settings/MasterKeyModal';

describe('MasterKeyModal component', () => {
  /**
   * @requirement SEC-MASTERKEY-CUSTOM-MODAL-REENCRYPTION
   * @category SEC
   * @type PositiveFeature
   * @description Validates master key inputs (length, match) and triggers atomic re-encryption.
   */
  it('validates key inputs and submits custom master key to callback', async () => {
    const onSetMasterKey = vi.fn().mockResolvedValue({ success: true, message: 'Updated' });
    const onClose = vi.fn();

    render(
      <MasterKeyModal
        isOpen={true}
        onClose={onClose}
        onSetMasterKey={onSetMasterKey}
      />
    );

    expect(screen.getByText('Set Master Encryption Key')).toBeInTheDocument();
    expect(screen.getByText(/Atomic Database Re-Encryption/i)).toBeInTheDocument();

    const keyInput = screen.getByLabelText(/New Master Key/i);
    const confirmInput = screen.getByLabelText(/Confirm Master Key/i);
    const submitBtn = screen.getByRole('button', { name: /Set & Re-encrypt Secrets/i });

    // Submit button should initially be disabled
    expect(submitBtn).toBeDisabled();

    // Key too short (< 16 chars)
    fireEvent.change(keyInput, { target: { value: 'short-key' } });
    fireEvent.change(confirmInput, { target: { value: 'short-key' } });
    expect(submitBtn).toBeDisabled();

    // Valid key (>= 16 chars)
    const validKey = 'MySuperSecretMasterKey2026!#$';
    fireEvent.change(keyInput, { target: { value: validKey } });
    fireEvent.change(confirmInput, { target: { value: validKey } });

    expect(submitBtn).not.toBeDisabled();

    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(onSetMasterKey).toHaveBeenCalledWith(validKey);
    expect(onClose).toHaveBeenCalled();
  });

  /**
   * @requirement SEC-MASTERKEY-CUSTOM-MODAL-REENCRYPTION
   * @category SEC
   * @type PositiveFeature
   * @description Auto-generates a strong master key and populates both key and confirmation inputs.
   */
  it('generates a strong random master key when auto-generate button is clicked', async () => {
    const onSetMasterKey = vi.fn().mockResolvedValue({ success: true });
    const onClose = vi.fn();

    render(
      <MasterKeyModal
        isOpen={true}
        onClose={onClose}
        onSetMasterKey={onSetMasterKey}
      />
    );

    const autoGenBtn = screen.getByRole('button', { name: /Auto-Generate/i });
    fireEvent.click(autoGenBtn);

    const keyInput = screen.getByLabelText(/New Master Key/i) as HTMLInputElement;
    const confirmInput = screen.getByLabelText(/Confirm Master Key/i) as HTMLInputElement;

    expect(keyInput.value.length).toBe(32);
    expect(confirmInput.value).toBe(keyInput.value);

    const submitBtn = screen.getByRole('button', { name: /Set & Re-encrypt Secrets/i });
    expect(submitBtn).not.toBeDisabled();
  });

  /**
   * @requirement SEC-MASTERKEY-CUSTOM-MODAL-REENCRYPTION
   * @category SEC
   * @type PositiveFeature
   * @description Displays error message when re-encryption fails on backend.
   */
  it('displays validation error when onSetMasterKey returns failure', async () => {
    const onSetMasterKey = vi.fn().mockResolvedValue({ success: false, error: 'Database re-encryption failed.' });
    const onClose = vi.fn();

    render(
      <MasterKeyModal
        isOpen={true}
        onClose={onClose}
        onSetMasterKey={onSetMasterKey}
      />
    );

    const validKey = 'ValidSecretMasterKey123456!';
    fireEvent.change(screen.getByLabelText(/New Master Key/i), { target: { value: validKey } });
    fireEvent.change(screen.getByLabelText(/Confirm Master Key/i), { target: { value: validKey } });

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /Set & Re-encrypt Secrets/i }));
    });

    expect(screen.getByText('Database re-encryption failed.')).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
