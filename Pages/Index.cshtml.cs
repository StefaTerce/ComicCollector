using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using ComicCollector.Models;
using System.Threading.Tasks;

namespace ComicCollector.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            ILogger<IndexModel> logger,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (_signInManager.IsSignedIn(User))
            {
                var user = await _userManager.GetUserAsync(User);

                // Se l'utente è admin, reindirizza alla dashboard admin
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToPage("/Admin/Dashboard");
                }

                // Altrimenti, reindirizza alla collezione personale
                return RedirectToPage("/Collection/Index");
            }

            // Se l'utente non è autenticato, mostra semplicemente la pagina Index
            return Page();
        }
    }
}