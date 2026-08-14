export interface RegisteredClient {
  id: string;
  clientId: string;
  displayName: string;
  isDynamic: boolean;
  scopes?: string[];
}

export interface NewClientResult {
  clientId: string;
  clientSecret: string;
}
