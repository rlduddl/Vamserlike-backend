using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Vamserlike.Api.Configurations;
using Vamserlike.Api.Dtos.Common;
using Vamserlike.Api.Repositories;
using Vamserlike.Api.Services;

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
        _logger.LogInformation("HealthCheck Ok.....");

        return Ok(ApiResponse<object>.Ok(new
        {
            Status = "ok",
            UtcNow = DateTime.UtcNow
        }, "api is running"));
    }
}