using NewsWebApp.Models;

public interface IUserRepository
{
    Task<List<User>> ListAllUsers();
    Task<User?> GetUserById(Guid id);
}