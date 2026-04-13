using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Models;
using Vamserlike.Api.Repositories;
using Vamserlike.Api.Utilities;

namespace Vamserlike.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public AuthResponse Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Nickname))
        {
            throw new ArgumentException("이메일, 비밀번호, 닉네임은 필수입니다.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (_userRepository.ExistsByEmail(email))
        {
            throw new InvalidOperationException("이미 가입된 이메일입니다.");
        }

        var user = new User
        {
            Email = email,
            Nickname = request.Nickname.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password)
        };

        _userRepository.Add(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Nickname = user.Nickname,
            AccessToken = "TODO-JWT"
        };
    }

    public AuthResponse Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("이메일과 비밀번호는 필수입니다.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = _userRepository.GetByEmail(email);

        if (user is null)
        {
            throw new UnauthorizedAccessException("이메일 또는 비밀번호가 올바르지 않습니다.");
        }

        var isValid = PasswordHasher.Verify(request.Password, user.PasswordHash);

        if (!isValid)
        {
            throw new UnauthorizedAccessException("이메일 또는 비밀번호가 올바르지 않습니다.");
        }

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Nickname = user.Nickname,
            AccessToken = "TODO-JWT"
        };
    }
}