using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mindflow_backend.WellnessEngine.Application.Services;

using Mindflow_backend.Shared.Infrastructure.Security;
namespace Mindflow_backend.WellnessEngine.Interfaces.Rest;

[ApiController]
[Route("api/v1/wellness")]
[Authorize]
public sealed class WellnessController(IWellnessService wellnessService) : ControllerBase
{
    [HttpPost("stress-check")]
    public async Task<IActionResult> StressCheck(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await wellnessService.RunStressCheckAsync(userId, ct);
        return Ok(result);
    }
}
