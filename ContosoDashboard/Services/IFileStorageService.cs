namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativeFolderPath);
    Task DeleteAsync(string storedPath);
    Task<Stream> DownloadAsync(string storedPath);
    Task<string> GetUrlAsync(string storedPath, TimeSpan expiration);
}
