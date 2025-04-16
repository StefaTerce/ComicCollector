using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Data;
using ComicCollector.Models;

namespace ComicCollector.Pages.Mangas
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DetailsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public Manga Manga { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            Manga = await _context.Mangas
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (Manga == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var manga = await _context.Mangas
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (manga == null)
            {
                return NotFound();
            }

            manga.IsFavorite = !manga.IsFavorite;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var manga = await _context.Mangas
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (manga == null)
            {
                return NotFound();
            }

            _context.Mangas.Remove(manga);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{manga.Title}' è stato rimosso dalla tua collezione.";
            return RedirectToPage("./Index");
        }
    }
}