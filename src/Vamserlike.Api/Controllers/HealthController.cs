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
        // 이렇게 JSON처럼 보이게 구조를 잡고, 아테나가 읽을 키(Key)를 넣어줘!
        _logger.LogInformation("LogEvent: {EventName}, Status: {Status}, UtcNow: {UtcNow}",
            "HealthCheck",
            "ok",
            DateTime.UtcNow);

        return Ok(ApiResponse<object>.Ok(new
        {
            Status = "ok",
            UtcNow = DateTime.UtcNow
        }, "api is running"));
    }
}