namespace AwesomeFiles.Api.Contracts;

public sealed class CreateArchiveRequest
{
    public IReadOnlyCollection<string> FileNames { get; init; } = [];
}
