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
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(u =>
                    u.UserName.Contains(SearchTerm) ||
                    u.Email.Contains(SearchTerm) ||
                    u.FirstName.Contains(SearchTerm) ||
                    u.LastName.Contains(SearchTerm));
            }

            var users = await query.ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var comicsCount = await _context.Comics.CountAsync(c => c.UserId == user.Id);
                var mangasCount = await _context.Mangas.CountAsync(m => m.UserId == user.Id);

                Users.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
                    ComicsCount = comicsCount,
                    MangasCount = mangasCount
                });
            }

            return Page();
        }

        public class UserViewModel
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public List<string> Roles { get; set; } = new List<string>();
            public int ComicsCount { get; set; }
            public int MangasCount { get; set; }
        }
    }
}