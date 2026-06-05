# Entity Relationship Diagram

This ERD shows the persistence model used by EF Core and SQL Server for OpsFlow workflow data. Identity tables are simplified to the user/role relationships relevant to authorization.

```mermaid
erDiagram
    AspNetUsers {
        uniqueidentifier Id PK
        string Email
        string DisplayName
        bool IsActive
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }

    AspNetRoles {
        uniqueidentifier Id PK
        string Name
        string NormalizedName
    }

    AspNetUserRoles {
        uniqueidentifier UserId FK
        uniqueidentifier RoleId FK
    }

    CaseTypes {
        uniqueidentifier Id PK
        string Name UK
        string Description
        bool IsActive
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }

    SlaRules {
        uniqueidentifier Id PK
        uniqueidentifier CaseTypeId FK
        string Priority
        int TargetHours
        bool IsActive
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }

    Cases {
        uniqueidentifier Id PK
        string CaseNumber UK
        string Title
        string Description
        string Priority
        string Status
        uniqueidentifier CaseTypeId FK
        uniqueidentifier AssignedToUserId FK
        uniqueidentifier CreatedByUserId FK
        datetime DueAtUtc
        datetime ResolvedAtUtc
        datetime ClosedAtUtc
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
        rowversion RowVersion
    }

    CaseNotes {
        uniqueidentifier Id PK
        uniqueidentifier CaseId FK
        uniqueidentifier AuthorUserId FK
        string Body
        datetime CreatedAtUtc
    }

    StatusHistories {
        uniqueidentifier Id PK
        uniqueidentifier CaseId FK
        string FromStatus
        string ToStatus
        uniqueidentifier ChangedByUserId FK
        string Reason
        datetime CreatedAtUtc
    }

    AssignmentHistories {
        uniqueidentifier Id PK
        uniqueidentifier CaseId FK
        uniqueidentifier FromUserId FK
        uniqueidentifier ToUserId FK
        uniqueidentifier AssignedByUserId FK
        string Reason
        datetime CreatedAtUtc
    }

    ApprovalRequests {
        uniqueidentifier Id PK
        uniqueidentifier CaseId FK
        uniqueidentifier RequestedByUserId FK
        uniqueidentifier ReviewedByUserId FK
        string Status
        string RequestReason
        string DecisionReason
        datetime RequestedAtUtc
        datetime DecisionAtUtc
    }

    AuditLogs {
        uniqueidentifier Id PK
        uniqueidentifier ActorUserId FK
        string EntityType
        uniqueidentifier EntityId
        string Action
        string OldValuesJson
        string NewValuesJson
        string MetadataJson
        string CorrelationId
        datetime CreatedAtUtc
    }

    AspNetUsers ||--o{ AspNetUserRoles : has
    AspNetRoles ||--o{ AspNetUserRoles : grants
    CaseTypes ||--o{ SlaRules : defines
    CaseTypes ||--o{ Cases : classifies
    AspNetUsers ||--o{ Cases : creates
    AspNetUsers ||--o{ Cases : assigned
    Cases ||--o{ CaseNotes : has
    AspNetUsers ||--o{ CaseNotes : authors
    Cases ||--o{ StatusHistories : records
    AspNetUsers ||--o{ StatusHistories : changes
    Cases ||--o{ AssignmentHistories : records
    AspNetUsers ||--o{ AssignmentHistories : assigned_by
    AspNetUsers ||--o{ AssignmentHistories : assigned_to
    Cases ||--o{ ApprovalRequests : requests
    AspNetUsers ||--o{ ApprovalRequests : requests
    AspNetUsers ||--o{ ApprovalRequests : reviews
    AspNetUsers ||--o{ AuditLogs : acts
    Cases ||..o{ AuditLogs : logical_timeline
```

## Relationship Notes

`AuditLogs.EntityType + EntityId` is a logical case timeline relationship, not a database foreign key to `Cases`. The table remains generic, while the current timeline exposes supported case business events for `EntityType = "Case"`.

Nullable workflow fields:

- `Cases.AssignedToUserId` can be null for new unassigned cases.
- `Cases.ResolvedAtUtc` is null until a case reaches `Resolved`; it is retained after later approval or reopen operations.
- `Cases.ClosedAtUtc` is null until closure and is cleared when a case is reopened.
- `ApprovalRequests.ReviewedByUserId`, `DecisionReason`, and `DecisionAtUtc` are unset while approval is pending.
- `StatusHistories.FromStatus` is null for the initial seeded creation history.

## Constraints

- `Cases.CaseNumber` is unique.
- `CaseTypes.Name` is unique.
- Active SLA rules are unique by `CaseTypeId + Priority` using a filtered unique index.
- One pending approval per case is enforced by a filtered unique index on `ApprovalRequests.CaseId` where `Status = 'Pending'`.
- History and audit relationships use restricted or no-action delete behavior; case history is not cascade-deleted.
- `Cases.RowVersion` is configured as an EF Core concurrency token.
