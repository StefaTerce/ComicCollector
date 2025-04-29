using Microsoft.AspNetCore.Identity;

namespace ComicCollector.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Add any additional user properties you might need here
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}