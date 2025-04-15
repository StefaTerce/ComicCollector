using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ComicCollector.Data;
using ComicCollector.Models;
using Microsoft.EntityFrameworkCore;

namespace ComicCollector.Pages
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public dynamic Item { get; set; }
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Type { get; set; } = "comic";

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Type.ToLower() == "comic")
            {
                Item = await _context.Comics.FirstOrDefaultAsync(c => c.Id == Id);
            }
            else if (Type.ToLower() == "manga")
            {
                Item = await _context.Mangas.FirstOrDefaultAsync(m => m.Id == Id);
            }
            if (Item == null) return NotFound();
            return Page();
        }

        // Aggiunge l'item ai preferiti
        public async Task<IActionResult> OnPostAddFavoriteAsync(int ItemId, string ItemType, string Title, string ImageUrl)
        {
            // Per semplicità si usa User.Identity.Name; in una soluzione reale usa l'ID utente appropriato
            var userId = User.Identity.Name;
            var favorite = new Favorite
            {
                UserId = userId,
                ItemId = ItemId,
                ItemType = ItemType,
                Title = Title,
                ImageUrl = ImageUrl
            };
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return RedirectToPage("/Favorites/Index");
        }
    }
}
