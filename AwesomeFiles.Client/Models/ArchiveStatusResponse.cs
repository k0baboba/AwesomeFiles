namespace AwesomeFiles.Client.Models;

public sealed class ArchiveStatusResponse
{
    public long TaskId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? Error { get; init; }
}
