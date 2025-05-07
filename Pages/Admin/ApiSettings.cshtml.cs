// filepath: c:\Users\snipe\OneDrive\Desktop\StefaTerce\ComicCollector\Pages\Admin\ApiSettings.cshtml.cs
using ComicCollector.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment

namespace ComicCollector.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ApiSettingsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly string _appSettingsPath;

        [BindProperty]
        public ApiKeySettings ApiKeySettings { get; set; }

        public ApiSettingsModel(IOptionsMonitor<ApiKeySettings> apiKeyOptions, IConfiguration configuration, IWebHostEnvironment env)
        {
            ApiKeySettings = apiKeyOptions.CurrentValue ?? new ApiKeySettings();
            _configuration = configuration;
            _env = env;
            _appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        }

        public void OnGet()
        {
            // ApiKeySettings is already populated by the constructor
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update appsettings.json
            // Note: This is a simplified approach. In a production environment, consider more robust configuration management.
            // Also, be aware that modifying appsettings.json at runtime might require an application restart for changes to be fully effective
            // across all services, depending on how they consume configuration. IOptionsMonitor helps with this.

            var appSettingsJson = await System.IO.File.ReadAllTextAsync(_appSettingsPath);
            var appSettings = JsonSerializer.Deserialize<JsonElement>(appSettingsJson);

            using var memoryStream = new MemoryStream(); // Declare MemoryStream here
            using (var newAppSettings = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true })) // Pass it to the writer
            {
                newAppSettings.WriteStartObject();

                bool apiKeysSectionExists = false;
                foreach (var property in appSettings.EnumerateObject())
                {
                    if (property.Name == "ApiKeys")
                    {
                        apiKeysSectionExists = true;
                        newAppSettings.WritePropertyName("ApiKeys");
                        newAppSettings.WriteStartObject();
                        newAppSettings.WriteString("ComicVineApiKey", ApiKeySettings.ComicVineApiKey);
                        newAppSettings.WriteString("MangaDexApiKey", ApiKeySettings.MangaDexApiKey);
                        newAppSettings.WriteString("GeminiApiKey", ApiKeySettings.GeminiApiKey); // Added Gemini
                        newAppSettings.WriteEndObject();
                    }
                    else
                    {
                        property.WriteTo(newAppSettings);
                    }
                }

                if (!apiKeysSectionExists)
                {
                    newAppSettings.WritePropertyName("ApiKeys");
                    newAppSettings.WriteStartObject();
                    newAppSettings.WriteString("ComicVineApiKey", ApiKeySettings.ComicVineApiKey);
                    newAppSettings.WriteString("MangaDexApiKey", ApiKeySettings.MangaDexApiKey);
                    newAppSettings.WriteString("GeminiApiKey", ApiKeySettings.GeminiApiKey); // Added Gemini
                    newAppSettings.WriteEndObject();
                }

                newAppSettings.WriteEndObject();
                // Flush is called automatically by Dispose when exiting the using block for Utf8JsonWriter
            } // Utf8JsonWriter is disposed here, and its contents are flushed to memoryStream

            await System.IO.File.WriteAllBytesAsync(_appSettingsPath, memoryStream.ToArray());
            
            // Trigger configuration reload
            ((IConfigurationRoot)_configuration).Reload();

            TempData["StatusMessage"] = "Impostazioni API aggiornate con successo.";
            return RedirectToPage();
        }
    }
}
