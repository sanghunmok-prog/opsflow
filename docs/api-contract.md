# API Contract

PR-00 exposes only a basic health endpoint:

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/health` | Confirms the API process is running. |

Business API endpoints will be added in later PRs.

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
