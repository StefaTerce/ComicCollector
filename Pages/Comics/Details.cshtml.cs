using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Data;
using ComicCollector.Models;

namespace ComicCollector.Pages.Comics
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

        public Comic Comic { get; set; }

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

            Comic = await _context.Comics
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (Comic == null)
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

            var comic = await _context.Comics
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (comic == null)
            {
                return NotFound();
            }

            comic.IsFavorite = !comic.IsFavorite;
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

            var comic = await _context.Comics
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (comic == null)
            {
                return NotFound();
            }

            _context.Comics.Remove(comic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{comic.Title}' è stato rimosso dalla tua collezione.";
            return RedirectToPage("./Index");
        }
    }
}