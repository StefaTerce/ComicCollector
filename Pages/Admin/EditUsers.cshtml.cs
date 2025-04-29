using System.ComponentModel.DataAnnotations;
using ComicCollector.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ComicCollector.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditUserModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public UserEditViewModel UserEdit { get; set; }

        public List<string> AvailableRoles { get; set; }
        public bool IsAdminUser { get; set; }

        public class UserEditViewModel
        {
            public string Id { get; set; }

            [Required(ErrorMessage = "L'email è richiesta")]
            [EmailAddress(ErrorMessage = "L'email non è valida")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Il nome è richiesto")]
            [Display(Name = "Nome")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Il cognome è richiesto")]
            [Display(Name = "Cognome")]
            public string LastName { get; set; }

            public List<string> Roles { get; set; } = new List<string>();
        }

        [BindProperty]
        public List<string> SelectedRoles { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            UserEdit = new UserEditViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            };

            // Get all available roles
            AvailableRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            // Check if this is the admin user
            IsAdminUser = await _userManager.IsInRoleAsync(user, "Admin") && user.Email == "admin@comicscollector.com";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Re-populate available roles if model is invalid
                AvailableRoles = _roleManager.Roles.Select(r => r.Name).ToList();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(UserEdit.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if this is the default admin user
            IsAdminUser = await _userManager.IsInRoleAsync(user, "Admin") && user.Email == "admin@comicscollector.com";

            // Update user details
            user.Email = UserEdit.Email;
            user.UserName = UserEdit.Email; // Keep username and email in sync
            user.FirstName = UserEdit.FirstName;
            user.LastName = UserEdit.LastName;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                AvailableRoles = _roleManager.Roles.Select(r => r.Name).ToList();
                return Page();
            }

            // Update roles except for the admin user
            if (!IsAdminUser)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove roles that are not selected
                foreach (var role in currentRoles)
                {
                    if (!SelectedRoles.Contains(role))
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }
                }

                // Add selected roles that the user doesn't have
                foreach (var role in SelectedRoles)
                {
                    if (!currentRoles.Contains(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            TempData["StatusMessage"] = "Utente aggiornato con successo.";
            return RedirectToPage("./UserManagement");
        }
    }
}