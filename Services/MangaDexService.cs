using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using ComicCollector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // For IOptionsMonitor

namespace ComicCollector.Services
{
    public class MangaDexService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MangaDexService> _logger;
        private readonly IOptionsMonitor<ApiKeySettings> _apiKeySettings;
        private readonly string _baseUrl = "https://api.mangadex.org";
        private readonly string _coverBaseUrl = "https://uploads.mangadex.org/covers"; // Corretto
        private string? _apiKey => _apiKeySettings.CurrentValue.MangaDexApiKey; // Use MangaDexApiKey

        public MangaDexService(HttpClient httpClient, ILogger<MangaDexService> logger, IOptionsMonitor<ApiKeySettings> apiKeySettings)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ComicCollectorApp/1.0 (+http://localhost; StefaTerce)");
            _logger = logger;
            _apiKeySettings = apiKeySettings;
        }

        private string GetPreferredText(MangaDexMultiLangText multiLangText, string preferredLang = "en")
        {
            if (multiLangText == null) return "N/A";
            if (multiLangText.TryGetValue(preferredLang, out var text) && !string.IsNullOrWhiteSpace(text)) return text;
            return multiLangText.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "N/A";
        }

        public async Task<List<MangaViewModel>> SearchMangaAsync(MangaDexSearchQuery searchQuery)
        {
            var mangaViewModels = new List<MangaViewModel>();
            try
            {
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                if (!string.IsNullOrWhiteSpace(searchQuery.Title)) queryParams["title"] = searchQuery.Title;
                if (searchQuery.Year.HasValue) queryParams["year"] = searchQuery.Year.ToString();
                if (!string.IsNullOrWhiteSpace(searchQuery.Status)) queryParams["status[]"] = searchQuery.Status;

                queryParams["limit"] = searchQuery.Limit.ToString();
                queryParams["offset"] = searchQuery.Offset.ToString();

                // Gestione includes
                var includesList = new List<string>();
                if (!string.IsNullOrEmpty(searchQuery.Includes))
                {
                    includesList.AddRange(searchQuery.Includes.Split(',').Select(s => s.Trim()));
                }
                // Assicura che cover_art sia sempre incluso se non già presente
                if (!includesList.Contains("cover_art")) includesList.Add("cover_art");
                if (!includesList.Contains("author")) includesList.Add("author");
                if (!includesList.Contains("artist")) includesList.Add("artist");

                foreach (var include in includesList.Distinct())
                {
                    queryParams.Add("includes[]", include);
                }

                if (!string.IsNullOrWhiteSpace(searchQuery.OrderCreatedAt)) queryParams["order[createdAt]"] = searchQuery.OrderCreatedAt;
                // Aggiungi altri ordini se necessari, es: queryParams["order[relevance]"] = "desc";


                string requestUrl = $"{_baseUrl}/manga?{queryParams.ToString()}";
                _logger.LogInformation($"Requesting MangaDex API: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"MangaDex API request failed. Status: {response.StatusCode}, URL: {requestUrl}, Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                    return mangaViewModels;
                }

                var mangaDexResponse = JsonSerializer.Deserialize<MangaDexSearchResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (mangaDexResponse?.Result != "ok" || mangaDexResponse.Data == null)
                {
                    _logger.LogError($"MangaDex API returned an error or no data. Result: '{mangaDexResponse?.Result}', Data Count: {mangaDexResponse?.Data?.Count}. URL: {requestUrl}");
                    return mangaViewModels;
                }

                foreach (var mangaData in mangaDexResponse.Data)
                {
                    var attributes = mangaData.Attributes;
                    if (attributes == null)
                    {
                        _logger.LogWarning($"MangaDex manga data (ID: {mangaData.Id}) has null attributes. Skipping.");
                        continue;
                    }

                    string title = GetPreferredText(attributes.Title);
                    string description = GetPreferredText(attributes.Description);

                    string coverArtId = mangaData.Relationships?.FirstOrDefault(r => r.Type == "cover_art")?.Id;
                    string coverImageUrl = null;
                    if (!string.IsNullOrEmpty(coverArtId))
                    {
                        coverImageUrl = await GetCoverUrlAsync(mangaData.Id, coverArtId);
                    }
                    else
                    {
                        _logger.LogWarning($"MangaDex - Manga ID: {mangaData.Id}, Title: {title} - No cover_art relationship found or ID is null.");
                    }

                    _logger.LogInformation($"MangaDex - Manga ID: {mangaData.Id}, Title: {title}, Determined CoverImageURL: {coverImageUrl ?? "null"}");

                    var authors = mangaData.Relationships?.Where(r => r.Type == "author" && r.Attributes?.Name != null).Select(r => r.Attributes.Name);
                    var artists = mangaData.Relationships?.Where(r => r.Type == "artist" && r.Attributes?.Name != null).Select(r => r.Attributes.Name);

                    string authorNames = "Unknown";
                    var authorList = new List<string>();
                    if (authors != null && authors.Any()) authorList.AddRange(authors.Distinct());
                    if (artists != null && artists.Any()) authorList.AddRange(artists.Distinct()); // Aggiungi artisti, verranno uniti e resi unici
                    if (authorList.Any()) authorNames = string.Join(", ", authorList.Distinct());


                    mangaViewModels.Add(new MangaViewModel
                    {
                        Id = mangaData.Id,
                        Title = title,
                        Description = SanitizeHtml(description),
                        CoverImageUrl = coverImageUrl,
                        Author = authorNames,
                        Year = attributes.Year,
                        Status = attributes.Status,
                        ContentRating = attributes.ContentRating,
                        Source = "MangaDex",
                        PublicationDateForSort = attributes.Year.HasValue ? new DateTime(attributes.Year.Value, 1, 1) : (attributes.CreatedAt != DateTime.MinValue ? attributes.CreatedAt : DateTime.MinValue)
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException while searching manga on MangaDex. URL: {RequestUrl}", $"{_baseUrl}/manga?{HttpUtility.ParseQueryString(string.Empty)}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JsonException while deserializing MangaDex response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error searching manga on MangaDex.");
            }
            return mangaViewModels;
        }

        public async Task<string> GetCoverUrlAsync(string mangaId, string coverArtRelationshipId)
        {
            if (string.IsNullOrEmpty(mangaId) || string.IsNullOrEmpty(coverArtRelationshipId))
            {
                _logger.LogWarning("GetCoverUrlAsync called with null/empty mangaId or coverArtRelationshipId.");
                return null;
            }

            try
            {
                string requestUrl = $"{_baseUrl}/cover/{coverArtRelationshipId}";
                _logger.LogInformation($"Requesting MangaDex Cover API: {requestUrl}");
                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var coverResponse = JsonSerializer.Deserialize<MangaDexCoverArtResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (coverResponse?.Result == "ok" && coverResponse.Data?.Attributes?.FileName != null)
                    {
                        string fileName = coverResponse.Data.Attributes.FileName;
                        // Costruisci l'URL completo della copertina.
                        // Formato: https://uploads.mangadex.org/covers/{manga_id}/{cover_filename}
                        // Potrebbe essere necessario scegliere una dimensione specifica, es. .512.jpg o .256.jpg
                        // Per ora usiamo il filename diretto.
                        string finalCoverUrl = $"{_coverBaseUrl}/{mangaId}/{fileName}";
                        _logger.LogInformation($"MangaDex Cover API Success - MangaID: {mangaId}, Cover Filename: {fileName}, Final URL: {finalCoverUrl}");
                        return finalCoverUrl;
                    }
                    else
                    {
                        _logger.LogWarning($"MangaDex Cover API did not return 'ok' or filename was null. Result: {coverResponse?.Result}, Filename: {coverResponse?.Data?.Attributes?.FileName}, URL: {requestUrl}");
                    }
                }
                else
                {
                    _logger.LogError($"MangaDex Cover API request failed. Status: {response.StatusCode}, URL: {requestUrl}, Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException while fetching MangaDex cover for MangaID {MangaID}, CoverRelationshipID {CoverID}", mangaId, coverArtRelationshipId);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JsonException while deserializing MangaDex cover response for MangaID {MangaID}, CoverRelationshipID {CoverID}", mangaId, coverArtRelationshipId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error fetching MangaDex cover for MangaID {MangaID}, CoverRelationshipID {CoverID}", mangaId, coverArtRelationshipId);
            }
            return null;
        }

        private string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }
    }
}