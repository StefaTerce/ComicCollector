using System.Collections.Generic;
using ComicCollector.Models;

namespace ComicCollector.ViewModels
{
    public class UserDetailViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Roles { get; set; }
        public List<Comic> Comics { get; set; } = new List<Comic>();
        public List<Manga> Mangas { get; set; } = new List<Manga>();
    }
}