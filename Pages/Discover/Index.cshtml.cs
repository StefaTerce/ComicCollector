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
        private readonly GeminiService _geminiService;
        private readonly ApplicationDbContext _context;

        private const int FEATURED_ITEMS_PER_SOURCE = 20; // Quanti elementi caricare per fonte per i "featured"
        private const int MAX_FEATURED_ITEMS_DISPLAY = 40; // Quanti elementi totali mostrare nella UI per i "featured"

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty]
        public bool SearchComicVine { get; set; } = true;

        [BindProperty]
        public bool SearchMangaDex { get; set; } = true;

        public List<Comic> ComicVineResults { get; set; } = new List<Comic>();
        public List<MangaViewModel> MangaDexResults { get; set; } = new List<MangaViewModel>();
        public bool ShowSearchResults { get; set; } = false;

        public List<object> FeaturedItems { get; set; } = new List<object>();

        public List<string> RecommendedComics { get; set; } = new List<string>();
        public List<string> RecommendedManga { get; set; } = new List<string>();
        public bool HasRecommendations => (RecommendedComics.Any() || RecommendedManga.Any());

        [TempData]
        public string StatusMessage { get; set; }
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
            // Log session info at the beginning of OnGet
            _sessionInfo.LogSessionInfo("Discover/Index - OnGetAsync Start");
            // Explicit log of current session values as requested
            _logger.LogInformation($"Session Info Check - UTC Time: {_sessionInfo.GetCurrentUtcDateTime()} | User: {_sessionInfo.GetSessionUserName()} (ID: {_sessionInfo.GetCurrentUserId()})");

            // Get user's collection - assuming all comics in DB are the user's for now
            // In a real app, you'd filter by UserId
            var userCollection = await _context.Comics.ToListAsync();

            if (userCollection.Any())
            {
                _logger.LogInformation($"User collection has {userCollection.Count} items. Fetching recommendations.");
                var recommendations = await _geminiService.GetRecommendationsAsync(userCollection, 5, 5); // Request 5 comics and 5 manga
                RecommendedComics = recommendations.RecommendedComics;
                RecommendedManga = recommendations.RecommendedManga;
                _logger.LogInformation($"Received {RecommendedComics.Count} comic recommendations and {RecommendedManga.Count} manga recommendations.");
            }
            else
            {
                _logger.LogInformation("User collection is empty. No recommendations will be fetched.");
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                ShowSearchResults = true;
                await PerformSearch();
            }
            else
            {
                ShowSearchResults = false; // Ensure search results are not shown if no query
                await LoadFeaturedContentAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Log session info at the beginning of OnPost
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
                    Query = isFeaturedSearch ? "" : SearchQuery, // Query vuota per featured, altrimenti la query dell'utente
                    Limit = isFeaturedSearch ? FEATURED_ITEMS_PER_SOURCE : 24,
                    // Per featured, potresti voler ordinare per 'date_added:desc' o 'store_date:desc'
                    // Questo richiederebbe di estendere ComicVineSearchQuery e il servizio per gestire 'sort'
                    // FieldList = "id,name,image,description,issue_number,volume,person_credits,cover_date,store_date" // Già nel modello
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
                    Title = isFeaturedSearch ? "" : SearchQuery, // Titolo vuoto per featured
                    Limit = isFeaturedSearch ? FEATURED_ITEMS_PER_SOURCE : 24,
                    OrderCreatedAt = isFeaturedSearch ? "desc" : null, // Ordina per recenti solo per featured
                    Includes = "cover_art,author,artist"
                };
                var results = await _mangaDexService.SearchMangaAsync(query);

                // Recupera gli URL delle copertine per tutti i risultati (sia featured che ricerca normale)
                // Questo può essere intensivo se ci sono molti risultati.
                foreach (var manga in results)
                {
                    if (string.IsNullOrEmpty(manga.CoverImageUrl)) // Prova a caricare solo se non già presente
                    {
                        // Il MangaDexService ora dovrebbe tentare di ottenere il coverId durante SearchMangaAsync
                        // e GetCoverUrlAsync lo userà.
                        // Per i featured, vogliamo assicurarci che le copertine siano caricate.
                        var mangaRelationships = await FetchMangaRelationshipsAsync(manga.Id); // Questo è ancora un placeholder
                        var coverRel = mangaRelationships?.FirstOrDefault(r => r.Type == "cover_art");
                        // Se SearchMangaAsync non ha popolato coverRel in modo utile, cerchiamo di nuovo.
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

        // Modificato per accettare un flag per forzare una chiamata API se necessario
        private async Task<List<MangaDexRelationship>> FetchMangaRelationshipsAsync(string mangaId, bool forceApiCall = false)
        {
            // Se 'forceApiCall' è true, o se le relazioni non sono state ottenute tramite 'includes',
            // si dovrebbe fare una chiamata a /manga/{mangaId}?includes[]=cover_art,author,artist
            // Per ora, questo metodo rimane un placeholder perché la logica di recupero copertina
            // è principalmente in MangaDexService.GetCoverUrlAsync.
            // Il flag 'forceApiCall' è per un'eventuale implementazione futura più granulare.
            await Task.CompletedTask;
            return new List<MangaDexRelationship>();
        }

        private async Task LoadFeaturedContentAsync()
        {
            _logger.LogInformation("Loading featured content for Discover page...");
            FeaturedItems = new List<object>(); // Resetta prima di caricare
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