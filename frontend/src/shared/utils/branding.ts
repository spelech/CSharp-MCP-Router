/**
 * Branded default favicon Data URI (dark slate background #0f172a, vibrant orange #f97316 router geometry)
 */
export const DEFAULT_FAVICON_DATA_URI = `data:image/svg+xml,${encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">` +
  `<rect width="100" height="100" rx="20" fill="#0f172a"/>` +
  `<circle cx="50" cy="50" r="30" fill="none" stroke="#f97316" stroke-width="8"/>` +
  `<circle cx="50" cy="50" r="14" fill="#f97316"/>` +
  `<line x1="50" y1="10" x2="50" y2="28" stroke="#f97316" stroke-width="6" stroke-linecap="round"/>` +
  `<line x1="50" y1="72" x2="50" y2="90" stroke="#f97316" stroke-width="6" stroke-linecap="round"/>` +
  `<line x1="10" y1="50" x2="28" y2="50" stroke="#f97316" stroke-width="6" stroke-linecap="round"/>` +
  `<line x1="72" y1="50" x2="90" y2="50" stroke="#f97316" stroke-width="6" stroke-linecap="round"/>` +
  `</svg>`
)}`;

/**
 * Checks whether an icon descriptor is a direct image URL / path vs a FontAwesome CSS class.
 */
export function isImageUrl(icon?: string | null): boolean {
  if (!icon || typeof icon !== 'string') return false;
  const trimmed = icon.trim();
  if (!trimmed) return false;

  if (
    trimmed.startsWith('/') ||
    trimmed.startsWith('http://') ||
    trimmed.startsWith('https://') ||
    trimmed.startsWith('data:image/')
  ) {
    return true;
  }

  const lower = trimmed.toLowerCase();
  return (
    lower.endsWith('.png') ||
    lower.endsWith('.jpg') ||
    lower.endsWith('.jpeg') ||
    lower.endsWith('.svg') ||
    lower.endsWith('.ico') ||
    lower.endsWith('.webp')
  );
}

/**
 * Updates browser tab title and favicon link based on branding configurations.
 */
export function updateFaviconAndTitle(title?: string | null, icon?: string | null): void {
  if (typeof document === 'undefined') return;

  const trimmedTitle = title ? title.trim() : '';
  document.title = trimmedTitle ? `${trimmedTitle} - MCP Router` : 'MCP Router Gateway Dashboard';

  let link = document.querySelector<HTMLLinkElement>("link[rel~='icon']");
  if (!link) {
    link = document.createElement('link');
    link.rel = 'icon';
    document.head.appendChild(link);
  }

  if (icon && isImageUrl(icon)) {
    link.href = icon;
    const lower = icon.toLowerCase();
    if (lower.endsWith('.png') || lower.includes('image/png')) {
      link.type = 'image/png';
    } else if (lower.endsWith('.svg') || lower.includes('image/svg+xml')) {
      link.type = 'image/svg+xml';
    } else if (lower.endsWith('.ico') || lower.includes('image/x-icon') || lower.includes('image/vnd.microsoft.icon')) {
      link.type = 'image/x-icon';
    } else if (lower.endsWith('.webp') || lower.includes('image/webp')) {
      link.type = 'image/webp';
    } else if (lower.endsWith('.jpg') || lower.endsWith('.jpeg') || lower.includes('image/jpeg')) {
      link.type = 'image/jpeg';
    }
  } else {
    link.href = DEFAULT_FAVICON_DATA_URI;
    link.type = 'image/svg+xml';
  }
}
