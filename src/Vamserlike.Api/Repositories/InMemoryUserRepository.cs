using System.Collections.Concurrent;
using Vamserlike.Api.Models;

namespace Vamserlike.Api.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new();

    public bool ExistsByEmail(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        return _users.ContainsKey(normalizedEmail);
    }

    public User? GetByEmail(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        return _users.TryGetValue(normalizedEmail, out var user) ? user : null;
    }

    public IEnumerable<User> GetAll()
    {
        return _users.Values.OrderBy(x => x.CreatedAtUtc);
    }

    public void Add(User user)
    {
        var normalizedEmail = NormalizeEmail(user.Email);

        if (!_users.TryAdd(normalizedEmail, user))
        {
            throw new InvalidOperationException("이미 가입된 이메일입니다.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}