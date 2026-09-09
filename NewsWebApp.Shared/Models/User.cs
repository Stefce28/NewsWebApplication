using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;

    public List<Article> Posts { get; set; } = new();

    // Needed by EF / Identity
    public User()
    {
        Id = Guid.NewGuid();
    }

    public User(
        string name,
        string surname,
        string email)
    {
        Id = Guid.NewGuid();

        Name = name;
        Surname = surname;

        Email = email;
        UserName = email;
    }
}