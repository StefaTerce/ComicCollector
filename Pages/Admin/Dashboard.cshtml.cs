using System;
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

        public int UserCount { get; set; }
        public int ComicCount { get; set; }
        public int MangaCount { get; set; }
        public List<RecentUserViewModel> RecentUsers { get; set; } = new List<RecentUserViewModel>();
        public List<ActivityViewModel> RecentActivity { get; set; } = new List<ActivityViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get counts
            UserCount = await _userManager.Users.CountAsync();
            ComicCount = await _context.Comics.CountAsync();
            MangaCount = await _context.Mangas.CountAsync();

            // Get recent users
            var users = await _userManager.Users
                .OrderByDescending(u => u.Id)
                .Take(5)
                .ToListAsync();

            foreach (var user in users)
            {
                RecentUsers.Add(new RecentUserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    CreatedAt = DateTime.Now.AddDays(-new Random().Next(1, 30)) // Simulazione data registrazione
                });
            }

            // Simulate recent activity
            var recentComics = await _context.Comics
                .Include(c => c.User)
                .OrderByDescending(c => c.Id)
                .Take(3)
                .ToListAsync();

            var recentMangas = await _context.Mangas
                .Include(m => m.User)
                .OrderByDescending(m => m.Id)
                .Take(3)
                .ToListAsync();

            foreach (var comic in recentComics)
            {
                RecentActivity.Add(new ActivityViewModel
                {
                    UserName = comic.User.UserName,
                    Description = $"Ha aggiunto il fumetto '{comic.Title}' alla collezione",
                    Timestamp = DateTime.Now.AddHours(-new Random().Next(1, 48))
                });
            }

            foreach (var manga in recentMangas)
            {
                RecentActivity.Add(new ActivityViewModel
                {
                    UserName = manga.User.UserName,
                    Description = $"Ha aggiunto il manga '{manga.Title}' alla collezione",
                    Timestamp = DateTime.Now.AddHours(-new Random().Next(1, 48))
                });
            }

            // Order activity by timestamp
            RecentActivity = RecentActivity.OrderByDescending(a => a.Timestamp).ToList();

            return Page();
        }

        public class RecentUserViewModel
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class ActivityViewModel
        {
            public string UserName { get; set; }
            public string Description { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}