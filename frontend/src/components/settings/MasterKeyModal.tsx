import React, { useState } from 'react';
import { Modal } from '../shared/Modal';

export interface MasterKeyModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSetMasterKey: (newKey: string) => Promise<{ success: boolean; message?: string; error?: string }>;
}

export const MasterKeyModal: React.FC<MasterKeyModalProps> = ({
  isOpen,
  onClose,
  onSetMasterKey,
}) => {
  const [newKey, setNewKey] = useState('');
  const [confirmKey, setConfirmKey] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleGenerateKey = () => {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+';
    const array = new Uint8Array(32);
    window.crypto.getRandomValues(array);
    const generated = Array.from(array, (byte) => chars[byte % chars.length]).join('');
    setNewKey(generated);
    setConfirmKey(generated);
    setShowPassword(true);
    setValidationError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError(null);

    const trimmed = newKey.trim();
    if (trimmed.length < 16) {
      setValidationError('Master encryption key must be at least 16 characters long.');
      return;
    }

    if (trimmed !== confirmKey.trim()) {
      setValidationError('Master encryption keys do not match.');
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await onSetMasterKey(trimmed);
      if (result.success) {
        setNewKey('');
        setConfirmKey('');
        onClose();
      } else {
        setValidationError(result.error || result.message || 'Failed to update master encryption key.');
      }
    } catch (err: any) {
      setValidationError(err.message || 'Failed to update master encryption key.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      id="master-key-modal"
      isOpen={isOpen}
      onClose={onClose}
      title={
        <span>
          <i className="fa-solid fa-key" style={{ color: 'var(--primary)', marginRight: '8px' }}></i>
          Set Master Encryption Key
        </span>
      }
      maxWidth="560px"
    >
      <form onSubmit={handleSubmit}>
        <div
          style={{
            padding: '12px 16px',
            marginBottom: '16px',
            background: 'rgba(234, 179, 8, 0.08)',
            border: '1px solid rgba(234, 179, 8, 0.25)',
            borderRadius: '8px',
            fontSize: '12px',
            lineHeight: '1.5',
            color: '#fef08a',
          }}
        >
          <div style={{ display: 'flex', gap: '8px', alignItems: 'flex-start' }}>
            <i className="fa-solid fa-triangle-exclamation" style={{ color: '#eab308', marginTop: '2px', fontSize: '14px' }}></i>
            <div>
              <strong style={{ color: '#fef08a' }}>Atomic Database Re-Encryption &amp; Disaster Recovery</strong>
              <p style={{ margin: '4px 0 0 0', color: '#fef9c3', opacity: 0.9 }}>
                Setting a permanent master key replaces the auto-generated key and re-encrypts all database secrets atomically.
                Please save this key in your password manager or secure vault to ensure recovery across container or volume re-provisioning.
              </p>
            </div>
          </div>
        </div>

        {validationError && (
          <div
            className="alert alert-danger"
            style={{
              padding: '8px 12px',
              marginBottom: '14px',
              fontSize: '12px',
              borderRadius: '6px',
              background: 'rgba(239, 68, 68, 0.15)',
              border: '1px solid rgba(239, 68, 68, 0.3)',
              color: '#fca5a5',
            }}
          >
            <i className="fa-solid fa-circle-exclamation" style={{ marginRight: '6px' }}></i>
            {validationError}
          </div>
        )}

        <div className="form-group" style={{ marginBottom: '14px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
            <label htmlFor="modal-master-key" style={{ margin: 0 }}>
              New Master Key (min 16 chars)
            </label>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={handleGenerateKey}
              style={{ fontSize: '11px', padding: '2px 8px' }}
            >
              <i className="fa-solid fa-wand-magic-sparkles"></i> Auto-Generate
            </button>
          </div>
          <div style={{ position: 'relative' }}>
            <input
              type={showPassword ? 'text' : 'password'}
              id="modal-master-key"
              placeholder="Enter strong master key..."
              value={newKey}
              onChange={(e) => {
                setNewKey(e.target.value);
                setValidationError(null);
              }}
              required
              minLength={16}
              style={{
                width: '100%',
                paddingRight: '36px',
                fontFamily: showPassword ? 'JetBrains Mono, monospace' : 'inherit',
              }}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? 'Hide master key' : 'Show master key'}
              style={{
                position: 'absolute',
                right: '10px',
                top: '50%',
                transform: 'translateY(-50%)',
                background: 'none',
                border: 'none',
                color: 'var(--text-muted)',
                cursor: 'pointer',
                fontSize: '14px',
              }}
            >
              <i className={showPassword ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye'}></i>
            </button>
          </div>
        </div>

        <div className="form-group" style={{ marginBottom: '16px' }}>
          <label htmlFor="modal-confirm-master-key">Confirm Master Key</label>
          <input
            type={showPassword ? 'text' : 'password'}
            id="modal-confirm-master-key"
            placeholder="Confirm new master key..."
            value={confirmKey}
            onChange={(e) => {
              setConfirmKey(e.target.value);
              setValidationError(null);
            }}
            required
            minLength={16}
            style={{
              width: '100%',
              fontFamily: showPassword ? 'JetBrains Mono, monospace' : 'inherit',
            }}
          />
        </div>

        <div className="modal-footer" style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '20px' }}>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={onClose}
            disabled={isSubmitting}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting || !newKey.trim() || newKey.length < 16 || newKey !== confirmKey}
          >
            {isSubmitting ? (
              <>
                <i className="fa-solid fa-spinner fa-spin"></i> Re-encrypting...
              </>
            ) : (
              <>
                <i className="fa-solid fa-lock"></i> Set &amp; Re-encrypt Secrets
              </>
            )}
          </button>
        </div>
      </form>
    </Modal>
  );
};
