using ComicCollector.Data;
using ComicCollector.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ComicCollector.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User NewUser { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            // Imposta il valore predefinito del campo Role
            if (string.IsNullOrEmpty(NewUser.Role))
            {
                NewUser.Role = "UTENTE";
            }

            // Verifica la validità del modello
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Errore nella validazione del modello. Campi non validi:";
                foreach (var entry in ModelState)
                {
                    if (entry.Value.Errors.Count > 0)
                    {
                        ErrorMessage += $"\nCampo: {entry.Key}, Errore: {entry.Value.Errors[0].ErrorMessage}";
                    }
                }
                return Page();
            }

            try
            {
                _context.Users.Add(NewUser);
                await _context.SaveChangesAsync();

                // Login automatico dopo la registrazione
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, NewUser.Username),
                    new Claim(ClaimTypes.Role, NewUser.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToPage("/Dashboard");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Errore durante la registrazione: " + ex.Message;
                return Page();
            }
        }
    }
}