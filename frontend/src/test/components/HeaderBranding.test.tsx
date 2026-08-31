import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import { Header } from '../../shared/components/Header';
import { isImageUrl, updateFaviconAndTitle } from '../../shared/utils/branding';
import { mockApiResponse } from '../setup';

/**
 * @requirement UI-05
 * @category UI
 * @type PositiveFeature
 * @description Header branding rendering with PNG image logo vs FontAwesome icon, browser tab title synchronization, and dynamic favicon updates.
 */
describe('Header Branding and Favicon Sync', () => {
  beforeEach(() => {
    document.title = 'Default Title';
    const existingIcons = document.querySelectorAll("link[rel*='icon']");
    existingIcons.forEach((el) => el.remove());
  });

  describe('isImageUrl', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description identifies image URLs and paths accurately
     */
    it('identifies image URLs and paths accurately', () => {
      expect(isImageUrl('/api/config/branding/logo')).toBe(true);
      expect(isImageUrl('/images/logo.png')).toBe(true);
      expect(isImageUrl('https://example.com/logo.svg')).toBe(true);
      expect(isImageUrl('http://example.com/icon.ico')).toBe(true);
      expect(isImageUrl('data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAE=')).toBe(true);
      expect(isImageUrl('custom-logo.webp')).toBe(true);
      expect(isImageUrl('custom-logo.JPG')).toBe(true);
    });

    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description identifies FontAwesome class names and invalid inputs as non-image URLs
     */
    it('identifies FontAwesome class names and invalid inputs as non-image URLs', () => {
      expect(isImageUrl('fa-solid fa-bolt')).toBe(false);
      expect(isImageUrl('fa-solid fa-network-wired')).toBe(false);
      expect(isImageUrl('fas fa-server')).toBe(false);
      expect(isImageUrl(null)).toBe(false);
      expect(isImageUrl(undefined)).toBe(false);
      expect(isImageUrl('')).toBe(false);
    });
  });

  describe('updateFaviconAndTitle', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description updates document.title and sets custom image favicon when icon is an image URL
     */
    it('updates document.title and sets custom image favicon when icon is an image URL', () => {
      updateFaviconAndTitle('Custom Gateway', '/api/config/branding/logo');

      expect(document.title).toBe('Custom Gateway - Model Context Gateway');
      const faviconLink = document.querySelector<HTMLLinkElement>("link[rel~='icon']");
      expect(faviconLink).not.toBeNull();
      expect(faviconLink?.href).toContain('/api/config/branding/logo');
    });

    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description sets default title and generated SVG favicon when branding is null or uses FontAwesome icon
     */
    it('sets default title and generated SVG favicon when branding is null or uses FontAwesome icon', () => {
      updateFaviconAndTitle(null, 'fa-solid fa-bolt');

      expect(document.title).toBe('Model Context Gateway (MCG)');
      const faviconLink = document.querySelector<HTMLLinkElement>("link[rel~='icon']");
      expect(faviconLink).not.toBeNull();
      expect(faviconLink?.href).toContain('data:image/svg+xml');
      expect(faviconLink?.type).toBe('image/svg+xml');
    });
  });

  describe('Header Component Branding Rendering', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description renders img element with logo-icon logo-img class when branding.icon is an image endpoint
     */
    it('renders img element with logo-icon logo-img class when branding.icon is an image endpoint', async () => {
      mockApiResponse('/api/config/branding', {
        title: 'Acme Gateway',
        icon: '/api/config/branding/logo'
      });

      await act(async () => {
        render(<Header />);
      });

      await waitFor(() => {
        expect(screen.getByText('Acme Gateway')).toBeInTheDocument();
      });

      const img = screen.getByAltText('Logo');
      expect(img).toBeInTheDocument();
      expect(img).toHaveAttribute('src', '/api/config/branding/logo');
      expect(img).toHaveClass('logo-icon');
      expect(img).toHaveClass('logo-img');
      expect(document.title).toBe('Acme Gateway - Model Context Gateway');
    });

    /**
     * @requirement UI-01
     * @category UI
     * @type Positive
     * @description renders FontAwesome i element when branding.icon is a FontAwesome class
     */
    it('renders FontAwesome i element when branding.icon is a FontAwesome class', async () => {
      mockApiResponse('/api/config/branding', {
        title: 'Lightning MCP',
        icon: 'fa-solid fa-bolt'
      });

      await act(async () => {
        render(<Header />);
      });

      await waitFor(() => {
        expect(screen.getByText('Lightning MCP')).toBeInTheDocument();
      });

      const icon = document.querySelector('.header-logo i.logo-icon');
      expect(icon).toBeInTheDocument();
      expect(icon).toHaveClass('fa-solid');
      expect(icon).toHaveClass('fa-bolt');
      expect(screen.queryByAltText('Logo')).toBeNull();
      expect(document.title).toBe('Lightning MCP - Model Context Gateway');
    });
  });
});
