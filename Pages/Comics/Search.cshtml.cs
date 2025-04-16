using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ComicCollector.Data;
using ComicCollector.Models;
using ComicCollector.Services;

namespace ComicCollector.Pages.Comics
{
    [Authorize]
    public class SearchModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ComicVineService _comicService;

        public SearchModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ComicVineService comicService)
        {
            _userManager = userManager;
            _context = context;
            _comicService = comicService;
        }

        [BindProperty]
        public string SearchTerm { get; set; }

        public List<Comic> SearchResults { get; set; }

        public bool SearchSubmitted { get; set; }

        public void OnGet()
        {
            // Initial page load - no action needed
        }

        public async Task<IActionResult> OnPostAsync()
        {
            SearchSubmitted = true;

            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                return Page();
            }

            SearchResults = await _comicService.SearchComics(SearchTerm);
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(string apiId)
        {
            if (string.IsNullOrEmpty(apiId))
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var comicDetails = await _comicService.GetComicDetails(apiId);

            if (comicDetails == null)
            {
                return NotFound();
            }

            comicDetails.UserId = user.Id;

            _context.Comics.Add(comicDetails);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{comicDetails.Title}' è stato aggiunto alla tua collezione.";
            return RedirectToPage("./Index");
        }
    }
}