using System.Threading.Tasks;
using ComicCollector.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ComicCollector.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public void OnGet()
        {
            // Non eseguire alcuna azione
        }

        public async Task<IActionResult> OnPost()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Utente disconnesso.");

            // Registra informazioni sulla sessione terminata
            _logger.LogInformation($"Sessione terminata: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            _logger.LogInformation($"Utente: StefaTerceil");

            return RedirectToPage("/Index");
        }
    }
}