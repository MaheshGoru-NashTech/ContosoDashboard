using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(int requestingUserId, string title, string? description, string category, int? projectId, string? tags, Stream fileStream, string fileName, string contentType);
    Task<List<Document>> GetAccessibleDocumentsAsync(int requestingUserId, int? projectId = null, string? searchTerm = null, string? category = null, DateTime? fromUtc = null, DateTime? toUtc = null, bool sharedWithMe = false);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId);
    Task<Document> UpdateDocumentAsync(int documentId, int requestingUserId, string title, string? description, string category, int? projectId, string? tags);
    Task<Document> ReplaceDocumentAsync(int documentId, int requestingUserId, Stream fileStream, string fileName, string contentType);
    Task<bool> ShareDocumentAsync(int documentId, int requestingUserId, int recipientUserId);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
    Task<Stream> DownloadDocumentAsync(int documentId, int requestingUserId);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService? _notificationService;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public DocumentService(ApplicationDbContext context, IFileStorageService fileStorageService, INotificationService? notificationService = null)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    public async Task<Document> UploadDocumentAsync(int requestingUserId, string title, string? description, string category, int? projectId, string? tags, Stream fileStream, string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new InvalidOperationException("Document title is required.");
        if (string.IsNullOrWhiteSpace(category)) throw new InvalidOperationException("Document category is required.");
        if (fileStream == null || fileStream.Length == 0) throw new InvalidOperationException("File is required.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type. Allowed types are PDF, Office documents, text files, and images.");
        }

        if (fileStream.Length > 25 * 1024 * 1024)
        {
            throw new InvalidOperationException("File exceeds the 25 MB limit.");
        }

        var currentUser = await _context.Users.FindAsync(requestingUserId);
        if (currentUser == null) throw new InvalidOperationException("User not found.");

        if (projectId.HasValue)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId.Value);

            if (project == null)
            {
                throw new InvalidOperationException("Project not found.");
            }

            var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == requestingUserId) || project.ProjectManagerId == requestingUserId;
            if (!isProjectMember)
            {
                throw new InvalidOperationException("You do not have access to upload documents for this project.");
            }
        }

        var relativeFolderPath = projectId.HasValue
            ? $"{requestingUserId}/projects/{projectId.Value}"
            : $"{requestingUserId}/personal";

        var storagePath = await _fileStorageService.UploadAsync(fileStream, fileName, contentType, relativeFolderPath);

        var document = new Document
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            Category = category.Trim(),
            FileName = fileName,
            StoredFileName = Path.GetFileName(storagePath),
            FilePath = storagePath,
            FileType = contentType,
            FileSizeBytes = fileStream.Length,
            Tags = tags?.Trim(),
            UploadedByUserId = requestingUserId,
            ProjectId = projectId,
            UploadedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Documents.Add(document);
        _context.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            Document = document,
            UserId = requestingUserId,
            ActivityType = "Upload",
            Details = $"Uploaded {fileName}"
        });
        await _context.SaveChangesAsync();

        if (_notificationService != null && projectId.HasValue)
        {
            var recipients = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId.Value && pm.UserId != requestingUserId)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var recipientId in recipients)
            {
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = recipientId,
                    Title = "New project document",
                    Message = $"{currentUser.DisplayName} uploaded '{document.Title}'.",
                    Type = NotificationType.DocumentUpdated,
                    Priority = NotificationPriority.Informational
                });
            }
        }

        return document;
    }

    public async Task<List<Document>> GetAccessibleDocumentsAsync(int requestingUserId, int? projectId = null, string? searchTerm = null, string? category = null, DateTime? fromUtc = null, DateTime? toUtc = null, bool sharedWithMe = false)
    {
        var query = _context.Documents
            .Where(d => !d.IsDeleted)
            .Include(d => d.UploadedBy)
            .Include(d => d.Project)
            .AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }
        else
        {
            query = query.Where(d => d.UploadedByUserId == requestingUserId ||
                d.ProjectId != null && (d.Project!.ProjectMembers.Any(pm => pm.UserId == requestingUserId) || d.Project.ProjectManagerId == requestingUserId) ||
                d.Shares.Any(s => s.UserId == requestingUserId));
        }

        if (sharedWithMe)
        {
            query = query.Where(d => d.Shares.Any(s => s.UserId == requestingUserId));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term) ||
                (d.Description != null && d.Description.ToLower().Contains(term)) ||
                (d.Tags != null && d.Tags.ToLower().Contains(term)) ||
                d.FileName.ToLower().Contains(term) ||
                (d.UploadedBy != null && d.UploadedBy.DisplayName.ToLower().Contains(term)) ||
                (d.Project != null && d.Project.Name.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(d => d.Category == category);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(d => d.UploadedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(d => d.UploadedAtUtc < toUtc.Value.AddDays(1));
        }

        return await query
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.UploadedBy)
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null) return null;

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectManager = document.Project?.ProjectManagerId == requestingUserId;
        var isProjectMember = document.Project?.ProjectMembers.Any(pm => pm.UserId == requestingUserId) ?? false;

        if (!isOwner && !isProjectManager && !isProjectMember)
        {
            return null;
        }

        return document;
    }

    public async Task<Document> UpdateDocumentAsync(int documentId, int requestingUserId, string title, string? description, string category, int? projectId, string? tags)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null) throw new InvalidOperationException("Document not found.");

        if (document.UploadedByUserId != requestingUserId)
        {
            throw new InvalidOperationException("Only the document owner can edit the document.");
        }

        document.Title = title.Trim();
        document.Description = description?.Trim();
        document.Category = category.Trim();
        document.Tags = tags?.Trim();
        document.ProjectId = projectId;
        document.UpdatedAtUtc = DateTime.UtcNow;

        _context.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            ActivityType = "Update",
            Details = "Document metadata updated."
        });

        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<Document> ReplaceDocumentAsync(int documentId, int requestingUserId, Stream fileStream, string fileName, string contentType)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);
        if (document == null) throw new InvalidOperationException("Document not found.");
        if (document.UploadedByUserId != requestingUserId) throw new InvalidOperationException("Only the document owner can replace the file.");
        ValidateFile(fileStream, fileName);

        var oldPath = document.FilePath;
        var relativeFolder = Path.GetDirectoryName(oldPath)?.Replace('\\', '/') ?? $"{requestingUserId}/personal";
        var newPath = await _fileStorageService.UploadAsync(fileStream, fileName, contentType, relativeFolder);
        document.FileName = fileName;
        document.StoredFileName = Path.GetFileName(newPath);
        document.FilePath = newPath;
        document.FileType = contentType;
        document.FileSizeBytes = fileStream.Length;
        document.UpdatedAtUtc = DateTime.UtcNow;

        _context.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            ActivityType = "Replace",
            Details = $"Replaced file with {fileName}."
        });
        await _context.SaveChangesAsync();
        await _fileStorageService.DeleteAsync(oldPath);
        return document;
    }

    public async Task<bool> ShareDocumentAsync(int documentId, int requestingUserId, int recipientUserId)
    {
        var document = await GetDocumentByIdAsync(documentId, requestingUserId);
        if (document == null || document.UploadedByUserId != requestingUserId)
        {
            return false;
        }

        var recipient = await _context.Users.FindAsync(recipientUserId);
        if (recipient == null || recipientUserId == requestingUserId)
        {
            return false;
        }

        if (!await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.UserId == recipientUserId))
        {
            _context.DocumentShares.Add(new DocumentShare
            {
                DocumentId = documentId,
                UserId = recipientUserId,
                SharedByUserId = requestingUserId,
                SharedAtUtc = DateTime.UtcNow
            });
            _context.DocumentActivityLogs.Add(new DocumentActivityLog
            {
                DocumentId = documentId,
                UserId = requestingUserId,
                ActivityType = "Share",
                Details = $"Shared with {recipient.DisplayName}."
            });
            await _context.SaveChangesAsync();
        }

        if (_notificationService != null)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = recipientUserId,
                Title = "Document shared with you",
                Message = $"'{document.Title}' is now available in Shared with me.",
                Type = NotificationType.DocumentShared,
                Priority = NotificationPriority.Important
            });
        }

        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);
        if (document == null) return false;

        var project = document.ProjectId.HasValue
            ? await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == document.ProjectId.Value)
            : null;

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectManager = project != null && project.ProjectManagerId == requestingUserId;

        if (!isOwner && !isProjectManager)
        {
            return false;
        }

        document.IsDeleted = true;
        document.UpdatedAtUtc = DateTime.UtcNow;

        _context.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            ActivityType = "Delete",
            Details = "Document deleted."
        });

        await _fileStorageService.DeleteAsync(document.FilePath);
        await _context.SaveChangesAsync();
        if (_notificationService != null && project != null)
        {
            var recipients = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.ProjectId && pm.UserId != requestingUserId)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();
            foreach (var recipientId in recipients)
            {
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = recipientId,
                    Title = "Project document deleted",
                    Message = $"A project document was deleted.",
                    Type = NotificationType.DocumentUpdated,
                    Priority = NotificationPriority.Informational
                });
            }
        }
        return true;
    }

    public async Task<Stream> DownloadDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await GetDocumentByIdAsync(documentId, requestingUserId);
        if (document == null) throw new InvalidOperationException("Document not found or access denied.");

        if (string.IsNullOrWhiteSpace(document.FilePath))
        {
            throw new InvalidOperationException("Document file path is missing.");
        }

        _context.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            ActivityType = "Download",
            Details = "Document downloaded."
        });
        await _context.SaveChangesAsync();
        return await _fileStorageService.DownloadAsync(document.FilePath);
    }

    private static void ValidateFile(Stream fileStream, string fileName)
    {
        if (fileStream == null || fileStream.Length == 0) throw new InvalidOperationException("File is required.");
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension)) throw new InvalidOperationException("Unsupported file type.");
        if (fileStream.Length > 25 * 1024 * 1024) throw new InvalidOperationException("File exceeds the 25 MB limit.");
    }
}
