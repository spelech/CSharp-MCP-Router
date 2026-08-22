import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { GeneralTab } from '../../components/settings/GeneralTab';

describe('GeneralTab component', () => {
  /**
   * @requirement AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE
   * @category AUTH
   * @type PositiveFeature
   * @description Renders GeneralTab with security default quota inputs and saves settings.
   */
  it('renders GeneralTab with security default quota inputs and triggers save', async () => {
    const saveSpy = vi.fn().mockResolvedValue(true);
    render(
      <GeneralTab
        settings={{
          dashboardTitle: 'Custom MCP Gateway',
          dashboardIcon: 'fa-solid fa-server',
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: 'http://litellm:4000/v1/embeddings',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
          allowOpenClientRegistration: false,
          userMaxKeys: 5,
          globalMaxKeys: 100,
        }}
        saveEmbeddingSettings={saveSpy}
      />
    );

    expect(screen.getByText('General Settings')).toBeInTheDocument();
    expect(screen.getByText('Security Defaults')).toBeInTheDocument();

    const userQuotaInput = screen.getByLabelText(/Default User Quota \(UserMaxKeys\)/i);
    const globalQuotaInput = screen.getByLabelText(/Global Max Keys/i);
    const dcrCheckbox = screen.getByRole('checkbox');

    expect(userQuotaInput).toHaveValue(5);
    expect(globalQuotaInput).toHaveValue(100);
    expect(dcrCheckbox).not.toBeChecked();

    // Modify user and global quotas
    fireEvent.change(userQuotaInput, { target: { value: '10' } });
    fireEvent.change(globalQuotaInput, { target: { value: '250' } });
    fireEvent.click(dcrCheckbox);

    expect(userQuotaInput).toHaveValue(10);
    expect(globalQuotaInput).toHaveValue(250);
    expect(dcrCheckbox).toBeChecked();

    const saveBtn = screen.getByRole('button', { name: /save settings/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(saveSpy).toHaveBeenCalledWith({
      dashboardTitle: 'Custom MCP Gateway',
      dashboardIcon: 'fa-solid fa-server',
      embeddingProvider: 'local',
      embeddingModelDir: 'data/models',
      embeddingApiUrl: 'http://litellm:4000/v1/embeddings',
      embeddingApiModel: 'all-MiniLM-L6-v2',
      embeddingApiKey: '',
      allowOpenClientRegistration: true,
      userMaxKeys: 10,
      globalMaxKeys: 250,
    });
  });

  /**
   * @requirement AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE
   * @category AUTH
   * @type PositiveFeature
   * @description Updates form state when settings prop changes.
   */
  it('updates form state when settings prop changes', async () => {
    const saveSpy = vi.fn().mockResolvedValue(true);
    const { rerender } = render(
      <GeneralTab
        settings={{
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: 'http://litellm:4000/v1/embeddings',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
          userMaxKeys: 5,
          globalMaxKeys: 100,
        }}
        saveEmbeddingSettings={saveSpy}
      />
    );

    expect(screen.getByLabelText(/Default User Quota \(UserMaxKeys\)/i)).toHaveValue(5);

    rerender(
      <GeneralTab
        settings={{
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: 'http://litellm:4000/v1/embeddings',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
          userMaxKeys: 20,
          globalMaxKeys: 500,
        }}
        saveEmbeddingSettings={saveSpy}
      />
    );

    expect(screen.getByLabelText(/Default User Quota \(UserMaxKeys\)/i)).toHaveValue(20);
    expect(screen.getByLabelText(/Global Max Keys/i)).toHaveValue(500);
  });
});
