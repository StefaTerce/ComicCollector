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
        public DbSet<Manga> Manga { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("YourConnectionStringHere",
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5, // Numero massimo di tentativi
                            maxRetryDelay: TimeSpan.FromSeconds(10), // Ritardo massimo
                            errorNumbersToAdd: null); // Specificare codici di errore aggiuntivi, se necessario
                    });
            }
        }
    }
}