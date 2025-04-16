using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Data;
using ComicCollector.Models;
using ComicCollector.ViewModels;

namespace ComicCollector.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public DashboardViewModel DashboardData { get; set; } = new DashboardViewModel();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            // Redirect if admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            DashboardData.RecentComics = await _context.Comics
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.Id)
                .Take(5)
                .ToListAsync();

            DashboardData.RecentMangas = await _context.Mangas
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.Id)
                .Take(5)
                .ToListAsync();

            DashboardData.FavoriteComics = await _context.Comics
                .Where(c => c.UserId == user.Id && c.IsFavorite)
                .Take(5)
                .ToListAsync();

            DashboardData.FavoriteMangas = await _context.Mangas
                .Where(m => m.UserId == user.Id && m.IsFavorite)
                .Take(5)
                .ToListAsync();

            return Page();
        }
    }
}