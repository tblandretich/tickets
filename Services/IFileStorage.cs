namespace TicketsAndretich.Web.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(string relativeFolder, string fileName, Stream stream, CancellationToken ct = default);
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
