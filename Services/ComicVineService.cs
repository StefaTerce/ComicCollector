using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using ComicCollector.Models;
using Microsoft.Extensions.Configuration; // Potrebbe servire se l'API key fosse in appsettings
using Microsoft.Extensions.Logging;
using System.Globalization; // Per parsing date

namespace ComicCollector.Services
{
    public class ComicVineService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ComicVineService> _logger;
        private readonly string _apiKey = "b609648fe9073f7ac39915d338fce6f9bfac4971"; // API Key fornita
        private readonly string _baseUrl = "https://comicvine.gamespot.com/api";

        public ComicVineService(HttpClient httpClient, ILogger<ComicVineService> logger)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ComicCollectorApp/1.0"); // Buona pratica
            _logger = logger;
        }

        private DateTime? ParseComicVineDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;

            // Prova a parsare "YYYY-MM-DD"
            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }
            // Prova a parsare "YYYY-MM-00" come il primo del mese
            if (dateStr.EndsWith("-00"))
            {
                if (DateTime.TryParseExact(dateStr.Replace("-00", "-01"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    return parsedDate;
                }
            }
            // Prova a parsare solo l'anno "YYYY" come 1 Gennaio di quell'anno
            if (dateStr.Length == 4 && int.TryParse(dateStr, out int year))
            {
                return new DateTime(year, 1, 1);
            }

            _logger.LogWarning($"Could not parse ComicVine date: {dateStr}");
            return null;
        }

        public async Task<List<Comic>> SearchComicsAsync(ComicVineSearchQuery searchQuery)
        {
            var comics = new List<Comic>();
            try
            {
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["api_key"] = _apiKey;
                queryParams["format"] = "json";
                queryParams["query"] = searchQuery.Query;
                queryParams["resources"] = searchQuery.Resources; // "issue"
                queryParams["limit"] = searchQuery.Limit.ToString();
                queryParams["page"] = searchQuery.Page.ToString();
                queryParams["field_list"] = searchQuery.FieldList;

                string requestUrl = $"{_baseUrl}/search?{queryParams.ToString()}";
                _logger.LogInformation($"Requesting ComicVine: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"ComicVine API request failed with status {response.StatusCode}: {responseContent}");
                    return comics;
                }

                var comicVineResponse = JsonSerializer.Deserialize<ComicVineResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (comicVineResponse?.Error != "OK" || comicVineResponse.Results == null)
                {
                    _logger.LogError($"ComicVine API returned an error or no results: {comicVineResponse?.Error}");
                    return comics;
                }

                foreach (var issue in comicVineResponse.Results)
                {
                    string authorNames = "Unknown";
                    if (issue.PersonCredits != null && issue.PersonCredits.Any())
                    {
                        // Prioritizza "writer"
                        var writers = issue.PersonCredits.Where(pc => pc.Role?.ToLower().Contains("writer") == true).Select(pc => pc.Name);
                        if (writers.Any())
                        {
                            authorNames = string.Join(", ", writers);
                        }
                        else // Altrimenti prendi i primi ruoli disponibili
                        {
                            authorNames = string.Join(", ", issue.PersonCredits.Take(2).Select(pc => pc.Name));
                        }
                    }

                    int? issueNumberParsed = null;
                    if (!string.IsNullOrEmpty(issue.IssueNumber) && int.TryParse(issue.IssueNumber, out int num))
                    {
                        issueNumberParsed = num;
                    }

                    comics.Add(new Comic
                    {
                        SourceId = issue.Id.ToString(),
                        Title = issue.Name ?? "N/A",
                        Series = issue.Volume?.Name ?? "N/A",
                        IssueNumber = issueNumberParsed,
                        Author = authorNames,
                        Publisher = issue.Volume?.Name != null ? GetPublisherFromVolume(issue.Volume.Name) : "Unknown", // Estrazione semplificata
                        PublicationDate = ParseComicVineDate(issue.CoverDate ?? issue.StoreDate) ?? DateTime.MinValue,
                        CoverImage = issue.Image?.SmallUrl ?? issue.Image?.ThumbUrl,
                        Description = SanitizeHtml(issue.Description),
                        Source = "ComicVine"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching comics on ComicVine.");
            }
            return comics;
        }

        // Funzione di utilità per estrarre un possibile editore dal nome del volume/serie
        private string GetPublisherFromVolume(string volumeName)
        {
            // Questo è molto euristico e potrebbe non essere accurato.
            // ComicVine non fornisce l'editore direttamente nell'oggetto Issue/Volume in modo semplice.
            // Sarebbe necessaria una chiamata API separata all'oggetto Volume per ottenere l'editore.
            if (string.IsNullOrEmpty(volumeName)) return "Unknown";
            if (volumeName.ToLower().Contains("marvel")) return "Marvel Comics";
            if (volumeName.ToLower().Contains("dc comics") || volumeName.ToLower().Contains("batman") || volumeName.ToLower().Contains("superman")) return "DC Comics";
            if (volumeName.ToLower().Contains("image")) return "Image Comics";
            // Aggiungi altre logiche euristiche se necessario
            return "Unknown";
        }

        // Semplice sanificazione HTML (per descrizioni)
        private string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            // Rimuove i tag HTML in modo basico. Per una sanificazione completa, usare una libreria.
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }
    }
}