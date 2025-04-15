using ComicCollector.Models;
using Microsoft.EntityFrameworkCore;

namespace ComicCollector.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Manga> Manga { get; set; }
    }
}