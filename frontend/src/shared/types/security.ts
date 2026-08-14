export interface AccessPolicy {
  id?: string;
  targetId: string;
  requiredGroup: string;
  isAllowed: boolean;
}

export interface GroupMapping {
  id?: string;
  externalId: string;
  internalGroup: string;
}
