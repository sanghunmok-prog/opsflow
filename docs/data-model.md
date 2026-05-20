# Data Model

PR-01 introduces the SQL Server schema that will support future queue, detail, workflow, approval, audit, and dashboard features.

## Entity Summary

- `AppUsers`: demo user records and roles. Passwords and auth tables are intentionally not included in PR-01.
- `CaseTypes`: active business exception categories.
- `SlaRules`: active target hours by case type and priority.
- `Cases`: core exception records with due dates, assignment, lifecycle timestamps, approval requirement, and rowversion concurrency.
- `CaseNotes`: case-level notes.
- `StatusHistories`: workflow timeline entries.
- `AssignmentHistories`: assignment timeline entries.
- `ApprovalRequests`: pending/approved/rejected manager approval samples.
- `AuditLogs`: broad entity-level audit events.

## Mermaid ERD

```mermaid
erDiagram
  AppUsers ||--o{ Cases : created
  AppUsers ||--o{ Cases : assigned
  AppUsers ||--o{ CaseNotes : authors
  AppUsers ||--o{ StatusHistories : changes
  AppUsers ||--o{ AssignmentHistories : assigns
  AppUsers ||--o{ ApprovalRequests : requests_reviews
  AppUsers ||--o{ AuditLogs : acts
  CaseTypes ||--o{ SlaRules : has
  CaseTypes ||--o{ Cases : categorizes
  Cases ||--o{ CaseNotes : has
  Cases ||--o{ StatusHistories : has
  Cases ||--o{ AssignmentHistories : has
  Cases ||--o{ ApprovalRequests : has

  AppUsers {
    uniqueidentifier Id PK
    nvarchar Email UK
    nvarchar DisplayName
    nvarchar Role
    bit IsActive
    datetime2 CreatedAtUtc
    datetime2 UpdatedAtUtc
  }

  CaseTypes {
    uniqueidentifier Id PK
    nvarchar Name UK
    nvarchar Description
    bit IsActive
    datetime2 CreatedAtUtc
    datetime2 UpdatedAtUtc
  }

  SlaRules {
    uniqueidentifier Id PK
    uniqueidentifier CaseTypeId FK
    nvarchar Priority
    int TargetHours
    bit IsActive
    datetime2 CreatedAtUtc
    datetime2 UpdatedAtUtc
  }

  Cases {
    uniqueidentifier Id PK
    nvarchar CaseNumber UK
    nvarchar Title
    nvarchar Description
    uniqueidentifier CaseTypeId FK
    nvarchar Priority
    nvarchar Status
    uniqueidentifier AssignedToUserId FK
    uniqueidentifier CreatedByUserId FK
    datetime2 DueAtUtc
    datetime2 ResolvedAtUtc
    datetime2 ClosedAtUtc
    bit RequiresApproval
    datetime2 CreatedAtUtc
    datetime2 UpdatedAtUtc
    rowversion RowVersion
  }

  CaseNotes {
    uniqueidentifier Id PK
    uniqueidentifier CaseId FK
    uniqueidentifier AuthorUserId FK
    nvarchar Body
    datetime2 CreatedAtUtc
  }

  StatusHistories {
    uniqueidentifier Id PK
    uniqueidentifier CaseId FK
    nvarchar FromStatus
    nvarchar ToStatus
    uniqueidentifier ChangedByUserId FK
    nvarchar Reason
    datetime2 CreatedAtUtc
  }

  AssignmentHistories {
    uniqueidentifier Id PK
    uniqueidentifier CaseId FK
    uniqueidentifier FromUserId FK
    uniqueidentifier ToUserId FK
    uniqueidentifier AssignedByUserId FK
    datetime2 CreatedAtUtc
  }

  ApprovalRequests {
    uniqueidentifier Id PK
    uniqueidentifier CaseId FK
    uniqueidentifier RequestedByUserId FK
    uniqueidentifier ReviewedByUserId FK
    nvarchar Status
    nvarchar RequestReason
    nvarchar DecisionReason
    datetime2 RequestedAtUtc
    datetime2 DecisionAtUtc
  }

  AuditLogs {
    uniqueidentifier Id PK
    uniqueidentifier ActorUserId FK
    nvarchar EntityType
    uniqueidentifier EntityId
    nvarchar Action
    nvarchar OldValuesJson
    nvarchar NewValuesJson
    nvarchar MetadataJson
    nvarchar CorrelationId
    datetime2 CreatedAtUtc
  }
```

## Important Constraints

- Enums are stored as strings for readable SQL.
- `Cases.CaseNumber`, `AppUsers.Email`, and `CaseTypes.Name` are unique.
- Active SLA rules are unique by `CaseTypeId` and `Priority`.
- Pending approvals are constrained to one pending request per case.
- History and audit relationships avoid cascade delete.
- `Cases.RowVersion` is configured for SQL Server optimistic concurrency.
