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
    public class ComicsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ComicVineService _comicService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ComicsController(
            ApplicationDbContext context,
            ComicVineService comicService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _comicService = comicService;
            _userManager = userManager;
        }

        // GET: Comics
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var comics = await _context.Comics
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            return View(comics);
        }

        // GET: Comics/Search
        public IActionResult Search()
        {
            return View();
        }

        // POST: Comics/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return View(new List<Comic>());
            }

            var results = await _comicService.SearchComics(searchTerm);
            return View(results);
        }

        // GET: Comics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comic = await _context.Comics
                .FirstOrDefaultAsync(m => m.Id == id);

            if (comic == null)
            {
                return NotFound();
            }

            return View(comic);
        }

        // GET: Comics/Add/apiId
        public async Task<IActionResult> Add(string apiId)
        {
            if (string.IsNullOrEmpty(apiId))
            {
                return NotFound();
            }

            var comicDetails = await _comicService.GetComicDetails(apiId);

            if (comicDetails == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            comicDetails.UserId = user.Id;

            _context.Comics.Add(comicDetails);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Comics/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var comic = await _context.Comics
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (comic == null)
            {
                return NotFound();
            }

            return View(comic);
        }

        // POST: Comics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var comic = await _context.Comics
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (comic != null)
            {
                _context.Comics.Remove(comic);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Comics/ToggleFavorite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var comic = await _context.Comics
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (comic == null)
            {
                return NotFound();
            }

            comic.IsFavorite = !comic.IsFavorite;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}