using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NewsWebApp.Shared.Models;

namespace NewsWebApp.Data;

public class ApplicationDbContext
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Article> Articles { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Article>()
            .Property(a => a.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<Article>()
            .Property(a => a.ShockValue)
            .HasDefaultValue(0m);

        builder.Entity<Article>()
            .HasOne(a => a.Author)
            .WithMany(u => u.Posts);

        builder.Entity<Article>()
            .HasOne(a => a.Category)
            .WithMany(c => c.Articles);
    }
}
