using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims; // Per User.Identity.Name

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
            // Formato richiesto: YYYY-MM-DD HH:MM:SS
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string GetSessionUserName()
        {
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            return string.IsNullOrEmpty(userName) ? "StefaTerce" : userName; // Default se non loggato o non trovato
        }

        public string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }


        public void LogSessionInfo(string pageName = null)
        {
            string logMessage = $"Session Info - Page: {(string.IsNullOrEmpty(pageName) ? "N/A" : pageName)} | UTC Time: {GetCurrentUtcDateTime()} | User: {GetSessionUserName()}";
            if (!string.IsNullOrEmpty(GetCurrentUserId()))
            {
                logMessage += $" (ID: {GetCurrentUserId()})";
            }
            _logger.LogInformation(logMessage);
        }
    }
}