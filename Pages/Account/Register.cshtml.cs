using ComicCollector.Data;
using ComicCollector.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            NewUser.Role = "UTENTE";
            _context.Users.Add(NewUser);
            _context.SaveChanges();

            return RedirectToPage("/Account/Login");
        }
    }
}