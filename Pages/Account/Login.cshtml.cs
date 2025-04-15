using ComicCollector.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ComicCollector.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }
        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnPost()
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == Username && u.Password == Password);

            if (user == null)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            if (user.Role == "ADMIN")
                return RedirectToPage("/Admin/Dashboard");

            return RedirectToPage("/User/Dashboard");
        }
    }
}