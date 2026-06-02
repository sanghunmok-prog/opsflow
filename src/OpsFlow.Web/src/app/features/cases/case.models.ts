export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CaseTypeSummary {
  id: string;
  name: string;
}

export interface UserSummary {
  id: string;
  displayName: string;
}

export interface CaseListItem {
  id: string;
  caseNumber: string;
  title: string;
  caseType: CaseTypeSummary;
  priority: CasePriority;
  status: CaseStatus;
  assignedTo: UserSummary | null;
  createdAtUtc: string;
  dueAtUtc: string;
  isOverdue: boolean;
}

export interface CaseListQuery {
  page: number;
  pageSize: number;
  search?: string;
  status?: CaseStatus | '';
  priority?: CasePriority | '';
  overdue?: boolean | null;
  sortBy: CaseSortBy;
  sortDirection: SortDirection;
}

export interface CreateCaseRequest {
  title: string;
  description?: string;
  caseTypeId: string;
  priority: CasePriority | '';
}

export interface CreateCaseResponse {
  id: string;
  caseNumber: string;
  title: string;
  description: string;
  caseType: CaseTypeSummary;
  priority: CasePriority;
  status: CaseStatus;
  assignedTo: UserSummary | null;
  createdBy: UserSummary;
  createdAtUtc: string;
  updatedAtUtc: string;
  dueAtUtc: string;
  isOverdue: boolean;
  rowVersion: string;
}

export interface CaseDetail {
  id: string;
  caseNumber: string;
  title: string;
  description: string;
  caseType: CaseTypeSummary;
  priority: CasePriority;
  status: CaseStatus;
  assignedTo: UserSummary | null;
  createdBy: UserSummary;
  createdAtUtc: string;
  updatedAtUtc: string;
  dueAtUtc: string;
  isOverdue: boolean;
  rowVersion: string;
}

export interface CaseNote {
  id: string;
  body: string;
  createdBy: UserSummary;
  createdAtUtc: string;
}

export interface CreateCaseNoteRequest {
  body: string;
}

export interface TimelineItem {
  id: string;
  action: 'CaseCreated' | 'NoteAdded';
  actor: UserSummary | null;
  createdAtUtc: string;
  description: string;
}

export type CasePriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type CaseStatus =
  | 'New'
  | 'Assigned'
  | 'InReview'
  | 'WaitingInfo'
  | 'Resolved'
  | 'PendingApproval'
  | 'Closed'
  | 'Reopened';
export type CaseSortBy = 'caseNumber' | 'createdAtUtc' | 'dueAtUtc' | 'priority' | 'status';
export type SortDirection = 'asc' | 'desc';
