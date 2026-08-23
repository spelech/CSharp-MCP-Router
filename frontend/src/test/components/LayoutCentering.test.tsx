import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import fs from 'fs';
import path from 'path';
import App from '../../App';
import { SettingsView } from '../../components/settings/SettingsView';
import { AppKeysCard } from '../../components/clients/AppKeysCard';
import { useUserStore } from '../../stores/useUserStore';
import { mockApiResponse, defaultMockData } from '../setup';

describe('Layout and Sub-Navigation Centering', () => {
  beforeEach(() => {
    mockApiResponse('/api/me', defaultMockData.me);
    useUserStore.setState({
      user: defaultMockData.me
    });
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Navigation tab bars use centered alignment across top bar and sub bars.
   */
  it('renders top navigation bar with centered alignment in layout.css and App', async () => {
    await act(async () => {
      render(<App />);
    });

    const nav = document.querySelector('nav.tabs-nav');
    expect(nav).toBeInTheDocument();

    const cssPath = path.resolve(__dirname, '../../styles/layout.css');
    const cssContent = fs.readFileSync(cssPath, 'utf-8');
    
    // Check that .tabs-nav in layout.css specifies justify-content: center
    const tabsNavMatch = cssContent.match(/\.tabs-nav\s*\{([^}]+)\}/);
    expect(tabsNavMatch).not.toBeNull();
    expect(tabsNavMatch![1]).toContain('justify-content: center');
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Navigation tab bars use centered alignment across top bar and sub bars.
   */
  it('renders tester tabs with centered alignment in tester.css', () => {
    const cssPath = path.resolve(__dirname, '../../styles/tester.css');
    const cssContent = fs.readFileSync(cssPath, 'utf-8');

    // Check that .tester-tabs in tester.css specifies justify-content: center
    const testerTabsMatch = cssContent.match(/\.tester-tabs\s*\{([^}]+)\}/);
    expect(testerTabsMatch).not.toBeNull();
    expect(testerTabsMatch![1]).toContain('justify-content: center');
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Navigation tab bars use centered alignment across top bar and sub bars.
   */
  it('renders SettingsView sub-navigation bar with centered alignment', async () => {
    await act(async () => {
      render(<SettingsView />);
    });

    const subNav = document.querySelector('.settings-sub-nav') as HTMLElement;
    expect(subNav).toBeInTheDocument();
    expect(subNav.style.justifyContent).toBe('center');
  });

  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Navigation tab bars use centered alignment across top bar and sub bars.
   */
  it('renders AppKeysCard sub-navigation tabs with centered alignment for admin', async () => {
    await act(async () => {
      render(<AppKeysCard />);
    });

    const subNav = document.querySelector('.sub-tabs-nav') as HTMLElement;
    expect(subNav).toBeInTheDocument();
    expect(subNav.style.justifyContent).toBe('center');
  });
});
