using AwesomeFiles.Api.Options;
using Microsoft.Extensions.Options;

namespace AwesomeFiles.Api.Services;

public sealed class FileCatalogService : IFileCatalogService
{
    private readonly string _filesDirectory;

    public FileCatalogService(IOptions<StorageOptions> storageOptions, IWebHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(storageOptions);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        _filesDirectory = ResolveAbsolutePath(storageOptions.Value.FilesPath, hostEnvironment.ContentRootPath);
        Directory.CreateDirectory(_filesDirectory);
    }

    public IReadOnlyCollection<string> GetAllFileNames()
    {
        return Directory
            .EnumerateFiles(_filesDirectory, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static fileName => !string.IsNullOrWhiteSpace(fileName))
            .Cast<string>()
            .OrderBy(static fileName => fileName)
            .ToArray();
    }

    public bool TryGetFilePath(string fileName, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var candidatePath = Path.Combine(_filesDirectory, fileName);

        if (!File.Exists(candidatePath))
        {
            return false;
        }

        fullPath = candidatePath;
        return true;
    }

    public IReadOnlyCollection<string> GetMissingFiles(IEnumerable<string> fileNames)
    {
        ArgumentNullException.ThrowIfNull(fileNames);

        var existing = new HashSet<string>(GetAllFileNames(), StringComparer.OrdinalIgnoreCase);

        return fileNames
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(fileName => !existing.Contains(fileName))
            .ToArray();
    }

    private static string ResolveAbsolutePath(string path, string contentRootPath)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
    }
}
