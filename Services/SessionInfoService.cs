using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ComicCollector.Services
{
    public class SessionInfoService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionInfoService> _logger;

        public SessionInfoService(IHttpContextAccessor httpContextAccessor, ILogger<SessionInfoService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string GetCurrentUtcDateTime()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string GetSessionUserName()
        {
            // Ottieni l'utente corrente o restituisci un valore predefinito
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "StefaTerce";
        }

        public void LogSessionInfo()
        {
            _logger.LogInformation($"Current Date and Time (UTC - YYYY-MM-DD HH:MM:SS formatted): {GetCurrentUtcDateTime()}");
            _logger.LogInformation($"Current User's Login: {GetSessionUserName()}");
        }
    }
}