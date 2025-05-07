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

        public List<Comic> UserComics { get; set; }
        public HashSet<string> AllSeries { get; set; }
        public HashSet<string> AllPublishers { get; set; }
        public int SeriesCount => AllSeries?.Count ?? 0;
        public int AuthorsCount { get; set; }
        public int PublishersCount => AllPublishers?.Count ?? 0;

        [TempData]
        public string StatusMessage { get; set; }
        [TempData]
        public bool IsError { get; set; }

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<IndexModel> logger,
            SessionInfoService sessionInfo)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _sessionInfo = sessionInfo;
            UserComics = new List<Comic>();
            AllSeries = new HashSet<string>();
            AllPublishers = new HashSet<string>();
        }

        public async Task OnGetAsync()
        {
            _sessionInfo.LogSessionInfo("Collection/Index");

            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                StatusMessage = "Errore: Utente non identificato.";
                IsError = true;
                return;
            }

            UserComics = await _context.Comics
                                 .Where(c => c.UserId == currentUserId)
                                 .OrderByDescending(c => c.CreatedAt)
                                 .ToListAsync();

            if (UserComics.Any())
            {
                AllSeries = new HashSet<string>(UserComics.Select(c => c.Series).Where(s => !string.IsNullOrEmpty(s)).Distinct(), StringComparer.OrdinalIgnoreCase);
                AllPublishers = new HashSet<string>(UserComics.Select(c => c.Publisher).Where(p => !string.IsNullOrEmpty(p)).Distinct(), StringComparer.OrdinalIgnoreCase);
                AuthorsCount = UserComics.Select(c => c.Author)
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
        }

        public async Task<IActionResult> OnPostAddComicAsync(Comic newComic) // Aggiunta Manuale
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return Forbid();

            // Rimuovi User e UserId da ModelState per la validazione del form manuale
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("SourceId"); // SourceId non è nel form manuale

            if (!ModelState.IsValid)
            {
                IsError = true;
                StatusMessage = "Errore nella validazione dei dati del fumetto. Controlla i campi.";
                await OnGetAsync();
                return Page();
            }

            newComic.UserId = currentUserId;
            newComic.CreatedAt = DateTime.UtcNow;
            newComic.UpdatedAt = DateTime.UtcNow;
            newComic.Source = "Local"; // Specifico per aggiunta manuale
            // newComic.SourceId sarà null per aggiunte manuali

            _context.Comics.Add(newComic);
            await _context.SaveChangesAsync();

            IsError = false;
            StatusMessage = $"'{newComic.Title}' aggiunto con successo alla tua collezione!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateComicAsync(Comic updatedComic)
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return Forbid();

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            // Source e SourceId sono hidden fields, quindi dovrebbero essere validi
            // CreatedAt è un hidden field

            if (!ModelState.IsValid)
            {
                IsError = true;
                StatusMessage = "Errore nella validazione dei dati per l'aggiornamento. Controlla i campi.";
                await OnGetAsync();
                return Page();
            }

            var comicToUpdate = await _context.Comics
                                        .FirstOrDefaultAsync(c => c.ComicCollectorId == updatedComic.ComicCollectorId && c.UserId == currentUserId);

            if (comicToUpdate == null)
            {
                IsError = true;
                StatusMessage = "Fumetto non trovato o non sei autorizzato a modificarlo.";
                return RedirectToPage();
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
            // Source, SourceId e CreatedAt non dovrebbero cambiare durante un update normale.

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
                await OnGetAsync(); // Ricarica i dati freschi
                return Page();
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = "Si è verificato un errore imprevisto durante l'aggiornamento.";
                _logger.LogError(ex, "Error updating comic ID {ComicId}", comicToUpdate.ComicCollectorId);
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage();
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
                return RedirectToPage();
            }

            _context.Comics.Remove(comicToDelete);
            await _context.SaveChangesAsync();

            IsError = false;
            StatusMessage = $"'{comicToDelete.Title}' rimosso dalla collezione.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddToCollectionFromDiscoverAsync(
            string title, string series, int? issueNumber, string author, string publisher,
            string publicationDateStr, string coverImage, string description,
            string source, string sourceId)
        {
            var currentUserId = _sessionInfo.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                // Se l'utente non è loggato, reindirizza al login
                TempData["ErrorMessage"] = "Devi effettuare il login per aggiungere elementi alla tua collezione.";
                return RedirectToPage("/Account/Login", new { ReturnUrl = Url.Page("/Discover/Index", new { SearchQuery = TempData["LastSearchQuery"] }) });
            }

            // Prova a parsare la data
            if (!DateTime.TryParse(publicationDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime publicationDate))
            {
                // Se il parsing fallisce, usa una data di default o gestisci l'errore
                // Potrebbe essere che l'API non fornisca sempre una data completa.
                // Per MangaDex, abbiamo usato Year. Se è Year, usiamo il 1 Gennaio di quell'anno.
                // Per ComicVine, abbiamo provato a parsare "YYYY-MM-DD" o "YYYY-MM-00".
                // Questo handler riceve una stringa formattata come "o" (ISO 8601).
                _logger.LogWarning($"Could not parse publicationDate: {publicationDateStr} for {title}. Using DateTime.MinValue.");
                publicationDate = DateTime.MinValue; // O gestisci come errore
            }


            // Controlla se l'elemento esiste già per questo utente dalla stessa fonte e ID sorgente
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

            // Valida il modello manualmente se necessario, anche se i dati provengono da API
            // TryValidateModel(newComic); // Opzionale

            _context.Comics.Add(newComic);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"'{title}' aggiunto alla tua collezione!";
            TempData["IsError"] = false;
            return RedirectToPage("/Discover/Index", new { SearchQuery = TempData["LastSearchQuery"] ?? "" });
        }
    }
}