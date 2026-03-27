namespace AwesomeFiles.Client.Models;

public sealed class CreateArchiveRequest
{
    public required IReadOnlyCollection<string> FileNames { get; init; }
}
