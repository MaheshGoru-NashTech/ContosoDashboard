using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoDashboard.Tests;

public class DocumentServiceTests
{
    [Fact]
    public async Task UploadDocumentAsync_AllowsValidUploadAndPersistsMetadata()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Users.Add(new User
        {
            UserId = 1,
            Email = "employee@contoso.com",
            DisplayName = "Employee User",
            Department = "Engineering",
            JobTitle = "Developer",
            Role = UserRole.Employee
        });
        await context.SaveChangesAsync();

        var storage = new FakeFileStorageService();
        var service = new DocumentService(context, storage);

        var document = await service.UploadDocumentAsync(
            1,
            "Quarterly Report",
            "Summary of the quarter",
            "Reports",
            null,
            "financial,operations",
            new MemoryStream(new byte[] { 1, 2, 3, 4 }),
            "quarterly-report.pdf",
            "application/pdf");

        Assert.NotNull(document);
        Assert.Equal("Quarterly Report", document.Title);
        Assert.Equal("Reports", document.Category);
        Assert.Equal(4, document.FileSizeBytes);
        Assert.Equal(1, document.UploadedByUserId);
        Assert.True(document.StoredFileName.Contains(".pdf", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UploadDocumentAsync_RejectsUnsupportedType()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Users.Add(new User
        {
            UserId = 1,
            Email = "employee@contoso.com",
            DisplayName = "Employee User",
            Department = "Engineering",
            JobTitle = "Developer",
            Role = UserRole.Employee
        });
        await context.SaveChangesAsync();

        var storage = new FakeFileStorageService();
        var service = new DocumentService(context, storage);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(
                1,
                "Bad file",
                null,
                "Personal Files",
                null,
                null,
                new MemoryStream(new byte[] { 1, 2, 3 }),
                "danger.exe",
                "application/octet-stream"));

        Assert.Contains("unsupported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAccessibleDocumentsAsync_FiltersBySearchAndCategory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var owner = new User { UserId = 1, Email = "employee@contoso.com", DisplayName = "Employee User", Role = UserRole.Employee };
        context.Users.Add(owner);
        context.Documents.AddRange(
            new Document { DocumentId = 1, Title = "Quarterly Report", Category = "Reports", FileName = "report.pdf", StoredFileName = "one.pdf", FilePath = "1/one.pdf", FileType = "application/pdf", FileSizeBytes = 10, UploadedByUserId = 1, UploadedBy = owner },
            new Document { DocumentId = 2, Title = "Team Plan", Category = "Team Resources", FileName = "plan.pdf", StoredFileName = "two.pdf", FilePath = "1/two.pdf", FileType = "application/pdf", FileSizeBytes = 10, UploadedByUserId = 1, UploadedBy = owner });
        await context.SaveChangesAsync();

        var service = new DocumentService(context, new FakeFileStorageService());
        var results = await service.GetAccessibleDocumentsAsync(1, searchTerm: "quarter", category: "Reports");

        var document = Assert.Single(results);
        Assert.Equal(1, document.DocumentId);
    }

    [Fact]
    public async Task ShareDocumentAsync_CreatesShareRecordAndAllowsRecipientAccess()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Users.AddRange(
            new User { UserId = 1, Email = "owner@contoso.com", DisplayName = "Owner", Role = UserRole.Employee },
            new User { UserId = 2, Email = "recipient@contoso.com", DisplayName = "Recipient", Role = UserRole.Employee });
        context.Documents.Add(new Document { DocumentId = 1, Title = "Shared Report", Category = "Reports", FileName = "report.pdf", StoredFileName = "one.pdf", FilePath = "1/one.pdf", FileType = "application/pdf", FileSizeBytes = 10, UploadedByUserId = 1 });
        await context.SaveChangesAsync();

        var service = new DocumentService(context, new FakeFileStorageService());
        Assert.True(await service.ShareDocumentAsync(1, 1, 2));

        var results = await service.GetAccessibleDocumentsAsync(2, sharedWithMe: true);
        Assert.Single(results);
        Assert.Equal(1, await context.DocumentShares.CountAsync());
    }

    [Fact]
    public async Task DeleteDocumentAsync_SoftDeletesAndRemovesStoredFile()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Users.Add(new User { UserId = 1, Email = "owner@contoso.com", DisplayName = "Owner", Role = UserRole.Employee });
        context.Documents.Add(new Document { DocumentId = 1, Title = "Report", Category = "Reports", FileName = "report.pdf", StoredFileName = "one.pdf", FilePath = "1/one.pdf", FileType = "application/pdf", FileSizeBytes = 10, UploadedByUserId = 1 });
        await context.SaveChangesAsync();

        var storage = new FakeFileStorageService();
        var service = new DocumentService(context, storage);
        Assert.True(await service.DeleteDocumentAsync(1, 1));

        Assert.True(await context.Documents.Where(d => d.DocumentId == 1).Select(d => d.IsDeleted).SingleAsync());
        Assert.Contains("1/one.pdf", storage.DeletedPaths);
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public List<string> DeletedPaths { get; } = new();

        public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativeFolderPath)
        {
            return Task.FromResult($"/uploads/{relativeFolderPath}/{fileName}");
        }

        public Task DeleteAsync(string storedPath)
        {
            DeletedPaths.Add(storedPath);
            return Task.CompletedTask;
        }

        public Task<Stream> DownloadAsync(string storedPath)
        {
            return Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
        }

        public Task<string> GetUrlAsync(string storedPath, TimeSpan expiration)
        {
            return Task.FromResult(storedPath);
        }
    }
}
