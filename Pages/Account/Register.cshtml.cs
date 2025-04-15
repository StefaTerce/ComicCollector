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
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Errore nella validazione del modello. Assicurati di aver compilato tutti i campi correttamente.";
                return Page();
            }

            try
            {
                NewUser.Role = "UTENTE";
                _context.Users.Add(NewUser);
                await _context.SaveChangesAsync();

                // Automatic login after registration
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