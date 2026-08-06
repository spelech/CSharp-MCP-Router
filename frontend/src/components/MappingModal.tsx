import React, { useEffect, useState } from 'react';
import { useSettingsStore } from '../stores/useSettingsStore';

export const MappingModal: React.FC = () => {
  const { isMappingModalOpen, editingMapping, saveMapping, closeMappingModal } = useSettingsStore();

  const [externalId, setExternalId] = useState('');
  const [internalGroup, setInternalGroup] = useState('');

  useEffect(() => {
    if (isMappingModalOpen) {
      if (editingMapping) {
        setExternalId(editingMapping.externalId || '');
        setInternalGroup(editingMapping.internalGroup || '');
      } else {
        setExternalId('');
        setInternalGroup('');
      }
    }
  }, [isMappingModalOpen, editingMapping]);

  if (!isMappingModalOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload: any = {
      externalId,
      internalGroup,
    };
    if (editingMapping) {
      payload.id = editingMapping.id;
    }
    await saveMapping(payload);
  };

  return (
    <div className="modal-backdrop" id="mapping-modal" style={{ display: 'flex' }}>
      <div className="glass-card modal-card" style={{ maxWidth: '500px', width: '90%' }}>
        <div className="modal-header">
          <h2>
            <i className="fa-solid fa-user-group"></i> {editingMapping ? 'Edit Group Mapping' : 'Create Group Mapping'}
          </h2>
          <button type="button" className="btn-close" onClick={closeMappingModal}>
            &times;
          </button>
        </div>
        <form id="mapping-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="mapping-external">External AD SID or OIDC Group</label>
            <input
              type="text"
              id="mapping-external"
              placeholder="e.g. S-1-5-21-... or pocketid_admins"
              value={externalId}
              onChange={(e) => setExternalId(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="mapping-internal">Internal Group Name</label>
            <input
              type="text"
              id="mapping-internal"
              placeholder="e.g. database_users"
              value={internalGroup}
              onChange={(e) => setInternalGroup(e.target.value)}
              required
            />
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={closeMappingModal}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              Save Mapping
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
