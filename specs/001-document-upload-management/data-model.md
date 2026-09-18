# Data Model: Document Upload and Management

## Overview

This feature extends the existing relational model with document metadata, sharing, and auditable activity tracking while reusing the current `User`, `Project`, `ProjectMember`, and `Notification` patterns.

## Entities

### Document

Represents a stored work file and its associated metadata.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| DocumentId | int | Primary key, required | Matches the repo’s integer-key pattern |
| Title | string | Required, max 255 | User-visible document name |
| Description | string? | Max 2000 | Optional summary |
| Category | string | Required | One of: Project Documents, Team Resources, Personal Files, Reports, Presentations, Other |
| FileName | string | Required, max 255 | Original filename, not used as a storage path |
| StoredFileName | string | Required, max 255 | GUID-based safe file name |
| FilePath | string | Required, max 1024 | Relative or storage-local path |
| FileType | string | Required, max 255 | MIME type or file extension metadata |
| FileSizeBytes | long | Required | File size for validation and display |
| UploadedByUserId | int | Required, FK to User | Creator/owner |
| ProjectId | int? | Nullable FK to Project | Optional project association |
| UploadedAtUtc | DateTime | Required | Audit timestamp |
| UpdatedAtUtc | DateTime | Required | Changed when metadata or file is replaced |
| IsDeleted | bool | Default false | Soft-delete support for recovery workflows |

Relationships:
- `Document` belongs to `User` via `UploadedByUserId`
- `Document` belongs to `Project` via `ProjectId` when linked to a project
- `Document` has many `DocumentShare` records
- `Document` has many `DocumentActivityLog` records

### DocumentShare

Tracks explicit sharing relationships between a document and users.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| DocumentShareId | int | Primary key |  |
| DocumentId | int | Required, FK | Shared document |
| UserId | int | Required, FK | Recipient |
| SharedByUserId | int | Required, FK | Owner or manager sharing the document |
| SharedAtUtc | DateTime | Required |  |
| NotificationSent | bool | Default false | Used to avoid duplicate alerts |

Relationships:
- `DocumentShare` belongs to `Document`
- `DocumentShare` belongs to target `User`
- `DocumentShare` belongs to sharing `User`

### DocumentActivityLog

Captures audit events and reporting data.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| ActivityId | int | Primary key |  |
| DocumentId | int | Required, FK |  |
| UserId | int | Required, FK | Actor |
| ActivityType | string | Required, max 50 | Upload, Download, Delete, Share, Replace |
| ActivityAtUtc | DateTime | Required |  |
| Details | string? | Max 2000 | Extra context for audit reports |

Relationships:
- `DocumentActivityLog` belongs to `Document`
- `DocumentActivityLog` belongs to `User`

## Validation rules

- Title is required and should be non-empty after trimming.
- Category must be one of the approved values defined in the stakeholder brief.
- File size must be <= 25 MB.
- File type must be in the allowed whitelist for the project.
- File path must be generated from a GUID-based storage name and must not use the user-supplied original filename directly.
- Project association is optional; if present, the user must be allowed to access that project.
- Users may only share a document with users they have permission to expose it to.

## State transitions

Document lifecycle is intentionally simple:

- Draft/Uploaded -> Active
- Active -> Updated/Replaced
- Active -> Shared
- Active -> Deleted
- Deleted -> Archived (logical purge if needed by retention rules)

The training project can implement this with a soft-delete flag and event logging instead of a more elaborate workflow engine.

## Existing entity reuse

This feature reuses the current project and role model without introducing new permission types:

- `User` -> identity and role resolution
- `Project` -> project-scoped document ownership
- `ProjectMember` -> authorization for project access
- `Notification` -> notifications for share and project-upload activity

## Notes

- Integer `DocumentId` is required for consistency with the repository’s current key pattern.
- Category storage as text values is required for training simplicity and later readability.
- Storage path and file naming must be generated server-side to prevent path traversal and duplicate-key problems.
