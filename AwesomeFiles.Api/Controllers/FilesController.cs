using AwesomeFiles.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AwesomeFiles.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileCatalogService _fileCatalogService;

    public FilesController(IFileCatalogService fileCatalogService)
    {
        _fileCatalogService = fileCatalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<string>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<string>> GetAllFiles()
    {
        return Ok(_fileCatalogService.GetAllFileNames());
    }
}
