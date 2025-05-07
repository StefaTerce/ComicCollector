using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using ComicCollector.Models;
using System.Threading.Tasks;
using System.Security.Claims; // Added for ClaimTypes

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

                if (user != null)
                {
                    // Se l'utente è admin, reindirizza alla dashboard admin
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToPage("/Admin/Dashboard");
                    }

                    // Altrimenti, reindirizza alla collezione personale
                    return RedirectToPage("/Collection/Index");
                }
                else
                {
                    // User is signed in according to SignInManager, but GetUserAsync returned null.
                    // This indicates an issue, perhaps the user was deleted from the database.
                    // Log this anomaly and sign the user out to clear the invalid authentication cookie.
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    _logger.LogWarning("User (ID: {UserId}) is signed in, but GetUserAsync returned null. Signing out the user.", userId);
                    await _signInManager.SignOutAsync();
                    // Redirect to the Index page, which will now show the non-authenticated view.
                    return RedirectToPage("/Index");
                }
            }

            // Se l'utente non è autenticato, mostra semplicemente la pagina Index
            return Page();
        }
    }
}