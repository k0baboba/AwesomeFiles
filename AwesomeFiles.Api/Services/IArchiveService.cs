using AwesomeFiles.Api.Domain;

namespace AwesomeFiles.Api.Services;

public interface IArchiveService
{
    long StartArchiveTask(IReadOnlyCollection<string> fileNames);

    bool TryGetTaskStatus(long taskId, out ArchiveTaskSnapshot snapshot);

    ArchiveDownloadResult GetDownloadResult(long taskId);
}

public sealed class ArchiveTaskSnapshot
{
    public long TaskId { get; init; }

    public ArchiveTaskStatus Status { get; init; }

    public string? Error { get; init; }
}

public sealed class ArchiveDownloadResult
{
    public required ArchiveDownloadState State { get; init; }

    public FileStream? Stream { get; init; }

    public string? FileName { get; init; }

    public string? Error { get; init; }
}

public enum ArchiveDownloadState
{
    NotFound,
    NotReady,
    Failed,
    Ready
}
