using Microsoft.EntityFrameworkCore;
using NewsWebApp.Data;
using NewsWebApp.Models;

namespace NewsWebApp.Repository;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> ListAllUsers()
    {
        return await _context.Users
            .Include(u => u.Posts)
            .ToListAsync();
    }

    public async Task<User?> GetUserById(Guid id)
    {
        return await _context.Users
            .Include(u => u.Posts)
            .ThenInclude(a => a.Category)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
