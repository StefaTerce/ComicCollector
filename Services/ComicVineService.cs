﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using ComicCollector.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Microsoft.Extensions.Options; // For IOptionsMonitor

namespace ComicCollector.Services
{
    public class ComicVineService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ComicVineService> _logger;
        private readonly IOptionsMonitor<ApiKeySettings> _apiKeySettings;
        private string? _apiKey => _apiKeySettings.CurrentValue.ComicVineApiKey;
        private readonly string _baseUrl = "https://comicvine.gamespot.com/api";

        public ComicVineService(HttpClient httpClient, ILogger<ComicVineService> logger, IOptionsMonitor<ApiKeySettings> apiKeySettings)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ComicCollectorApp/1.0 (+http://localhost; StefaTerce)");
            _logger = logger;
            _apiKeySettings = apiKeySettings;
        }

        private DateTime? ParseComicVineDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;
            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }
            if (dateStr.EndsWith("-00"))
            {
                if (DateTime.TryParseExact(dateStr.Replace("-00", "-01"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    return parsedDate;
                }
            }
            if (dateStr.Length == 4 && int.TryParse(dateStr, out int year))
            {
                return new DateTime(year, 1, 1);
            }
            _logger.LogWarning($"Could not parse ComicVine date: {dateStr}. It will be set to DateTime.MinValue or null if handled by caller.");
            return null;
        }

        public async Task<(List<Comic> Comics, int TotalResults)> SearchComicsAsync(ComicVineSearchQuery searchQuery)
        {
            var comics = new List<Comic>();
            int totalResults = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    _logger.LogWarning("ComicVine API key is not configured.");
                    return (comics, totalResults);
                }

                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["api_key"] = _apiKey;
                queryParams["format"] = "json";
                if (!string.IsNullOrWhiteSpace(searchQuery.Query)) queryParams["query"] = searchQuery.Query;
                queryParams["resources"] = searchQuery.Resources;
                queryParams["limit"] = searchQuery.Limit.ToString();
                queryParams["page"] = searchQuery.Page.ToString();
                queryParams["field_list"] = searchQuery.FieldList;
                if (!string.IsNullOrWhiteSpace(searchQuery.Sort)) queryParams["sort"] = searchQuery.Sort;

                string requestUrl = $"{_baseUrl}/search?{queryParams.ToString()}";
                _logger.LogInformation($"Requesting ComicVine API: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"ComicVine API request failed. Status: {response.StatusCode}, URL: {requestUrl}, Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                    return (comics, totalResults);
                }

                var comicVineResponse = JsonSerializer.Deserialize<ComicVineResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (comicVineResponse?.Error != "OK" || comicVineResponse.Results == null)
                {
                    _logger.LogError($"ComicVine API returned an error or no results. Error: '{comicVineResponse?.Error}', Results Count: {comicVineResponse?.Results?.Count}. URL: {requestUrl}");
                    return (comics, comicVineResponse?.NumberOfTotalResults ?? 0);
                }

                totalResults = comicVineResponse.NumberOfTotalResults;

                foreach (var issue in comicVineResponse.Results)
                {
                    string authorNames = "Unknown";
                    if (issue.PersonCredits != null && issue.PersonCredits.Any())
                    {
                        var writers = issue.PersonCredits.Where(pc => pc.Role?.ToLower().Contains("writer") == true).Select(pc => pc.Name);
                        if (writers.Any()) authorNames = string.Join(", ", writers);
                        else authorNames = string.Join(", ", issue.PersonCredits.Take(2).Select(pc => pc.Name));
                    }

                    int? issueNumberParsed = null;
                    if (!string.IsNullOrEmpty(issue.IssueNumber) && int.TryParse(issue.IssueNumber, out int num)) issueNumberParsed = num;

                    string imageUrl = issue.Image?.SmallUrl ?? issue.Image?.ThumbUrl ?? issue.Image?.IconUrl;
                    if (string.IsNullOrEmpty(imageUrl) && issue.Image?.OriginalUrl != null)
                    {
                        _logger.LogWarning($"ComicVine - Title: {issue.Name}, No small/thumb/icon image. Original URL exists: {issue.Image.OriginalUrl} but might be too large.");
                    }
                    _logger.LogInformation($"ComicVine - Title: '{issue.Name}', Issue ID: {issue.Id}, Attempted Image URL: '{imageUrl}' (From Small: '{issue.Image?.SmallUrl}', Thumb: '{issue.Image?.ThumbUrl}', Icon: '{issue.Image?.IconUrl}', Original: '{issue.Image?.OriginalUrl}')");

                    comics.Add(new Comic
                    {
                        SourceId = issue.Id.ToString(),
                        Title = issue.Name ?? "N/A",
                        Series = issue.Volume?.Name ?? "N/A",
                        IssueNumber = issueNumberParsed,
                        Author = authorNames,
                        Publisher = issue.Volume?.Name != null ? GetPublisherFromVolume(issue.Volume.Name) : "Unknown",
                        PublicationDate = ParseComicVineDate(issue.CoverDate ?? issue.StoreDate) ?? DateTime.MinValue,
                        CoverImage = imageUrl,
                        Description = SanitizeHtml(issue.Description),
                        Source = "ComicVine"
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException while searching comics on ComicVine. URL: {RequestUrl}", $"{_baseUrl}/search?{HttpUtility.ParseQueryString(string.Empty)}");
                return (new List<Comic>(), 0);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JsonException while deserializing ComicVine response.");
                return (new List<Comic>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error searching comics on ComicVine.");
                return (new List<Comic>(), 0);
            }
            return (comics, totalResults);
        }

        private string GetPublisherFromVolume(string volumeName)
        {
            if (string.IsNullOrEmpty(volumeName)) return "Unknown";
            if (volumeName.ToLower().Contains("marvel")) return "Marvel Comics";
            if (volumeName.ToLower().Contains("dc comics") || volumeName.ToLower().Contains("batman") || volumeName.ToLower().Contains("superman")) return "DC Comics";
            if (volumeName.ToLower().Contains("image")) return "Image Comics";
            return "Unknown";
        }

        private string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }
    }
}