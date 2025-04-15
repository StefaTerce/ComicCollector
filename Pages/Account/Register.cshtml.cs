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
            // Imposta il valore di default del campo Role
            NewUser.Role = "UTENTE";

            try
            {
                // Salva il nuovo utente nel database
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