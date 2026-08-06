import React, { useEffect, useState } from 'react';
import { useSettingsStore } from '../stores/useSettingsStore';

export const PolicyModal: React.FC = () => {
  const { isPolicyModalOpen, editingPolicy, savePolicy, closePolicyModal } = useSettingsStore();

  const [targetId, setTargetId] = useState('');
  const [requiredGroup, setRequiredGroup] = useState('');
  const [isAllowed, setIsAllowed] = useState(true);

  useEffect(() => {
    if (isPolicyModalOpen) {
      if (editingPolicy) {
        setTargetId(editingPolicy.targetId || '');
        setRequiredGroup(editingPolicy.requiredGroup || '');
        setIsAllowed(editingPolicy.isAllowed);
      } else {
        setTargetId('');
        setRequiredGroup('');
        setIsAllowed(true);
      }
    }
  }, [isPolicyModalOpen, editingPolicy]);

  if (!isPolicyModalOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload: any = {
      targetId,
      requiredGroup,
      isAllowed,
    };
    if (editingPolicy) {
      payload.id = editingPolicy.id;
    }
    await savePolicy(payload);
  };

  return (
    <div className="modal-backdrop" id="policy-modal" style={{ display: 'flex' }}>
      <div className="glass-card modal-card" style={{ maxWidth: '500px', width: '90%' }}>
        <div className="modal-header">
          <h2>
            <i className="fa-solid fa-shield-halved"></i> {editingPolicy ? 'Edit Access Policy' : 'Create Access Policy'}
          </h2>
          <button type="button" className="btn-close" onClick={closePolicyModal}>
            &times;
          </button>
        </div>
        <form id="policy-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="policy-target">Target ID</label>
            <input
              type="text"
              id="policy-target"
              placeholder="e.g. server:ha or tool:docker__list_containers"
              value={targetId}
              onChange={(e) => setTargetId(e.target.value)}
              required
            />
            <small style={{ color: 'var(--text-muted)', fontSize: '11px' }}>
              Use <code>server:ha</code>, <code>tool:plex__play</code>, <code>prompt:router__diagnose</code>, <code>resource:router://status</code>
            </small>
          </div>

          <div className="form-group">
            <label htmlFor="policy-group">Required Group / Internal Group</label>
            <input
              type="text"
              id="policy-group"
              placeholder="e.g. database_users or Administrators"
              value={requiredGroup}
              onChange={(e) => setRequiredGroup(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="policy-allowed">Policy Mode</label>
            <select
              id="policy-allowed"
              value={isAllowed ? 'true' : 'false'}
              onChange={(e) => setIsAllowed(e.target.value === 'true')}
              required
            >
              <option value="true">ALLOW Access</option>
              <option value="false">DENY Access</option>
            </select>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={closePolicyModal}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              Save Policy
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
