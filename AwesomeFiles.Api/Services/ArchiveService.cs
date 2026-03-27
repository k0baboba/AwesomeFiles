using System.Collections.Concurrent;
using System.IO.Compression;
using AwesomeFiles.Api.Domain;
using AwesomeFiles.Api.Options;
using Microsoft.Extensions.Options;

namespace AwesomeFiles.Api.Services;

public sealed class ArchiveService : IArchiveService
{
    private readonly IFileCatalogService _fileCatalogService;
    private readonly string _archivesDirectory;
    private readonly ConcurrentDictionary<long, ArchiveTaskModel> _tasks = new();
    private long _lastTaskId;

    public ArchiveService(
        IFileCatalogService fileCatalogService,
        IOptions<StorageOptions> storageOptions,
        IWebHostEnvironment hostEnvironment)
    {
        _fileCatalogService = fileCatalogService;

        _archivesDirectory = ResolveAbsolutePath(storageOptions.Value.ArchivesPath, hostEnvironment.ContentRootPath);
        Directory.CreateDirectory(_archivesDirectory);
    }

    public long StartArchiveTask(IReadOnlyCollection<string> fileNames)
    {
        var taskId = Interlocked.Increment(ref _lastTaskId);

        var task = new ArchiveTaskModel
        {
            TaskId = taskId,
            RequestedFiles = fileNames.ToArray(),
            Status = ArchiveTaskStatus.Queued
        };

        _tasks[taskId] = task;

        _ = Task.Run(() => ProcessArchiveTask(task));

        return taskId;
    }

    public bool TryGetTaskStatus(long taskId, out ArchiveTaskSnapshot snapshot)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            snapshot = new ArchiveTaskSnapshot();
            return false;
        }

        lock (task.SyncRoot)
        {
            snapshot = new ArchiveTaskSnapshot
            {
                TaskId = task.TaskId,
                Status = task.Status,
                Error = task.Error
            };
        }

        return true;
    }

    public ArchiveDownloadResult GetDownloadResult(long taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            return new ArchiveDownloadResult
            {
                State = ArchiveDownloadState.NotFound
            };
        }

        lock (task.SyncRoot)
        {
            if (task.Status is ArchiveTaskStatus.Queued or ArchiveTaskStatus.InProgress)
            {
                return new ArchiveDownloadResult
                {
                    State = ArchiveDownloadState.NotReady
                };
            }

            if (task.Status is ArchiveTaskStatus.Failed)
            {
                return new ArchiveDownloadResult
                {
                    State = ArchiveDownloadState.Failed,
                    Error = task.Error
                };
            }

            if (string.IsNullOrWhiteSpace(task.ArchivePath) || !File.Exists(task.ArchivePath))
            {
                return new ArchiveDownloadResult
                {
                    State = ArchiveDownloadState.Failed,
                    Error = "Archive file was not found on server."
                };
            }

            var stream = new FileStream(task.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return new ArchiveDownloadResult
            {
                State = ArchiveDownloadState.Ready,
                Stream = stream,
                FileName = Path.GetFileName(task.ArchivePath)
            };
        }
    }

    private void ProcessArchiveTask(ArchiveTaskModel task)
    {
        try
        {
            lock (task.SyncRoot)
            {
                task.Status = ArchiveTaskStatus.InProgress;
            }

            var archivePath = Path.Combine(_archivesDirectory, $"{task.TaskId}.zip");

            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            using var zipArchive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

            foreach (var requestedFile in task.RequestedFiles)
            {
                if (!_fileCatalogService.TryGetFilePath(requestedFile, out var sourcePath))
                {
                    throw new FileNotFoundException($"Source file '{requestedFile}' does not exist.", requestedFile);
                }

                zipArchive.CreateEntryFromFile(sourcePath, requestedFile, CompressionLevel.Fastest);
            }

            lock (task.SyncRoot)
            {
                task.Status = ArchiveTaskStatus.Completed;
                task.ArchivePath = archivePath;
                task.Error = null;
            }
        }
        catch (Exception ex)
        {
            lock (task.SyncRoot)
            {
                task.Status = ArchiveTaskStatus.Failed;
                task.Error = ex.Message;
            }
        }
    }

    private static string ResolveAbsolutePath(string path, string contentRootPath)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
    }

    private sealed class ArchiveTaskModel
    {
        public object SyncRoot { get; } = new();

        public long TaskId { get; init; }

        public required IReadOnlyCollection<string> RequestedFiles { get; init; }

        public ArchiveTaskStatus Status { get; set; }

        public string? ArchivePath { get; set; }

        public string? Error { get; set; }
    }
}
