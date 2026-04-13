using Microsoft.AspNetCore.Mvc;
using Vamserlike.Api.Dtos.Common;
using Vamserlike.Api.Repositories;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("dev-list")]
    public ActionResult<ApiResponse<object>> GetUsers()
    {
        var users = _userRepository.GetAll()
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.Nickname,
                x.CreatedAtUtc
            });

        return Ok(ApiResponse<object>.Ok(users, "개발용 유저 목록"));
    }
}