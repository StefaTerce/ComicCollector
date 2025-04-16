using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ComicCollector.Data;
using ComicCollector.Models;
using ComicCollector.Services;

namespace ComicCollector.Pages.Mangas
{
    [Authorize]
    public class SearchModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly MyAnimeListService _mangaService;

        public SearchModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            MyAnimeListService mangaService)
        {
            _userManager = userManager;
            _context = context;
            _mangaService = mangaService;
        }

        [BindProperty]
        public string SearchTerm { get; set; }

        public List<Manga> SearchResults { get; set; }

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

            SearchResults = await _mangaService.SearchManga(SearchTerm);
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(string apiId)
        {
            if (string.IsNullOrEmpty(apiId) || !int.TryParse(apiId, out int malId))
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var mangaDetails = await _mangaService.GetMangaDetails(malId);

            if (mangaDetails == null)
            {
                return NotFound();
            }

            mangaDetails.UserId = user.Id;

            _context.Mangas.Add(mangaDetails);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{mangaDetails.Title}' è stato aggiunto alla tua collezione.";
            return RedirectToPage("./Index");
        }
    }
}