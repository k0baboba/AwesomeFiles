using AwesomeFiles.Api.Domain;

namespace AwesomeFiles.Api.Contracts;

public sealed class ArchiveStatusResponse
{
    public long TaskId { get; init; }

    public ArchiveTaskStatus Status { get; init; }

    public string? Error { get; init; }
}
