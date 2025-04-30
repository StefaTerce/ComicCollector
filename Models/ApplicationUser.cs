using Microsoft.AspNetCore.Identity;
using System;

namespace ComicCollector.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Proprietà personalizzate per l'utente
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Rimuovi queste proprietà che causano il problema
        // public DateTime? CreatedDate { get; set; } = DateTime.Now;
        // public DateTime? LastLoginDate { get; set; }
    }
}