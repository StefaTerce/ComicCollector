using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ComicCollector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComicCollector.Pages.Collection
{
    [Authorize(Policy = "RequireUserRole")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public List<Comic> UserComics { get; set; }
        public HashSet<string> AllSeries { get; set; }
        public HashSet<string> AllPublishers { get; set; }
        public int SeriesCount => AllSeries.Count;
        public int AuthorsCount { get; set; }
        public int PublishersCount => AllPublishers.Count;
        public string CurrentUserName { get; set; }
        public string CurrentDate { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public bool IsError { get; set; }

        public IndexModel(UserManager<ApplicationUser> userManager, ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
            UserComics = new List<Comic>();
            AllSeries = new HashSet<string>();
            AllPublishers = new HashSet<string>();
            AuthorsCount = 0;
        }

        public async Task OnGetAsync()
        {
            // Ottieni informazioni sull'utente corrente
            var currentUser = await _userManager.GetUserAsync(User);
            CurrentUserName = currentUser?.UserName ?? "StefaTerceil";
            CurrentDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // In un'app reale, questi fumetti sarebbero caricati dal database
            // Per ora, creiamo alcuni fumetti di esempio
            var currentUserId = _userManager.GetUserId(User);
            CreateSampleComics(currentUserId);

            // Popola le liste di serie e editori
            AllSeries = new HashSet<string>(UserComics.Select(c => c.Series));
            AllPublishers = new HashSet<string>(UserComics.Select(c => c.Publisher));

            // Conta autori unici
            HashSet<string> authors = new HashSet<string>();
            foreach (var comic in UserComics)
            {
                authors.Add(comic.Author);
            }
            AuthorsCount = authors.Count;
        }

        public IActionResult OnPostAddComic(Comic newComic)
        {
            if (!ModelState.IsValid)
            {
                IsError = true;
                StatusMessage = "Errore nella validazione dei dati del fumetto.";
                return RedirectToPage();
            }

            // In un'app reale, salveresti il fumetto nel database
            // Per ora, aggiungiamo un messaggio di successo e torniamo alla pagina

            newComic.Id = Guid.NewGuid().ToString();
            newComic.UserId = _userManager.GetUserId(User);
            newComic.CreatedAt = DateTime.Now;
            newComic.UpdatedAt = DateTime.Now;

            IsError = false;
            StatusMessage = "Fumetto aggiunto con successo!";
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateComic(Comic updatedComic)
        {
            if (!ModelState.IsValid)
            {
                IsError = true;
                StatusMessage = "Errore nella validazione dei dati del fumetto.";
                return RedirectToPage();
            }

            // In un'app reale, aggiorneresti il fumetto nel database
            updatedComic.UpdatedAt = DateTime.Now;

            IsError = false;
            StatusMessage = "Fumetto aggiornato con successo!";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteComic(string comicId)
        {
            if (string.IsNullOrEmpty(comicId))
            {
                IsError = true;
                StatusMessage = "ID fumetto non valido.";
                return RedirectToPage();
            }

            // In un'app reale, elimineresti il fumetto dal database

            IsError = false;
            StatusMessage = "Fumetto rimosso dalla collezione.";
            return RedirectToPage();
        }

        private void CreateSampleComics(string userId)
        {
            // Creiamo alcuni fumetti di esempio per la demo
            UserComics = new List<Comic>
            {
                new Comic
                {
                    Id = "1",
                    Title = "Batman: Anno Uno",
                    Series = "Batman",
                    IssueNumber = 1,
                    Author = "Frank Miller",
                    Publisher = "DC Comics",
                    PublicationDate = new DateTime(1987, 2, 1),
                    PageCount = 48,
                    CoverImage = "https://www.dccomics.com/sites/default/files/styles/covers192x291/public/comic-covers/2018/06/batman_v1_404_5b1711c8e1d8c2.26538074.jpg",
                    Description = "La storia delle origini di Batman rivisitata da Frank Miller.",
                    Notes = "Prima edizione, ottime condizioni",
                    UserId = userId
                },
                new Comic
                {
                    Id = "2",
                    Title = "Il ritorno del Cavaliere Oscuro",
                    Series = "Batman",
                    IssueNumber = 1,
                    Author = "Frank Miller",
                    Publisher = "DC Comics",
                    PublicationDate = new DateTime(1986, 2, 1),
                    PageCount = 52,
                    CoverImage = "https://m.media-amazon.com/images/I/91K96aHEXOL._SY466_.jpg",
                    Description = "Un Batman invecchiato torna dall'ombra per affrontare una nuova ondata di crimini.",
                    Notes = "Leggeri segni di usura",
                    UserId = userId
                },
                new Comic
                {
                    Id = "3",
                    Title = "Civil War",
                    Series = "Civil War",
                    IssueNumber = 1,
                    Author = "Mark Millar",
                    Publisher = "Marvel",
                    PublicationDate = new DateTime(2006, 7, 1),
                    PageCount = 40,
                    CoverImage = "https://cdn.marvel.com/u/prod/marvel/i/mg/c/10/51ed8a1dce6fb/clean.jpg",
                    Description = "Una legge di registrazione dei supereroi divide gli Avengers.",
                    Notes = "Nuova edizione",
                    UserId = userId
                },
                new Comic
                {
                    Id = "4",
                    Title = "Watchmen",
                    Series = "Watchmen",
                    IssueNumber = 1,
                    Author = "Alan Moore",
                    Publisher = "DC Comics",
                    PublicationDate = new DateTime(1986, 9, 1),
                    PageCount = 36,
                    CoverImage = "https://m.media-amazon.com/images/I/91M4h1+IFML._SY466_.jpg",
                    Description = "In un mondo alternativo, i supereroi sono considerati fuorilegge.",
                    Notes = "Edizione speciale",
                    UserId = userId
                },
                new Comic
                {
                    Id = "5",
                    Title = "L'Ultima Caccia di Kraven",
                    Series = "Spider-Man",
                    IssueNumber = 294,
                    Author = "J.M. DeMatteis",
                    Publisher = "Marvel",
                    PublicationDate = new DateTime(1987, 10, 1),
                    PageCount = 32,
                    CoverImage = "https://i.annihil.us/u/prod/marvel/i/mg/2/b0/5cd9ca8e99580/clean.jpg",
                    Description = "Kraven il cacciatore affronta Spider-Man per l'ultima volta.",
                    Notes = "Buone condizioni",
                    UserId = userId
                },
                new Comic
                {
                    Id = "6",
                    Title = "Preacher: Finché il mondo non finisca",
                    Series = "Preacher",
                    IssueNumber = 1,
                    Author = "Garth Ennis",
                    Publisher = "Vertigo",
                    PublicationDate = new DateTime(1995, 4, 1),
                    PageCount = 48,
                    CoverImage = "https://m.media-amazon.com/images/I/91dXpS+QmNL._SY466_.jpg",
                    Description = "Un predicatore ottiene un potere soprannaturale e parte alla ricerca di Dio.",
                    Notes = "Edizione italiana",
                    UserId = userId
                }
            };
        }
    }
}