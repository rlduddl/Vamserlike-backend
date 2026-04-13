using Vamserlike.Api.Models;

namespace Vamserlike.Api.Repositories;

public interface IUserRepository
{
    bool ExistsByEmail(string email);
    User? GetByEmail(string email);
    IEnumerable<User> GetAll();
    void Add(User user);
}