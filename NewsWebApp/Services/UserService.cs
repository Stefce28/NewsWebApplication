using Microsoft.AspNetCore.Identity;
using NewsWebApp.Shared.Models;
using NewsWebApp.Repository;
using NewsWebApp.Services.IServices;

namespace NewsWebApp.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly UserManager<User> _userManager;

    public UserService(
        IUserRepository repository,
        UserManager<User> userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    public async Task<List<User>> ListAllUsers()
    {
        return await _repository.ListAllUsers();
    }

    public async Task<User?> GetUserById(Guid id)
    {
        return await _repository.GetUserById(id);
    }

    public async Task<User?> UpdateUser(
        Guid id,
        string name,
        string surname,
        string email)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return null;

        user.Name = name;
        user.Surname = surname;
        user.Email = email;
        user.UserName = email;

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return null;

        return user;
    }

    public async Task<bool> DeleteUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return false;

        var result =
            await _userManager.DeleteAsync(user);

        return result.Succeeded;
    }
}