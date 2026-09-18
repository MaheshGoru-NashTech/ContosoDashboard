using System.Security.Cryptography;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredRoot = configuration["FileStorage:UploadRoot"] ?? Path.Combine("AppData", "uploads");
        var appDataRoot = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot);
        Directory.CreateDirectory(appDataRoot);
        _rootPath = appDataRoot;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativeFolderPath)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));

        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName);
        var uniqueName = $"{Guid.NewGuid():N}{extension}";

        var fullFolderPath = Path.Combine(_rootPath, relativeFolderPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(fullFolderPath);

        var fullFilePath = Path.Combine(fullFolderPath, uniqueName);
        await using var output = File.Create(fullFilePath);
        await fileStream.CopyToAsync(output);

        return Path.Combine(relativeFolderPath, uniqueName).Replace('\\', '/');
    }

    public Task DeleteAsync(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return Task.CompletedTask;

        var fullPath = Path.Combine(_rootPath, storedPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) throw new ArgumentException("Stored path is required.", nameof(storedPath));

        var fullPath = Path.Combine(_rootPath, storedPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Document file was not found.", storedPath);

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task<string> GetUrlAsync(string storedPath, TimeSpan expiration)
    {
        return Task.FromResult($"/documents/download?path={Uri.EscapeDataString(storedPath)}");
    }
}
