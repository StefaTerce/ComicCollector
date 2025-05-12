using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ComicCollector.Models;
using ComicCollector.Services;
using ComicCollector.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ComicCollector.Pages.Discover
{
    public class IndexModel : PageModel
    {
        private readonly ComicVineService _comicVineService;
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<IndexModel> _logger;
        private readonly SessionInfoService _sessionInfo;
        private readonly GeminiService _geminiService; // Kept for potential future use, but not for recommendations here
        private readonly ApplicationDbContext _context; // Kept for potential future use

        private const int FEATURED_ITEMS_PER_SOURCE = 20;
        private const int MAX_FEATURED_ITEMS_DISPLAY = 40;

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; } = string.Empty;

        [BindProperty]
        public bool SearchComicVine { get; set; } = true;

        [BindProperty]
        public bool SearchMangaDex { get; set; } = true;

        public List<Comic> ComicVineResults { get; set; } = new List<Comic>();
        public List<MangaViewModel> MangaDexResults { get; set; } = new List<MangaViewModel>();
        public bool ShowSearchResults { get; set; } = false;

        public List<object> FeaturedItems { get; set; } = new List<object>();

        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public bool IsError { get; set; }

        public IndexModel(
            ComicVineService comicVineService,
            MangaDexService mangaDexService,
            ILogger<IndexModel> logger,
            SessionInfoService sessionInfo,
            GeminiService geminiService,
            ApplicationDbContext context)
        {
            _comicVineService = comicVineService;
            _mangaDexService = mangaDexService;
            _logger = logger;
            _sessionInfo = sessionInfo;
            _geminiService = geminiService;
            _context = context;
        }

        public async Task OnGetAsync()
        {
            _sessionInfo.LogSessionInfo("Discover/Index - OnGetAsync Start");
            _logger.LogInformation($"Session Info Check - UTC Time: {_sessionInfo.GetCurrentUtcDateTime()} | User: {_sessionInfo.GetSessionUserName()} (ID: {_sessionInfo.GetCurrentUserId()})");

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                ShowSearchResults = true;
                await PerformSearch();
            }
            else
            {
                ShowSearchResults = false;
                await LoadFeaturedContentAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _sessionInfo.LogSessionInfo("Discover/Index - OnPostAsync Start");
            _logger.LogInformation($"Session Info Check POST - UTC Time: {_sessionInfo.GetCurrentUtcDateTime()} | User: {_sessionInfo.GetSessionUserName()} (ID: {_sessionInfo.GetCurrentUserId()})");

            ShowSearchResults = true;
            TempData["LastSearchQuery"] = SearchQuery;

            if (string.IsNullOrWhiteSpace(SearchQuery) && !SearchComicVine && !SearchMangaDex)
            {
                StatusMessage = "Inserisci un termine di ricerca o seleziona almeno una fonte.";
                IsError = true;
                return Page();
            }
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = "Inserisci un termine di ricerca.";
                IsError = true;
                return Page();
            }
            if (!SearchComicVine && !SearchMangaDex)
            {
                StatusMessage = "Seleziona almeno una fonte (ComicVine o MangaDex).";
                IsError = true;
                return Page();
            }

            await PerformSearch();
            return Page();
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            _logger.LogInformation($"Performing search for: '{SearchQuery}', ComicVine: {SearchComicVine}, MangaDex: {SearchMangaDex}");
            var searchTasks = new List<Task>();

            if (SearchComicVine)
            {
                searchTasks.Add(SearchComicVineAsync(isFeaturedSearch: false));
            }
            else
            {
                ComicVineResults = new List<Comic>();
            }

            if (SearchMangaDex)
            {
                searchTasks.Add(SearchMangaDexAsync(isFeaturedSearch: false));
            }
            else
            {
                MangaDexResults = new List<MangaViewModel>();
            }

            if (searchTasks.Any())
            {
                await Task.WhenAll(searchTasks);
            }
        }

        private async Task SearchComicVineAsync(bool isFeaturedSearch)
        {
            try
            {
                var query = new ComicVineSearchQuery
                {
                    Query = isFeaturedSearch ? "" : SearchQuery,
                    Limit = isFeaturedSearch ? FEATURED_ITEMS_PER_SOURCE : 24,
                };
                var results = await _comicVineService.SearchComicsAsync(query);
                if (isFeaturedSearch)
                {
                    lock (FeaturedItems) { FeaturedItems.AddRange(results); }
                }
                else
                {
                    ComicVineResults = results;
                }
                _logger.LogInformation($"Found {results.Count} comics from ComicVine for '{(isFeaturedSearch ? "featured" : SearchQuery)}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ComicVine search error for '{(isFeaturedSearch ? "featured" : SearchQuery)}'.");
                if (!isFeaturedSearch)
                {
                    ComicVineResults = new List<Comic>();
                    StatusMessage = "Errore durante la ricerca su ComicVine.";
                    IsError = true;
                }
            }
        }

        private async Task SearchMangaDexAsync(bool isFeaturedSearch)
        {
            try
            {
                var query = new MangaDexSearchQuery
                {
                    Title = isFeaturedSearch ? "" : SearchQuery,
                    Limit = isFeaturedSearch ? FEATURED_ITEMS_PER_SOURCE : 24,
                    OrderCreatedAt = isFeaturedSearch ? "desc" : string.Empty,
                    Includes = "cover_art,author,artist"
                };
                var results = await _mangaDexService.SearchMangaAsync(query);

                foreach (var manga in results)
                {
                    if (string.IsNullOrEmpty(manga.CoverImageUrl))
                    {
                        var mangaRelationships = await FetchMangaRelationshipsAsync(manga.Id);
                        var coverRel = mangaRelationships?.FirstOrDefault(r => r.Type == "cover_art");
                        if (coverRel == null)
                        {
                            var tempMangaData = new MangaDexManga { Id = manga.Id, Relationships = await FetchMangaRelationshipsAsync(manga.Id, true) };
                            coverRel = tempMangaData.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
                        }

                        if (coverRel != null && !string.IsNullOrEmpty(coverRel.Id))
                        {
                            manga.CoverImageUrl = await _mangaDexService.GetCoverUrlAsync(manga.Id, coverRel.Id);
                        }
                    }
                }

                if (isFeaturedSearch)
                {
                    lock (FeaturedItems) { FeaturedItems.AddRange(results); }
                }
                else
                {
                    MangaDexResults = results;
                }
                _logger.LogInformation($"Found {results.Count} manga from MangaDex for '{(isFeaturedSearch ? "featured" : SearchQuery)}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MangaDex search error for '{(isFeaturedSearch ? "featured" : SearchQuery)}'.");
                if (!isFeaturedSearch)
                {
                    MangaDexResults = new List<MangaViewModel>();
                    StatusMessage = "Errore durante la ricerca su MangaDex.";
                    IsError = true;
                }
            }
        }

        private async Task<List<MangaDexRelationship>> FetchMangaRelationshipsAsync(string mangaId, bool forceApiCall = false)
        {
            await Task.CompletedTask;
            return new List<MangaDexRelationship>();
        }

        private async Task LoadFeaturedContentAsync()
        {
            _logger.LogInformation("Loading featured content for Discover page...");
            FeaturedItems = new List<object>();
            var tasks = new List<Task>
            {
                SearchComicVineAsync(isFeaturedSearch: true),
                SearchMangaDexAsync(isFeaturedSearch: true)
            };

            await Task.WhenAll(tasks);

            var random = new Random();
            FeaturedItems = FeaturedItems.OrderBy(x => random.Next()).Take(MAX_FEATURED_ITEMS_DISPLAY).ToList();
            _logger.LogInformation($"Total featured items after mixing and taking max: {FeaturedItems.Count}");
        }
    }
}