import React from 'react';
import { useToastStore } from '../stores/useToastStore';
import { Toast } from '../types';

export const Toasts: React.FC = () => {
  const { toasts, removeToast } = useToastStore();

  if (toasts.length === 0) return null;

  return (
    <div id="toast-container">
      {toasts.map((toast: Toast) => {
        let iconClass = 'fa-circle-info';
        if (toast.type === 'success') iconClass = 'fa-circle-check';
        if (toast.type === 'error') iconClass = 'fa-circle-exclamation';

        return (
          <div key={toast.id} className={`toast-card toast-${toast.type}`}>
            <div className="toast-content">
              <i className={`fa-solid ${iconClass}`}></i>
              <span>{toast.message}</span>
            </div>
            <button
              type="button"
              className="toast-close"
              onClick={() => removeToast(toast.id)}
            >
              <i className="fa-solid fa-xmark"></i>
            </button>
          </div>
        );
      })}
    </div>
  );
};
