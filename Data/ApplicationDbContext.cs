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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configura la relazione tra Comic e ApplicationUser
            builder.Entity<Comic>(entity =>
            {
                entity.HasOne(c => c.User)
                    .WithMany() // Se ApplicationUser non ha una navigation property esplicita List<Comic>
                    .HasForeignKey(c => c.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade); // Se un utente viene eliminato, elimina i suoi fumetti

                // Indice per migliorare le query sulla collezione di un utente
                // e per aiutare a prevenire duplicati se SourceId è noto
                entity.HasIndex(c => new { c.UserId, c.Source, c.SourceId })
                      .IsUnique(false); // Impostare a true se la combinazione DEVE essere unica.
                                        // Per ora false, la logica di "già in collezione" sarà nell'handler.

                entity.Property(c => c.IssueNumber).IsRequired(false);
                entity.Property(c => c.PageCount).IsRequired(false);
                entity.Property(c => c.Publisher).IsRequired(false);
                entity.Property(c => c.CoverImage).IsRequired(false); // Ensure CoverImage can be null
                entity.Property(c => c.Description).HasMaxLength(2000).IsRequired(false); // Ensure Description can be null
                entity.Property(c => c.Notes).HasMaxLength(1000).IsRequired(false);       // Ensure Notes can be null
                entity.Property(c => c.Source).HasMaxLength(50);
                entity.Property(c => c.SourceId).IsRequired(false); // SourceId può essere null per aggiunte manuali ("Local")
            });
        }
    }
}