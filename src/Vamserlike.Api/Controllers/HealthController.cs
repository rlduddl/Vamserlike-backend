using Microsoft.AspNetCore.Mvc;

using Vamserlike.Api.Dtos.Common;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ILogger<HealthController> logger)
    {
        _logger = logger;
    }


    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        _logger.LogInformation("{@LogData}", new
        {
            EventName = "HealthCheck",
            Status = "ok",
            UtcNow = DateTime.UtcNow
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            Status = "ok",
            UtcNow = DateTime.UtcNow
        }, "api is running"));
    }
}