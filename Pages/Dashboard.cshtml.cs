using ComicCollector.Data;
using ComicCollector.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ComicCollector.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Manga> MangaList { get; set; }

        public void OnGet()
        {
            // Simulating API fetch and saving to database
            if (!_context.Manga.Any())
            {
                var mangaFromApi = new List<Manga> // This data would come from an actual API
                {
                    new Manga { Title = "Naruto", Description = "A ninja story", ImageUrl = "https://example.com/naruto.jpg" },
                    new Manga { Title = "One Piece", Description = "Pirate adventures", ImageUrl = "https://example.com/onepiece.jpg" }
                };

                _context.Manga.AddRange(mangaFromApi);
                _context.SaveChanges();
            }

            MangaList = _context.Manga.ToList();
        }
    }
}