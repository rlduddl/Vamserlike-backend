using Microsoft.AspNetCore.Mvc;
using Vamserlike.Api.Dtos.Common;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            status = "ok",
            serverTimeUtc = DateTime.UtcNow
        }, "서버 정상"));
    }
}