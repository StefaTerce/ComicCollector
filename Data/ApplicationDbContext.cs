using ComicCollector.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ComicCollector.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Comic> Comics { get; set; }
        public DbSet<Manga> Mangas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurazioni addizionali se necessarie
            builder.Entity<Comic>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comics)
                .HasForeignKey(c => c.UserId);

            builder.Entity<Manga>()
                .HasOne(m => m.User)
                .WithMany(u => u.Mangas)
                .HasForeignKey(m => m.UserId);
        }
    }
}