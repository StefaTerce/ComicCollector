using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using ComicCollector.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using ComicCollector.Services;

namespace ComicCollector.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class UserManagementModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserManagementModel> _logger;
        private readonly SessionInfoService _sessionInfo;

        public UserManager<ApplicationUser> UserManager => _userManager;
        public List<ApplicationUser> Users { get; set; }

        // Dizionario per memorizzare le date di registrazione simulate per utente
        public Dictionary<string, string> UserRegistrationDates { get; set; } = new Dictionary<string, string>();

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public bool IsError { get; set; }

        public UserManagementModel(
            UserManager<ApplicationUser> userManager,
            ILogger<UserManagementModel> logger,
            SessionInfoService sessionInfo)
        {
            _userManager = userManager;
            _logger = logger;
            _sessionInfo = sessionInfo;
        }

        public void OnGet()
        {
            // Registra informazioni di sessione
            _sessionInfo.LogSessionInfo();

            Users = _userManager.Users.ToList();

            // Genera date di registrazione simulate
            UserRegistrationDates = new Dictionary<string, string>();
            Random random = new Random();
            foreach (var user in Users)
            {
                // Simula una data di registrazione negli ultimi 90 giorni
                DateTime registrationDate = DateTime.Now.AddDays(-random.Next(1, 90));
                UserRegistrationDates[user.Id] = registrationDate.ToString("dd/MM/yyyy");
            }
        }

        public async Task<IActionResult> OnPostEditUserAsync(string userId, string firstName, string lastName, string email, bool isAdmin)
        {
            if (string.IsNullOrEmpty(userId))
            {
                IsError = true;
                StatusMessage = "ID utente non valido";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                IsError = true;
                StatusMessage = "Utente non trovato";
                return RedirectToPage();
            }

            // Aggiorna i dettagli dell'utente
            user.FirstName = firstName;
            user.LastName = lastName;

            // Se l'email è cambiata, aggiorna anche quella
            if (user.Email != email)
            {
                user.UserName = email;
                user.Email = email;
                user.NormalizedEmail = email.ToUpper();
                user.NormalizedUserName = email.ToUpper();
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                IsError = true;
                StatusMessage = "Errore durante l'aggiornamento dell'utente: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToPage();
            }

            // Controlla se il ruolo è cambiato
            bool currentlyAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (currentlyAdmin && !isAdmin)
            {
                // Rimuovi ruolo Admin
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                await _userManager.AddToRoleAsync(user, "User");
            }
            else if (!currentlyAdmin && isAdmin)
            {
                // Aggiungi ruolo Admin
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            IsError = false;
            StatusMessage = "Utente aggiornato con successo";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                IsError = true;
                StatusMessage = "ID utente non valido";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                IsError = true;
                StatusMessage = "Utente non trovato";
                return RedirectToPage();
            }

            // Impedisci la cancellazione dell'utente corrente
            if (user.Id == _userManager.GetUserId(User))
            {
                IsError = true;
                StatusMessage = "Non puoi eliminare il tuo stesso account";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                IsError = true;
                StatusMessage = "Errore durante l'eliminazione dell'utente: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToPage();
            }

            IsError = false;
            StatusMessage = "Utente eliminato con successo";
            return RedirectToPage();
        }
    }
}