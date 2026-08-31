export interface ToolItem {
  name: string;
  description: string;
  inputSchema?: {
    type?: string;
    properties?: Record<string, any>;
    required?: string[];
  };
}

export interface PromptItem {
  name: string;
  description: string;
  arguments?: {
    name: string;
    description?: string;
    required?: boolean;
  }[];
}

export interface ResourceItem {
  uri: string;
  name: string;
  mimeType?: string;
  description?: string;
}

export interface TemplateItem {
  uriTemplate: string;
  name: string;
  description?: string;
}

export interface ResourcesData {
  resources: ResourceItem[];
  templates: TemplateItem[];
}

export interface LogEntry {
  id: string;
  timestamp: string;
  level: number;
  category: string;
  message: string;
  exception?: string;
}
