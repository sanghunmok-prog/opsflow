export interface DashboardSummary {
  openCases: number;
  overdueOpenCases: number;
  pendingApprovals: number;
  averageOpenAgeHours: number;
  slaBreachRate: number;
}

export interface DashboardBreakdowns {
  byStatus: DashboardBreakdownItem[];
  byPriority: DashboardBreakdownItem[];
  byCaseType: DashboardBreakdownItem[];
  byAssignee: DashboardBreakdownItem[];
}

export interface DashboardBreakdownItem {
  key: string;
  label: string;
  count: number;
  routeQuery?: Record<string, string> | null;
}
