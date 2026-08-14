export type StatusType = 'online' | 'offline' | 'connecting' | 'warning' | 'error' | 'disabled';

export interface PaginationOptions {
  page: number;
  pageSize: number | 'all';
}
