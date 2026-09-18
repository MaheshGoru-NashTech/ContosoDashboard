# Quickstart: Document Upload and Management Validation

## Prerequisites

- .NET 10 SDK or compatible local environment
- Repository checked out locally
- Application running in development mode
- A valid mock login user available in the seeded data set

## Run the app

1. Navigate to the project folder: `cd ContosoDashboard`
2. Start the application: `dotnet run`
3. Open the login page and sign in as one of the seeded users.

## Validation scenarios

### 1. Upload a valid document

- Sign in as a valid employee or manager.
- Navigate to the document upload page or the project context where upload is available.
- Choose a valid file such as a PDF or Word document under 25 MB.
- Enter a title, category, and optional project association.
- Submit the upload.

Expected outcome:
- The upload succeeds and the document appears in the user’s document list.
- Metadata includes uploader, category, project link, and file size.

### 2. Upload a rejected document

- Attempt to upload an unsupported file or a document above 25 MB.

Expected outcome:
- The request is rejected with a clear validation message.
- No unsafe file is stored and no duplicate metadata row is created.

### 3. Search and filter

- Upload or locate multiple documents across categories and projects.
- Apply search by title, description, tags, uploader, or project name.
- Filter by category or date range.

Expected outcome:
- Only documents the current user is allowed to access are returned.
- Results update correctly and quickly for a small document set.

### 4. Project access and authorization

- Sign in as an employee who belongs to a project.
- Open the project document view.
- Try to access a document belonging to another project or a document outside the user’s permissions.

Expected outcome:
- Only documents for the current project or otherwise authorized documents appear.
- Unauthorized access gets blocked by the authorization flow.

### 5. Share and notification flow

- Share a document with another user who has access to the dashboard.
- Confirm the recipient receives an in-app notification.

Expected outcome:
- The recipient sees the shared document in the correct shared view.
- The notification is created and remains visible until marked read.

### 6. Metadata update and file replacement

- Edit a document’s title or category.
- Re-upload a replacement document in the same record context.

Expected outcome:
- Metadata updates persist.
- Replacement file processing preserves the document identity with a valid audit trail.

## Expected results

The feature is considered ready when the following are true:
- Upload succeeds for supported files under the size limit.
- Validation rejects invalid uploads with clear guidance.
- Users can browse and search document records by permission.
- Project-scoped visibility behaves correctly.
- Sharing and audit notifications function as expected.
