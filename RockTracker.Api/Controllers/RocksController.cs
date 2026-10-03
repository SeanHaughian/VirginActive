using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RockTracker.Api.Common;
using RockTracker.Api.Models;
using RockTracker.Api.Services;

namespace RockTracker.Api.Controllers;

[ApiController]
[Route("members/{memberId}/rocks")]
public class RocksController : ControllerBase
{
    private readonly IRocksService _rocksService;

    public RocksController(IRocksService rocksService)
    {
        ArgumentNullException.ThrowIfNull(rocksService);
        _rocksService = rocksService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRock([FromRoute] string memberId, [FromBody] CreateRockRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _rocksService.CreateRockAsync(memberId, request, cancellationToken);
        if (!result.IsSuccess)
            return result.ToActionResult(HttpContext?.Request?.Path);

        return CreatedAtAction(nameof(GetRocks), new { memberId }, result.Rock);
    }

    [HttpGet]
    public IActionResult GetRocks([FromRoute] string memberId, [FromQuery] RockStatus? status)
    {
        var items = _rocksService.GetRocks(memberId, status);
        return Ok(items);
    }

    [HttpPatch("{rockId:guid}")]
    public IActionResult UpdateStatus([FromRoute] string memberId, [FromRoute] Guid rockId, [FromBody] UpdateRockStatusRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = _rocksService.UpdateStatus(memberId, rockId, request);
        if (!result.IsSuccess)
            return result.ToActionResult(HttpContext?.Request?.Path);

        return Ok(result.Rock);
    }
}
