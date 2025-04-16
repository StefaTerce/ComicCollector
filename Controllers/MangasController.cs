using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Data;
using ComicCollector.Models;
using ComicCollector.Services;
using Microsoft.AspNetCore.Authorization;

namespace ComicCollector.Controllers
{
    [Authorize]
    public class MangasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MyAnimeListService _mangaService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MangasController(
            ApplicationDbContext context,
            MyAnimeListService mangaService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _mangaService = mangaService;
            _userManager = userManager;
        }

        // GET: Mangas
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var mangas = await _context.Mangas
                .Where(m => m.UserId == user.Id)
                .ToListAsync();

            return View(mangas);
        }

        // GET: Mangas/Search
        public IActionResult Search()
        {
            return View();
        }

        // POST: Mangas/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return View(new List<Manga>());
            }

            var results = await _mangaService.SearchManga(searchTerm);
            return View(results);
        }

        // GET: Mangas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manga = await _context.Mangas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manga == null)
            {
                return NotFound();
            }

            return View(manga);
        }

        // GET: Mangas/Add/apiId
        public async Task<IActionResult> Add(string apiId)
        {
            if (string.IsNullOrEmpty(apiId) || !int.TryParse(apiId, out int malId))
            {
                return NotFound();
            }

            var mangaDetails = await _mangaService.GetMangaDetails(malId);

            if (mangaDetails == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            mangaDetails.UserId = user.Id;

            _context.Mangas.Add(mangaDetails);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Mangas/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var manga = await _context.Mangas
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (manga == null)
            {
                return NotFound();
            }

            return View(manga);
        }

        // POST: Mangas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var manga = await _context.Mangas
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (manga != null)
            {
                _context.Mangas.Remove(manga);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Mangas/ToggleFavorite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var manga = await _context.Mangas
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (manga == null)
            {
                return NotFound();
            }

            manga.IsFavorite = !manga.IsFavorite;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}