import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { GeneralTab } from '../../components/settings/GeneralTab';
import * as settingsApi from '../../api/settingsApi';

/**
 * @requirement UI-05
 * @category UI
 * @type PositiveFeature
 * @description General Settings Tab provides an image upload button and live preview for custom branding logos.
 */
describe('GeneralTab Logo Upload and Live Preview', () => {
  const mockSaveSettings = vi.fn().mockResolvedValue(true);

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders branding label and FontAwesome icon preview when icon is a CSS class', () => {
    render(
      <GeneralTab
        settings={{
          dashboardTitle: 'MCP Gateway',
          dashboardIcon: 'fa-solid fa-network-wired',
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: '',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
          allowOpenClientRegistration: true,
          globalMaxKeys: 100,
          userMaxKeys: 5,
        }}
        saveEmbeddingSettings={mockSaveSettings}
      />
    );

    expect(
      screen.getByLabelText(/Header Icon or Logo URL \(FontAwesome class or Image URL\)/i)
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /upload image/i })).toBeInTheDocument();

    const previewIcon = document.querySelector('.logo-preview-box i');
    expect(previewIcon).toBeInTheDocument();
    expect(previewIcon).toHaveClass('fa-solid');
    expect(previewIcon).toHaveClass('fa-network-wired');
    expect(document.querySelector('.logo-preview-box img')).toBeNull();
  });

  it('renders img live preview when dashboardIcon is an image URL', () => {
    render(
      <GeneralTab
        settings={{
          dashboardTitle: 'MCP Gateway',
          dashboardIcon: '/api/config/branding/logo',
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: '',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
          allowOpenClientRegistration: true,
          globalMaxKeys: 100,
          userMaxKeys: 5,
        }}
        saveEmbeddingSettings={mockSaveSettings}
      />
    );

    const img = screen.getByAltText('Logo Preview');
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('src', '/api/config/branding/logo');
    expect(img).toHaveClass('logo-img');
    expect(document.querySelector('.logo-preview-box i')).toBeNull();
  });

  it('updates dashboardIcon and live preview when a logo image file is uploaded', async () => {
    const uploadSpy = vi.spyOn(settingsApi, 'uploadBrandingLogo').mockResolvedValue({
      url: '/api/config/branding/logo',
      success: true,
    });

    render(
      <GeneralTab
        settings={{
          dashboardTitle: 'MCP Gateway',
          dashboardIcon: 'fa-solid fa-network-wired',
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: '',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
          allowOpenClientRegistration: true,
          globalMaxKeys: 100,
          userMaxKeys: 5,
        }}
        saveEmbeddingSettings={mockSaveSettings}
      />
    );

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    expect(fileInput).toBeInTheDocument();

    const file = new File(['dummy-logo-content'], 'custom-logo.png', { type: 'image/png' });

    await act(async () => {
      fireEvent.change(fileInput, { target: { files: [file] } });
    });

    await waitFor(() => {
      expect(uploadSpy).toHaveBeenCalledWith(file);
    });

    const iconInput = screen.getByLabelText(
      /Header Icon or Logo URL \(FontAwesome class or Image URL\)/i
    ) as HTMLInputElement;
    expect(iconInput.value).toBe('/api/config/branding/logo');

    const previewImg = screen.getByAltText('Logo Preview');
    expect(previewImg).toBeInTheDocument();
    expect(previewImg).toHaveAttribute('src', '/api/config/branding/logo');
  });

  it('saves settings with the updated logo URL when form is submitted after upload', async () => {
    vi.spyOn(settingsApi, 'uploadBrandingLogo').mockResolvedValue({
      url: '/api/config/branding/logo',
      success: true,
    });

    render(
      <GeneralTab
        settings={{
          dashboardTitle: 'Custom MCP Hub',
          dashboardIcon: 'fa-solid fa-server',
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: '',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
          allowOpenClientRegistration: true,
          globalMaxKeys: 100,
          userMaxKeys: 5,
        }}
        saveEmbeddingSettings={mockSaveSettings}
      />
    );

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['dummy-logo'], 'logo.svg', { type: 'image/svg+xml' });

    await act(async () => {
      fireEvent.change(fileInput, { target: { files: [file] } });
    });

    const saveBtn = screen.getByRole('button', { name: /save settings/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(mockSaveSettings).toHaveBeenCalledWith(
      expect.objectContaining({
        dashboardTitle: 'Custom MCP Hub',
        dashboardIcon: '/api/config/branding/logo',
      })
    );
  });
});
