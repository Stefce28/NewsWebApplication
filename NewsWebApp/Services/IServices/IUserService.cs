using NewsWebApp.Shared.Models;

public interface IUserService
{
    Task<List<User>> ListAllUsers();

    Task<User?> GetUserById(Guid id);

    Task<User?> UpdateUser(
        Guid id,
        string name,
        string surname,
        string email
    );

    Task<bool> DeleteUser(Guid id);
}