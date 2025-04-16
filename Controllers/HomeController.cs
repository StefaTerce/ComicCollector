using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Data;
using ComicCollector.Models;
using ComicCollector.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace ComicCollector.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            // Redirect based on authentication status and role
            if (_signInManager.IsSignedIn(User))
            {
                var user = await _userManager.GetUserAsync(User);

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }

                var dashboard = new DashboardViewModel
                {
                    RecentComics = await _context.Comics
                        .Where(c => c.UserId == user.Id)
                        .OrderByDescending(c => c.Id)
                        .Take(5)
                        .ToListAsync(),

                    RecentMangas = await _context.Mangas
                        .Where(m => m.UserId == user.Id)
                        .OrderByDescending(m => m.Id)
                        .Take(5)
                        .ToListAsync(),

                    FavoriteComics = await _context.Comics
                        .Where(c => c.UserId == user.Id && c.IsFavorite)
                        .Take(5)
                        .ToListAsync(),

                    FavoriteMangas = await _context.Mangas
                        .Where(m => m.UserId == user.Id && m.IsFavorite)
                        .Take(5)
                        .ToListAsync()
                };

                return View("Dashboard", dashboard);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}