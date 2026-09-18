# Research: Document Upload and Management

## Decision

- Use a layered document feature built on the existing `Models`, `Data`, `Services`, and `Pages` structure.
- Store document metadata in the relational database and uploaded files in a local `AppData/uploads` structure outside `wwwroot`.
- Generate a GUID-based unique file name before persisting metadata to avoid duplicate key and orphan-record problems.
- Enforce access control in the service layer using the current user, project membership, and role model.
- Keep the storage abstraction behind `IFileStorageService` for a future Azure Blob implementation without changing the page or service callers.

## Rationale

The repository already implements the training-safe pattern of a single ASP.NET Core application with service-layer authorization and a mock cookie auth model. The document feature should follow that same design instead of creating a separate mini-architecture. Because the project is intentionally offline-first, local filesystem storage is the correct default while still allowing a migration-ready abstraction.

## Alternatives considered

1. Storing files directly under `wwwroot`
   - Rejected because it exposes upload content to the web root and conflicts with the security requirement to protect files behind authorization checks.

2. Using direct DB BLOB storage
   - Rejected because it violates the project’s guidance for local file storage and other architecture patterns.

3. Bypassing service-layer authorization checks
   - Rejected because it would create an IDOR vulnerability and conflict with the repository’s security-first constitution.

4. Using Azure Blob storage in the training implementation
   - Rejected because the project explicitly requires offline operation and no cloud dependency for the default workflow.

## Key findings

- Existing `ApplicationDbContext` and model patterns provide a strong place for new document metadata and relationship tables.
- `ProjectService` and `TaskService` already implement authorization checks based on project membership and role, which can be mirrored for document access.
- The app is configured to use SQLite by default when no SQL Server connection is supplied, so the document feature should remain compatible with local development patterns.
- Current roles and project membership already map to the required document permissions: employees, team leads, project managers, and administrators.

## Open design assumptions resolved

- Document IDs will be int-based to match the current relational keys.
- Categories will be stored as text values to stay aligned with the project’s simplified data model.
- File validation will happen before persistence, with extension whitelisting, file-size enforcement, and safe generation of unique file names.
- Shared documents will be represented as a separate sharing relationship entity rather than embedding access in the base document row.
