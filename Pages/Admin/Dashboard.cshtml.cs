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
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SessionInfoService _sessionInfo;

        public UserManager<ApplicationUser> UserManager => _userManager;

        public int TotalUsers { get; set; }
        public int TotalCollections { get; set; } = 0; // Simulato
        public int TotalComics { get; set; } = 0; // Simulato
        public List<ApplicationUser> RecentUsers { get; set; }
        public Dictionary<string, string> UserRegistrationDates { get; set; } = new Dictionary<string, string>();

        public DashboardModel(
            ILogger<DashboardModel> logger,
            UserManager<ApplicationUser> userManager,
            SessionInfoService sessionInfo)
        {
            _logger = logger;
            _userManager = userManager;
            _sessionInfo = sessionInfo;
        }

        public async Task OnGetAsync()
        {
            // Registra informazioni di sessione
            _sessionInfo.LogSessionInfo();

            // Ottieni il conteggio totale degli utenti
            TotalUsers = _userManager.Users.Count();

            // Ottieni gli utenti recenti
            RecentUsers = _userManager.Users.Take(5).ToList();

            // Simula date di registrazione per gli utenti
            UserRegistrationDates = new Dictionary<string, string>();
            foreach (var user in RecentUsers)
            {
                // Genera una data casuale negli ultimi 30 giorni
                var date = DateTime.Now.AddDays(-new Random().Next(1, 30));
                UserRegistrationDates[user.Id] = date.ToString("dd/MM/yyyy");
            }

            // Simula il numero di collezioni e fumetti
            TotalCollections = new Random().Next(10, 50);
            TotalComics = new Random().Next(100, 500);
        }
    }
}