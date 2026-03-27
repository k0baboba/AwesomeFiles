namespace AwesomeFiles.Api.Services;

public interface IFileCatalogService
{
    IReadOnlyCollection<string> GetAllFileNames();

    bool TryGetFilePath(string fileName, out string fullPath);

    IReadOnlyCollection<string> GetMissingFiles(IEnumerable<string> fileNames);
}
