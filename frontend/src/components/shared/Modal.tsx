import React from 'react';

interface ModalProps {
  id?: string;
  isOpen: boolean;
  onClose: () => void;
  title: React.ReactNode;
  children: React.ReactNode;
  maxWidth?: string;
  className?: string;
}

export const Modal: React.FC<ModalProps> = ({
  id,
  isOpen,
  onClose,
  title,
  children,
  maxWidth = '500px',
  className = ''
}) => {
  if (!isOpen) return null;

  return (
    <div id={id} className="modal-backdrop" style={{ display: 'flex' }}>
      <div className={`glass-card modal-card ${className}`.trim()} style={{ maxWidth, width: '90%' }}>
        <div className="modal-header">
          <h2>{title}</h2>
          <button type="button" className="btn-close" onClick={onClose}>
            &times;
          </button>
        </div>
        {children}
      </div>
    </div>
  );
};
