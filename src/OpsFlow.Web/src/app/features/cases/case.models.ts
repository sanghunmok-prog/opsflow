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
  caseTypeId?: string;
  assignedToUserId?: string;
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
  closedAtUtc?: string | null;
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
  closedAtUtc?: string | null;
  isOverdue: boolean;
  rowVersion: string;
  approvalSummary?: ApprovalSummary | null;
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

export interface AssignCaseRequest {
  assignedToUserId: string;
  reason: string;
  rowVersion?: string;
}

export interface UpdateCaseStatusRequest {
  targetStatus: CaseStatus;
  reason: string;
  rowVersion: string;
}

export interface AnalystLookup {
  id: string;
  displayName: string;
  email: string;
}

export interface TimelineItem {
  id: string;
  action:
    | 'CaseCreated'
    | 'NoteAdded'
    | 'Assigned'
    | 'StatusChanged'
    | 'ClosureRequested'
    | 'ApprovalApproved'
    | 'ApprovalRejected'
    | 'CaseReopened';
  actor: UserSummary | null;
  createdAtUtc: string;
  description: string;
}

export interface ApprovalSummary {
  approvalId: string;
  status: ApprovalStatus;
  requestReason: string;
  requestedBy: UserSummary;
  requestedAtUtc: string;
  decisionReason: string | null;
  reviewedBy: UserSummary | null;
  decisionAtUtc: string | null;
}

export interface RequestClosureRequest {
  requestReason: string;
  rowVersion: string;
}

export interface ApprovalDecisionRequest {
  decisionReason?: string | null;
  rowVersion?: string | null;
}

export interface ApprovalRequestResult {
  id: string;
  caseId: string;
  caseNumber: string;
  caseTitle: string;
  priority: CasePriority;
  caseStatus: CaseStatus;
  approvalStatus: ApprovalStatus;
  requestReason: string;
  requestedBy: UserSummary;
  requestedAtUtc: string;
  rowVersion: string;
}

export interface ApprovalQueueItem {
  id: string;
  caseId: string;
  caseNumber: string;
  caseTitle: string;
  priority: CasePriority;
  caseStatus: CaseStatus;
  requestReason: string;
  requestedBy: UserSummary;
  requestedAtUtc: string;
  assignedTo: UserSummary | null;
  dueAtUtc: string;
  isOverdue: boolean;
  rowVersion: string;
}

export interface ApprovalDecisionResult {
  approvalId: string;
  caseId: string;
  caseNumber: string;
  caseTitle: string;
  approvalStatus: ApprovalStatus;
  caseStatus: CaseStatus;
  decisionReason: string | null;
  reviewedBy: UserSummary;
  decisionAtUtc: string;
  rowVersion: string;
}

export type CasePriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected';
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
