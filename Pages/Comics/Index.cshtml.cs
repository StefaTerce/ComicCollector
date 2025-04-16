﻿using System.Collections.Generic;
using System.Linq;
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
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<Comic> Comics { get; set; } = new List<Comic>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool FavoritesOnly { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var query = _context.Comics
                .Where(c => c.UserId == user.Id);

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(c =>
                    c.Title.Contains(SearchTerm) ||
                    c.Authors.Contains(SearchTerm) ||
                    c.Characters.Contains(SearchTerm));
            }

            if (FavoritesOnly)
            {
                query = query.Where(c => c.IsFavorite);
            }

            Comics = await query.OrderBy(c => c.Title).ToListAsync();

            return Page();
        }
    }
}