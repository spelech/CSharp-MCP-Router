export class ApiError extends Error {
  public status: number;
  public statusText: string;

  constructor(status: number, statusText: string, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.statusText = statusText;
  }
}

export interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  body?: any;
}

export async function apiRequest<T = any>(url: string, options: ApiRequestOptions = {}): Promise<T> {
  const defaultHeaders: Record<string, string> = {
    'Content-Type': 'application/json'
  };

  const fetchOptions: RequestInit = {
    ...options,
    headers: {
      ...defaultHeaders,
      ...((options.headers as Record<string, string>) || {})
    }
  };

  if (options.body !== undefined) {
    if (typeof options.body === 'object' && !(options.body instanceof Blob) && !(options.body instanceof FormData)) {
      fetchOptions.body = JSON.stringify(options.body);
    } else {
      fetchOptions.body = options.body;
    }
  }

  const response = await fetch(url, fetchOptions);
  if (!response.ok) {
    const text = await response.text();
    console.error(`API Error: ${response.status} ${response.statusText} - ${text}`);
    throw new Error(text || `Request failed with status ${response.status}`);
  }

  if (response.status === 204) return null as any;
  return response.json();
}

export function escapeHtml(str: string | null | undefined): string {
  if (!str) return '';
  return str.replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
}
