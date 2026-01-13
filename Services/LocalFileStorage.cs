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
        
        // Generar nombre único con timestamp
        var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}";
        var fullPath = Path.Combine(folder, uniqueFileName);

        using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fs, ct);
        }
        
        // Guardar ruta RELATIVA (desde la carpeta uploads)
        return Path.Combine(relativeFolder, uniqueFileName);
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        // Si la ruta es absoluta (legado), usarla directamente
        // Si es relativa, resolver desde _root
        string fullPath;
        if (Path.IsPathRooted(storagePath))
        {
            fullPath = storagePath;
        }
        else
        {
            fullPath = Path.Combine(_root, storagePath);
        }
        
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }
        
        Stream s = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(s);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        string fullPath = Path.IsPathRooted(storagePath) 
            ? storagePath 
            : Path.Combine(_root, storagePath);
            
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
