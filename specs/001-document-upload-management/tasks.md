# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the document feature foundation in the existing Blazor + EF Core application.

- [X] T001 Create the feature working folder and confirm the repository path layout for `ContosoDashboard/` and `specs/001-document-upload-management/`
- [X] T002 [P] Add the local upload storage directory convention and default directory configuration in `ContosoDashboard/appsettings.Development.json`
- [X] T003 [P] Add storage bootstrap guidance in `ContosoDashboard/Program.cs` so the application prepares a secure upload directory outside `wwwroot`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the shared document entities, storage abstraction, and access rules that all stories depend on.

**Critical**: No user story work may begin until this phase is complete.

- [X] T004 Create the base document metadata model in `ContosoDashboard/Models/Document.cs`
- [X] T005 [P] Create the `DocumentShare` model in `ContosoDashboard/Models/DocumentShare.cs`
- [X] T006 [P] Create the `DocumentActivityLog` model in `ContosoDashboard/Models/DocumentActivityLog.cs`
- [X] T007 Add `DbSet` declarations and relationship configuration for document entities in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T008 [P] Add storage abstraction contract `IFileStorageService` in `ContosoDashboard/Services/IFileStorageService.cs`
- [X] T009 [P] Implement the local file storage service `LocalFileStorageService` in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T010 Implement the base `IDocumentService` contract and document query/validation workflow in `ContosoDashboard/Services/DocumentService.cs`
- [X] T011 Add service-layer authorization checks for project access, owner access, and role-based governance in `ContosoDashboard/Services/DocumentService.cs`
- [X] T012 Add document activity and notification hooks into `ContosoDashboard/Services/NotificationService.cs` for upload/share/delete events

**Checkpoint**: Foundation ready - document uploads, permission logic, and storage can now be implemented in story phases.

---

## Phase 3: User Story 1 - Upload and classify work documents (Priority: P1) 🎯 MVP

**Goal**: Allow employees to upload supported files with the right metadata and secure storage context.

**Independent Test**: A signed-in user can select a valid file, enter the required metadata, and successfully complete an upload without exposing an unsafe file path or creating duplicate records.

### Implementation for User Story 1

- [X] T013 [P] [US1] Create the upload request and validation model in `ContosoDashboard/Models/DocumentUploadRequest.cs`
- [X] T014 [US1] Implement `UploadDocumentAsync` and validation logic in `ContosoDashboard/Services/DocumentService.cs`
- [X] T015 [US1] Add secure file writing and GUID-based naming flow through `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T016 [US1] Add document upload UI and form in `ContosoDashboard/Pages/Documents.razor`
- [X] T017 [US1] Add upload form code-behind or page logic in `ContosoDashboard/Pages/Documents.razor.cs`
- [X] T018 [US1] Add success/error messaging and upload progress behavior in `ContosoDashboard/Pages/Documents.razor`
- [X] T019 [US1] Add project-linked upload handling and metadata persistence for category, tags, and project associations in `ContosoDashboard/Services/DocumentService.cs`

**Checkpoint**: User Story 1 should be fully functional and independently testable.

---

## Phase 4: User Story 2 - Search, browse, and retrieve documents by permission (Priority: P2)

**Goal**: Provide searchable, filterable document views that respect project and role access rules.

**Independent Test**: A user can search, sort, and filter their accessible documents and retrieve a preview or download only when authorized.

### Implementation for User Story 2

- [X] T020 [P] [US2] Implement document listing and filtering queries in `ContosoDashboard/Services/DocumentService.cs`
- [X] T021 [US2] Add the My Documents and Project Documents views in `ContosoDashboard/Pages/Documents.razor`
- [X] T022 [US2] Add search, sort, and category/date filter logic in `ContosoDashboard/Pages/Documents.razor`
- [X] T023 [US2] Add preview/download access checks and safe file retrieval in `ContosoDashboard/Services/DocumentService.cs`
- [X] T024 [US2] Connect document previews and downloads to the stored local file path in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T025 [US2] Ensure unauthorized document access is blocked for non-members and restricted roles in `ContosoDashboard/Services/DocumentService.cs`

**Checkpoint**: User Stories 1 and 2 should both work independently.

---

## Phase 5: User Story 3 - Share, update, and maintain document records (Priority: P3)

**Goal**: Allow owners and managers to maintain the document lifecycle with sharing, edits, replacements, and cleanup.

**Independent Test**: A document owner can update metadata, share a document, replace a file, and delete it after confirmation while audit and notification events are recorded.

### Implementation for User Story 3

- [X] T026 [P] [US3] Add metadata update and file replacement workflow in `ContosoDashboard/Services/DocumentService.cs`
- [X] T027 [US3] Add document sharing logic and recipient notification generation in `ContosoDashboard/Services/DocumentService.cs`
- [X] T028 [US3] Add the shared-with-me document view and notification wiring in `ContosoDashboard/Pages/Documents.razor`
- [X] T029 [US3] Add deletion confirmation and physical file cleanup logic in `ContosoDashboard/Services/DocumentService.cs`
- [X] T030 [US3] Add audit log creation for uploads, downloads, sharing, replacements, and deletes in `ContosoDashboard/Models/DocumentActivityLog.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T031 [US3] Update project task or dashboard integration for document context in `ContosoDashboard/Pages/Index.razor` and `ContosoDashboard/Services/DashboardService.cs`

**Checkpoint**: All user stories should now be independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalize experience, documentation, and validation across all stories.

- [X] T032 [P] Validate the feature against the quickstart flow in `specs/001-document-upload-management/quickstart.md`
- [X] T033 [P] Add repository-level guidance and feature overview notes to `README.md`
- [X] T034 Review security and authorization edge cases across `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Pages/Documents.razor`
- [X] T035 Run the application build and smoke-check the document flow in `ContosoDashboard/Program.cs` and the relevant Razor pages
- [X] T036 Clean up code quality issues and ensure consistent naming, validation, and access rules across document-related files

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; starts immediately.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all story work.
- **User Story 1 (Phase 3)**: Depends on Foundational completion.
- **User Story 2 (Phase 4)**: Depends on Foundational completion; can begin after US1 if desired.
- **User Story 3 (Phase 5)**: Depends on Foundational completion; can begin after US1/US2 if desired.
- **Polish (Phase 6)**: Depends on all desired story work being complete.

### User Story Dependencies

- **US1**: No dependencies on other stories; this is the MVP story.
- **US2**: Depends on the foundation and may rely on the document model and upload mechanism created in US1.
- **US3**: Depends on the foundation and should align with the identity, sharing, and audit structures created by US1/US2.

### Parallel Opportunities

- Setup tasks T002 and T003 can run in parallel.
- Foundational tasks T005, T006, T008, and T009 can run in parallel when the model and storage design is stable.
- US1 tasks T013, T015, and T019 are independent by file area once the core service contract is in place.
- US2 tasks T020 and T023 are independent from the view wiring once the service contract is ready.
- Polish tasks T032 and T033 can run in parallel after the main feature is complete.

---

## Parallel Example: User Story 1

```bash
# Example parallel workstreams after foundational completion
# 1) Upload validation and business logic
#    - DocumentService upload workflow
# 2) Storage behavior
#    - LocalFileStorageService write path
# 3) UI form
#    - Documents.razor file and code-behind
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup.
2. Complete Foundational.
3. Deliver User Story 1 only.
4. Test the upload flow end-to-end and validate the file restriction rules.
5. Stop and demo the MVP before broader document features are added.

### Incremental Delivery

1. Complete Setup + Foundational.
2. Add User Story 1 with upload and metadata capture.
3. Add User Story 2 with search, browse, and access checks.
4. Add User Story 3 with sharing, lifecycle updates, and audit tracking.
5. Finish with polish, documentation, and smoke validation.

### Parallel Team Strategy

- Developer A: foundation and storage abstraction
- Developer B: document upload page and validation
- Developer C: search, browse, and access control
- Developer D: sharing, update, and audit lifecycle work

This keeps the feature independently testable while allowing parallel work on different code areas.

---

## Notes

- Tasks are organized by story so each story can be implemented and validated independently.
- The file paths call out the exact repository files most likely to change during the implementation.
- Validation is driven by the story acceptance criteria and the quickstart scenarios rather than by mock-only behavior.
