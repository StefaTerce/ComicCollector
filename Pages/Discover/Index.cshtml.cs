using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ComicCollector.Models;
using ComicCollector.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq; // Per .Any()

namespace ComicCollector.Pages.Discover
{
    public class IndexModel : PageModel
    {
        private readonly ComicVineService _comicVineService;
        private readonly MangaDexService _mangaDexService;
        private readonly ILogger<IndexModel> _logger;
        private readonly SessionInfoService _sessionInfo;

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty]
        public bool SearchComicVine { get; set; } = true;

        [BindProperty]
        public bool SearchMangaDex { get; set; } = true;

        public List<Comic> ComicVineResults { get; set; } = new List<Comic>();
        public List<MangaViewModel> MangaDexResults { get; set; } = new List<MangaViewModel>();
        public bool ShowSearchResults { get; set; } = false;

        public List<Comic> NewReleases { get; set; } = new List<Comic>();

        [TempData]
        public string StatusMessage { get; set; }
        [TempData]
        public bool IsError { get; set; }


        public IndexModel(
            ComicVineService comicVineService,
            MangaDexService mangaDexService,
            ILogger<IndexModel> logger,
            SessionInfoService sessionInfo)
        {
            _comicVineService = comicVineService;
            _mangaDexService = mangaDexService;
            _logger = logger;
            _sessionInfo = sessionInfo;
        }

        public async Task OnGetAsync()
        {
            _sessionInfo.LogSessionInfo("Discover/Index");

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
            _sessionInfo.LogSessionInfo("Discover/Index - POST Search");
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
                searchTasks.Add(SearchComicVineAsync());
            }
            else
            {
                ComicVineResults = new List<Comic>();
            }

            if (SearchMangaDex)
            {
                searchTasks.Add(SearchMangaDexAsync());
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

        private async Task SearchComicVineAsync()
        {
            try
            {
                var query = new ComicVineSearchQuery { Query = SearchQuery, Limit = 12 };
                ComicVineResults = await _comicVineService.SearchComicsAsync(query);
                _logger.LogInformation($"Found {ComicVineResults.Count} comics from ComicVine for '{SearchQuery}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ComicVine search error for '{SearchQuery}'.");
                ComicVineResults = new List<Comic>();
                StatusMessage = "Errore durante la ricerca su ComicVine.";
                IsError = true;
            }
        }

        private async Task SearchMangaDexAsync()
        {
            try
            {
                var query = new MangaDexSearchQuery { Title = SearchQuery, Limit = 12 };
                MangaDexResults = await _mangaDexService.SearchMangaAsync(query);

                foreach (var manga in MangaDexResults)
                {
                    // Tenta di ottenere l'URL della copertina. 
                    // Questa logica è semplificata; GetMangaDexRelationshipsAsync non è implementato per fare una chiamata separata.
                    // Il MangaDexService.GetCoverUrlAsync ora fa la chiamata API necessaria.
                    var mangaRelationships = await GetMangaDexRelationshipsAsync(manga.Id); // Questo metodo è un placeholder
                    var coverRel = mangaRelationships?.FirstOrDefault(r => r.Type == "cover_art");

                    if (coverRel != null && !string.IsNullOrEmpty(coverRel.Id))
                    {
                        manga.CoverImageUrl = await _mangaDexService.GetCoverUrlAsync(manga.Id, coverRel.Id);
                    }
                    else if (!string.IsNullOrEmpty(manga.Id)) // Fallback se la relazione non è trovata direttamente
                    {
                        // Prova a cercare la relazione di copertina se non è stata inclusa o trovata subito
                        // Questo è un tentativo, potrebbe essere necessario un approccio più robusto
                        var tempMangaData = new MangaDexManga { Id = manga.Id, Relationships = await FetchMangaRelationshipsAsync(manga.Id) };
                        var actualCoverRel = tempMangaData.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
                        if (actualCoverRel != null && !string.IsNullOrEmpty(actualCoverRel.Id))
                        {
                            manga.CoverImageUrl = await _mangaDexService.GetCoverUrlAsync(manga.Id, actualCoverRel.Id);
                        }
                    }
                }
                _logger.LogInformation($"Found {MangaDexResults.Count} manga from MangaDex for '{SearchQuery}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MangaDex search error for '{SearchQuery}'.");
                MangaDexResults = new List<MangaViewModel>();
                StatusMessage = "Errore durante la ricerca su MangaDex.";
                IsError = true;
            }
        }

        // Questo metodo è un placeholder e non è completamente implementato per fare una chiamata API separata
        // per le relazioni se non sono incluse nella ricerca principale.
        // La logica di recupero della copertina è stata spostata in MangaDexService.GetCoverUrlAsync.
        private async Task<List<MangaDexRelationship>> GetMangaDexRelationshipsAsync(string mangaId)
        {
            // In una implementazione reale, se le relazioni non sono disponibili tramite 'includes',
            // dovresti fare una chiamata API a /manga/{mangaId} per ottenerle.
            // Per questa demo, restituiamo una lista vuota, assumendo che 'includes' abbia funzionato
            // o che GetCoverUrlAsync in MangaDexService gestirà il recupero.
            await Task.CompletedTask; // Per evitare warning CS1998
            return new List<MangaDexRelationship>();
        }

        // Metodo di esempio per recuperare relazioni se non incluse (non usato attivamente per la copertina ora)
        private async Task<List<MangaDexRelationship>> FetchMangaRelationshipsAsync(string mangaId)
        {
            // Qui dovresti implementare una chiamata HTTP al MangaDexService per ottenere i dettagli del manga,
            // inclusi i suoi relationships, se non sono stati forniti dalla query di ricerca iniziale.
            // Esempio: var mangaDetails = await _mangaDexService.GetMangaDetailsByIdAsync(mangaId);
            // return mangaDetails?.Relationships ?? new List<MangaDexRelationship>();
            await Task.CompletedTask;
            return new List<MangaDexRelationship>();
        }

        private async Task LoadFeaturedContentAsync()
        {
            try
            {
                NewReleases = CreateSampleComicsForFeatured(5, "New Release"); // Utilizza il metodo corretto
                // In futuro, potresti voler caricare dati reali qui, ad esempio:
                // var query = new ComicVineSearchQuery { Limit = 5, /* ...altri filtri/sort per novità... */ };
                // NewReleases = await _comicVineService.SearchComicsAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading featured content (New Releases).");
                NewReleases = new List<Comic>();
            }
        }

        // Metodo corretto per creare dati di esempio per la sezione "featured"
        private List<Comic> CreateSampleComicsForFeatured(int count, string type) // Riga 214 (circa)
        {
            var comics = new List<Comic>();
            var random = new Random();
            for (int i = 1; i <= count; i++)
            {
                comics.Add(new Comic
                {
                    // CORREZIONE: Usa SourceId per ID da API fittizia
                    SourceId = $"sample-feat-{type.ToLower().Replace(" ", "-")}-{i}", // RIGA 223 (circa) - CORRETTA
                    Title = $"{type} Example Vol. {i}",
                    Series = $"{type} Series Example",
                    IssueNumber = i,
                    Author = "Featured Author Name",
                    Publisher = "Featured Publisher Ltd.",
                    PublicationDate = DateTime.Now.AddDays(-random.Next(1, 60)),
                    CoverImage = $"https://via.placeholder.com/300x450/5dade2/ffffff?text={Uri.EscapeDataString(type)}+%23{i}",
                    Source = "ComicVine", // O un'altra fonte se stai simulando quella
                    Description = $"Questa è una descrizione di esempio per il fumetto '{type} Example Vol. {i}'. Contiene dettagli sulla trama e sui personaggi."
                });
            }
            return comics;
        }
    }
}