namespace AwesomeFiles.Api.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string FilesPath { get; set; } = "Data/Files";

    public string ArchivesPath { get; set; } = "Data/Archives";
}
