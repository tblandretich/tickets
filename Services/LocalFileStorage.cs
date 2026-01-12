namespace TicketsAndretich.Web.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    public LocalFileStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(string relativeFolder, string fileName, Stream stream, CancellationToken ct = default)
    {
        var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var folder = Path.Combine(_root, relativeFolder);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}");

        using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fs, ct);
        }
        return path;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        Stream s = File.OpenRead(storagePath);
        return Task.FromResult(s);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        if (File.Exists(storagePath)) File.Delete(storagePath);
        return Task.CompletedTask;
    }
}
