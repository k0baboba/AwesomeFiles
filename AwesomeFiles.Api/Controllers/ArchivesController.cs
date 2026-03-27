using AwesomeFiles.Api.Contracts;
using AwesomeFiles.Api.Domain;
using AwesomeFiles.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AwesomeFiles.Api.Controllers;

[ApiController]
[Route("api/archives")]
public sealed class ArchivesController : ControllerBase
{
    private readonly IArchiveService _archiveService;
    private readonly IFileCatalogService _fileCatalogService;

    public ArchivesController(IArchiveService archiveService, IFileCatalogService fileCatalogService)
    {
        _archiveService = archiveService;
        _fileCatalogService = fileCatalogService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateArchiveResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<CreateArchiveResponse> CreateArchiveTask([FromBody] CreateArchiveRequest request)
    {
        if (request.FileNames.Count == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.FileNames)] = ["At least one file name must be provided."]
            }));
        }

        var normalizedFileNames = request.FileNames
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => fileName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var missingFiles = _fileCatalogService.GetMissingFiles(normalizedFileNames);
        if (missingFiles.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.FileNames)] = ["Unknown files: " + string.Join(", ", missingFiles)]
            }));
        }

        var taskId = _archiveService.StartArchiveTask(normalizedFileNames);

        return Accepted(new CreateArchiveResponse { TaskId = taskId });
    }

    [HttpGet("{taskId:long}/status")]
    [ProducesResponseType(typeof(ArchiveStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ArchiveStatusResponse> GetArchiveTaskStatus(long taskId)
    {
        if (!_archiveService.TryGetTaskStatus(taskId, out var status))
        {
            return NotFound($"Task with id '{taskId}' was not found.");
        }

        return Ok(new ArchiveStatusResponse
        {
            TaskId = status.TaskId,
            Status = status.Status,
            Error = status.Error
        });
    }

    [HttpGet("{taskId:long}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult DownloadArchive(long taskId)
    {
        var result = _archiveService.GetDownloadResult(taskId);

        return result.State switch
        {
            ArchiveDownloadState.NotFound => NotFound($"Task with id '{taskId}' was not found."),
            ArchiveDownloadState.NotReady => Conflict("Archive is not ready yet."),
            ArchiveDownloadState.Failed => Conflict(result.Error ?? "Archive task failed."),
            ArchiveDownloadState.Ready => File(result.Stream!, "application/zip", result.FileName),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
