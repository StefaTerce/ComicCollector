using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ComicCollector.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        // Navigazione
        public virtual ICollection<Comic> Comics { get; set; } = new List<Comic>();
        public virtual ICollection<Manga> Mangas { get; set; } = new List<Manga>();
    }
}