# API Contract

The API currently exposes only a basic health endpoint:

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/health` | Confirms the API process is running. |

Business API endpoints will be added in later PRs. PR-01 adds the database schema that will support the future case queue, case detail, workflow, approval, audit timeline, and dashboard endpoints.

## Planned Contract Areas

- Authentication and current-user profile
- Role-aware case queue and case detail
- SLA due date and overdue indicators
- Notes and status workflow
- Manager reassignment
- Manager approval decisions
- Audit timeline
- SQL-backed dashboard metrics

## Error Handling Placeholder

Problem Details, validation responses, authorization failures, and concurrency responses will be documented when those behaviors are implemented.

## PR-01 Boundary

No authentication endpoints, case endpoints, approval endpoints, dashboard endpoints, or lookup endpoints are implemented in PR-01.
