# Document Storage Contract

## Purpose

This contract defines the internal storage behavior required for the document upload feature and forms the foundation for the `IFileStorageService` abstraction.

## Contract: IFileStorageService

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativeFolderPath);
    Task DeleteAsync(string storedPath);
    Task<Stream> DownloadAsync(string storedPath);
    Task<string> GetUrlAsync(string storedPath, TimeSpan expiration);
}
```

## Required behaviors

### UploadAsync
- Must generate or accept a safe, unique storage path.
- Must validate the file before writing.
- Must return a storage identifier that can be persisted in the database without exposing user-controlled path values.
- Must fail cleanly if the target directory cannot be created or the file cannot be saved.

### DeleteAsync
- Must remove the stored file if it exists.
- Must not leave partial or orphaned file records behind.
- Must operate safely when the file is already missing.

### DownloadAsync
- Must return a readable stream for an existing stored file.
- Must not allow unauthorized download requests to bypass file-level checks.

### GetUrlAsync
- Must provide a signed or generated URL when a preview or external access pattern is needed.
- For the local offline implementation, this may return a relative path or a local endpoint route.

## Sequence contract

1. Validate user authorization.
2. Validate file restrictions and extension.
3. Generate unique file name and storage folder.
4. Persist metadata record after successful disk write.
5. Log activity and notify relevant users.
6. On failure, roll back or leave no partial record.

## Error contract

- Unsupported file type -> validation error, no file write
- File too large -> validation error, no file write
- Storage write failure -> do not create a document record
- Unauthorized download request -> deny access, log activity

## Notes

This contract is intentionally architecture-friendly and matches the repository’s offline-first demo pattern. A future `AzureBlobStorageService` can implement the same interface without changing the document feature’s business logic.
