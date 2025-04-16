using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Data;
using ComicCollector.Models;

namespace ComicCollector.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserDetailsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserDetailsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<Comic> Comics { get; set; } = new List<Comic>();
        public List<Manga> Mangas { get; set; } = new List<Manga>();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            User = await _userManager.FindByIdAsync(id);

            if (User == null)
            {
                return NotFound();
            }

            Roles = (await _userManager.GetRolesAsync(User)).ToList();

            Comics = await _context.Comics
                .Where(c => c.UserId == id)
                .ToListAsync();

            Mangas = await _context.Mangas
                .Where(m => m.UserId == id)
                .ToListAsync();

            return Page();
        }
    }
}