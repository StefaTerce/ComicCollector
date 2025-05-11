using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ComicCollector.Models;
using ComicCollector.Data;
using ComicCollector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ComicCollector.Pages.Collection
{
    [Authorize(Policy = "RequireUserRole")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly SessionInfoService _sessionInfo;
        private readonly GeminiService _geminiService;

        public List<Comic> UserComics { get; set; }
        public HashSet<string> AllSeries { get; set; }
        public HashSet<string> AllPublishers { get; set; }
        public int SeriesCount => AllSeries?.Count ?? 0;
        public int AuthorsCount { get; set; }
        public int PublishersCount => AllPublishers?.Count ?? 0;
        public List<string> RecommendedComics { get; set; }
        public List<string> RecommendedManga { get; set; }

        [TempData]
        public string StatusMessage { get; set; }
        [TempData]
        public bool IsError { get; set; }

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; private set; } = 20; // Number of items per page
        public int TotalPages { get; set; }
        public int TotalComicsCount { get; set; }

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<IndexModel> logger,
            SessionInfoService sessionInfo,
            GeminiService geminiService)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _sessionInfo = sessionInfo;
            _geminiService = geminiService;
            UserComics = new List<Comic>();
            AllSeries = new HashSet<string>();
            AllPublishers = new HashSet<string>();
            RecommendedComics = new List<string>();
            RecommendedManga = new List<string>();
        }

        public async Task OnGetAsync(int p = 1)
        {
            _sessionInfo.LogSessionInfo("Collection/Index");

            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                StatusMessage = "Errore: Utente non identificato.";
                IsError = true;
                UserComics = new List<Comic>();
                AllSeries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                AllPublishers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                AuthorsCount = 0;
                TotalComicsCount = 0;
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            // Get statistics for the entire collection first
            var allUserComicsForStats = await _context.Comics
                                         .Where(c => c.UserId == currentUserId)
                                         .ToListAsync();

            TotalComicsCount = allUserComicsForStats.Count;

            if (allUserComicsForStats.Any())
            {
                AllSeries = new HashSet<string>(allUserComicsForStats.Select(c => c.Series).Where(s => !string.IsNullOrEmpty(s)).Distinct(), StringComparer.OrdinalIgnoreCase);
                AllPublishers = new HashSet<string>(allUserComicsForStats.Select(c => c.Publisher).Where(p => !string.IsNullOrEmpty(p)).Distinct(), StringComparer.OrdinalIgnoreCase);
                AuthorsCount = allUserComicsForStats.Select(c => c.Author)
                                         .Where(a => !string.IsNullOrEmpty(a))
                                         .Distinct(StringComparer.OrdinalIgnoreCase)
                                         .Count();
            }
            else
            {
                AllSeries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                AllPublishers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                AuthorsCount = 0;
            }

            // Pagination logic
            CurrentPage = p;
            TotalPages = (int)Math.Ceiling(TotalComicsCount / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // Fetch paginated comics
            UserComics = await _context.Comics
                                 .Where(c => c.UserId == currentUserId)
                                 .OrderByDescending(c => c.CreatedAt)
                                 .Skip((CurrentPage - 1) * PageSize)
                                 .Take(PageSize)
                                 .ToListAsync();

            // Load AI recommendations
            var userCollection = await _context.Comics.Where(c => c.UserId == currentUserId).ToListAsync();
            if (userCollection.Any())
            {
                var recommendations = await _geminiService.GetRecommendationsAsync(userCollection, 5, 5);
                RecommendedComics = recommendations.RecommendedComics;
                RecommendedManga = recommendations.RecommendedManga;
            }
            else
            {
                RecommendedComics = new List<string>();
                RecommendedManga = new List<string>();
            }
        }

        public async Task<IActionResult> OnPostAddComicAsync(Comic newComic)
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return Forbid();

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("SourceId");

            if (!ModelState.IsValid)
            {
                IsError = true;
                StatusMessage = "Errore nella validazione dei dati del fumetto. Controlla i campi.";
                await OnGetAsync(CurrentPage);
                return Page();
            }

            newComic.UserId = currentUserId;
            newComic.CreatedAt = DateTime.UtcNow;
            newComic.UpdatedAt = DateTime.UtcNow;
            newComic.Source = "Local";

            _context.Comics.Add(newComic);
            await _context.SaveChangesAsync();

            IsError = false;
            StatusMessage = $"'{newComic.Title}' aggiunto con successo alla tua collezione!";
            return RedirectToPage(new { p = CurrentPage });
        }

        public async Task<IActionResult> OnPostUpdateComicAsync(Comic updatedComic)
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return Forbid();

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (!ModelState.IsValid)
            {
                IsError = true;
                StatusMessage = "Errore nella validazione dei dati per l'aggiornamento. Controlla i campi.";
                await OnGetAsync(CurrentPage);
                return Page();
            }

            var comicToUpdate = await _context.Comics
                                        .FirstOrDefaultAsync(c => c.ComicCollectorId == updatedComic.ComicCollectorId && c.UserId == currentUserId);

            if (comicToUpdate == null)
            {
                IsError = true;
                StatusMessage = "Fumetto non trovato o non sei autorizzato a modificarlo.";
                return RedirectToPage(new { p = CurrentPage });
            }

            comicToUpdate.Title = updatedComic.Title;
            comicToUpdate.Series = updatedComic.Series;
            comicToUpdate.IssueNumber = updatedComic.IssueNumber;
            comicToUpdate.Author = updatedComic.Author;
            comicToUpdate.Publisher = updatedComic.Publisher;
            comicToUpdate.PublicationDate = updatedComic.PublicationDate;
            comicToUpdate.PageCount = updatedComic.PageCount;
            comicToUpdate.CoverImage = updatedComic.CoverImage;
            comicToUpdate.Description = updatedComic.Description;
            comicToUpdate.Notes = updatedComic.Notes;
            comicToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                IsError = false;
                StatusMessage = $"'{comicToUpdate.Title}' aggiornato con successo!";
            }
            catch (DbUpdateConcurrencyException)
            {
                IsError = true;
                StatusMessage = "Errore di concorrenza durante l'aggiornamento. Il fumetto potrebbe essere stato modificato da un altro processo. Riprova.";
                _logger.LogError("DbUpdateConcurrencyException for comic ID {ComicId}", comicToUpdate.ComicCollectorId);
                await OnGetAsync(CurrentPage);
                return Page();
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = "Si è verificato un errore imprevisto durante l'aggiornamento.";
                _logger.LogError(ex, "Error updating comic ID {ComicId}", comicToUpdate.ComicCollectorId);
                await OnGetAsync(CurrentPage);
                return Page();
            }

            return RedirectToPage(new { p = CurrentPage });
        }

        public async Task<IActionResult> OnPostDeleteComicAsync(int comicIdToDelete)
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return Forbid();

            var comicToDelete = await _context.Comics
                                        .FirstOrDefaultAsync(c => c.ComicCollectorId == comicIdToDelete && c.UserId == currentUserId);

            if (comicToDelete == null)
            {
                IsError = true;
                StatusMessage = "Fumetto non trovato o non sei autorizzato a eliminarlo.";
                return RedirectToPage(new { p = CurrentPage });
            }

            _context.Comics.Remove(comicToDelete);
            await _context.SaveChangesAsync();

            IsError = false;
            StatusMessage = $"'{comicToDelete.Title}' rimosso dalla collezione.";
            return RedirectToPage(new { p = CurrentPage });
        }

        public async Task<IActionResult> OnPostAddToCollectionFromDiscoverAsync(
            string title, string series, int? issueNumber, string author, string publisher,
            string publicationDateStr, string coverImage, string description,
            string source, string sourceId)
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                TempData["ErrorMessage"] = "Devi effettuare il login per aggiungere elementi alla tua collezione.";
                return RedirectToPage("/Account/Login", new { ReturnUrl = Url.Page("/Discover/Index", new { SearchQuery = TempData["LastSearchQuery"] }) });
            }

            if (!DateTime.TryParse(publicationDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime publicationDate))
            {
                _logger.LogWarning($"Could not parse publicationDate: {publicationDateStr} for {title}. Using DateTime.MinValue.");
                publicationDate = DateTime.MinValue;
            }

            var existingComic = await _context.Comics
                .FirstOrDefaultAsync(c => c.UserId == currentUserId && c.Source == source && c.SourceId == sourceId);

            if (existingComic != null)
            {
                TempData["StatusMessage"] = $"'{title}' è già nella tua collezione.";
                TempData["IsError"] = true;
                return RedirectToPage("/Discover/Index", new { SearchQuery = TempData["LastSearchQuery"] ?? "" });
            }

            var newComic = new Comic
            {
                Title = title,
                Series = series,
                IssueNumber = issueNumber,
                Author = author,
                Publisher = publisher,
                PublicationDate = publicationDate,
                CoverImage = coverImage,
                Description = description,
                Source = source,
                SourceId = sourceId,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Comics.Add(newComic);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"'{title}' aggiunto alla tua collezione!";
            TempData["IsError"] = false;
            return RedirectToPage("/Discover/Index", new { SearchQuery = TempData["LastSearchQuery"] ?? "" });
        }

        public async Task<JsonResult> OnGetComicDetailsWithGeminiAsync(int comicId)
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new JsonResult(new { error = "User not authenticated." }) { StatusCode = 401 };
            }

            var comic = await _context.Comics
                                .FirstOrDefaultAsync(c => c.ComicCollectorId == comicId && c.UserId == currentUserId);

            if (comic == null)
            {
                return new JsonResult(new { error = "Comic not found or not authorized." }) { StatusCode = 404 };
            }

            try
            {
                var originalAuthor = comic.Author;
                var originalPublisher = comic.Publisher;
                var originalDescription = comic.Description;
                var originalPublicationDate = comic.PublicationDate;

                Comic? enrichedComic = await _geminiService.EnrichComicDataAsync(comic);
                bool updated = false;
                if (enrichedComic != null)
                {
                    if (originalAuthor != enrichedComic.Author || 
                        originalPublisher != enrichedComic.Publisher ||
                        originalDescription != enrichedComic.Description ||
                        originalPublicationDate != enrichedComic.PublicationDate)
                    {
                        comic = enrichedComic;
                        _context.Comics.Update(comic);
                        await _context.SaveChangesAsync();
                        updated = true;
                        _logger.LogInformation($"Comic ID {comic.ComicCollectorId} was enriched and updated by Gemini.");
                    }
                }

                string prompt = $"Per il fumetto o manga intitolato '{comic.Title}'";
                if (!string.IsNullOrWhiteSpace(comic.Series))
                {
                    prompt += $" nella serie '{comic.Series}'";
                }
                if (!string.IsNullOrWhiteSpace(comic.Author))
                {
                    prompt += $" dell'autore '{comic.Author}'";
                }
                prompt += ". Fornisci un elenco dettagliato dei suoi lati positivi (almeno 3-5 punti) e lati negativi (almeno 2-4 punti). Rispondi in italiano e formatta la risposta con intestazioni come 'Lati Positivi:' e 'Lati Negativi:'.";

                string? reviewSummary = await _geminiService.GetReviewSummaryAsync(prompt);

                var comicData = new
                {
                    comic.ComicCollectorId,
                    comic.Title,
                    comic.Series,
                    comic.IssueNumber,
                    comic.Author,
                    comic.Publisher,
                    PublicationDate = comic.PublicationDate.ToString("yyyy-MM-dd"),
                    comic.PageCount,
                    comic.CoverImage,
                    comic.Description,
                    comic.Notes,
                    comic.Source,
                    comic.SourceId,
                    CreatedAt = comic.CreatedAt.ToString("o"),
                    UpdatedAt = comic.UpdatedAt.ToString("o"),
                    GeminiReviewSummary = reviewSummary,
                    WasEnriched = updated
                };
                return new JsonResult(comicData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching details with Gemini for comic ID {comicId}.");
                var basicComicData = new
                {
                    comic.ComicCollectorId,
                    comic.Title,
                    comic.Series,
                    comic.IssueNumber,
                    comic.Author,
                    comic.Publisher,
                    PublicationDate = comic.PublicationDate.ToString("yyyy-MM-dd"),
                    comic.PageCount,
                    comic.CoverImage,
                    comic.Description,
                    comic.Notes,
                    comic.Source,
                    comic.SourceId,
                    CreatedAt = comic.CreatedAt.ToString("o"),
                    UpdatedAt = comic.UpdatedAt.ToString("o"),
                    GeminiReviewSummary = "Error fetching review summary.",
                    WasEnriched = false
                };
                return new JsonResult(new { comic = basicComicData, error = "Error processing Gemini data." });
            }
        }
    }
}