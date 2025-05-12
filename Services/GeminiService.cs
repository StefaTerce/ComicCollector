using ComicCollector.Models;
using ComicCollector.Data; // Added for ApplicationDbContext
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // Added for ToListAsync
using System;
using System.Net.Http;
using System.Net.Http.Json; // Required for PostAsJsonAsync and ReadFromJsonAsync
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions; // Added for Regex
using System.Linq; // Added for LINQ methods
using System.Threading.Tasks;

namespace ComicCollector.Services
{
    public class RecommendedTitle
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class RecommendationResult
    {
        public List<RecommendedTitle> RecommendedComics { get; set; } = new List<RecommendedTitle>();
        public List<RecommendedTitle> RecommendedManga { get; set; } = new List<RecommendedTitle>();
    }

    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly IOptionsMonitor<ApiKeySettings> _apiKeySettings;
        private string? ApiKey => _apiKeySettings.CurrentValue.GeminiApiKey;

        // Hypothetical Gemini API Configuration
        private readonly string _geminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models"; // Example base URL
        private readonly string _geminiModelForText = "gemini-1.5-flash-latest:generateContent"; // Example model for text generation
        private readonly string _geminiModelForEnrichment = "gemini-1.5-flash-latest:generateContent"; // Can be the same or different

        public GeminiService(HttpClient httpClient, ILogger<GeminiService> logger, IOptionsMonitor<ApiKeySettings> apiKeySettings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKeySettings = apiKeySettings;
        }

        public async Task<string?> GetReviewSummaryAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                _logger.LogWarning("Gemini API key is not configured.");
                return null;
            }

            try
            {
                var requestUrl = $"{_geminiApiBaseUrl}/{_geminiModelForText}?key={ApiKey}";
                var requestPayload = new GeminiRequest { Contents = new[] { new Content { Parts = new[] { new Part { Text = prompt } } } } };

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestPayload);
                response.EnsureSuccessStatusCode();

                var geminiApiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                string? summary = geminiApiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(summary))
                {
                    _logger.LogWarning("Gemini returned an empty or invalid summary.");
                    return "Non è stato possibile recuperare il riepilogo al momento.";
                }

                // 1. Remove ALL asterisk characters from the summary.
                summary = summary.Replace("*", "");

                // 2. Trim leading/trailing whitespace (includes newlines) from the entire summary.
                summary = summary.Trim();

                // 3. Normalize multiple internal blank lines to a single blank line.
                // This regex looks for two or more consecutive newline sequences (allowing for whitespace on empty lines between them)
                // and replaces them with a single pair of newlines (effectively one blank line).
                if (!string.IsNullOrWhiteSpace(summary)) 
                {
                    summary = Regex.Replace(summary, @"(\r\n|\r|\n)(\s*(\r\n|\r|\n))+", $"{Environment.NewLine}{Environment.NewLine}");
                }
                
                // After all cleaning, check if the summary became empty or consists only of whitespace.
                if (string.IsNullOrWhiteSpace(summary))
                {
                    _logger.LogWarning("Gemini summary became empty after cleaning procedures.");
                    return "Non è stato possibile recuperare un riepilogo valido al momento.";
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review summary from Gemini.");
                return "Si è verificato un errore durante il recupero del riepilogo.";
            }
        }

        public async Task<Comic?> EnrichComicDataAsync(Comic comic)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                _logger.LogWarning("Gemini API key is not configured for data enrichment.");
                return comic;
            }

            bool needsEnrichment = string.IsNullOrWhiteSpace(comic.Author) ||
                                   string.IsNullOrWhiteSpace(comic.Publisher) ||
                                   comic.PublicationDate == DateTime.MinValue ||
                                   string.IsNullOrWhiteSpace(comic.Description);

            if (!needsEnrichment)
            {
                _logger.LogInformation($"No enrichment needed for comic '{comic.Title}'.");
                return comic;
            }

            try
            {
                string prompt = $"For the comic/manga titled '{comic.Title}'" +
                                (comic.IssueNumber.HasValue ? $" issue number {comic.IssueNumber}" : "") +
                                $" in the series '{comic.Series}':\n";

                if (string.IsNullOrWhiteSpace(comic.Author)) prompt += "- What are the names of the author(s)/writer(s)?\n";
                if (string.IsNullOrWhiteSpace(comic.Publisher)) prompt += "- Who is the publisher?\n";
                if (comic.PublicationDate == DateTime.MinValue) prompt += "- What is the publication date (YYYY-MM-DD)?\n";
                if (string.IsNullOrWhiteSpace(comic.Description)) prompt += "- Provide a brief plot description (2-3 sentences).\n";

                prompt += "If any information is not found, state 'Not found'. Respond in a structured format, for example: Author: [Name]. Publisher: [Name]. PublicationDate: [YYYY-MM-DD]. Description: [Text].";

                _logger.LogInformation($"Sending prompt to Gemini for data enrichment: {prompt}");

                var requestUrl = $"{_geminiApiBaseUrl}/{_geminiModelForEnrichment}?key={ApiKey}";
                var requestPayload = new GeminiRequest { Contents = new[] { new Content { Parts = new[] { new Part { Text = prompt } } } } };

                await Task.Delay(200); // Simulate network latency
                GeminiResponse? geminiApiResponse = SimulateGeminiEnrichmentResponse(comic);
                string? geminiResponseText = geminiApiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                _logger.LogInformation($"Gemini enrichment response received for '{comic.Title}': {geminiResponseText}");

                if (!string.IsNullOrWhiteSpace(geminiResponseText))
                {
                    ParseAndApplyEnrichment(comic, geminiResponseText);
                    comic.UpdatedAt = DateTime.UtcNow;
                }

                return comic;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enriching comic data from Gemini for '{comic.Title}'.");
                return comic;
            }
        }

        public async Task<RecommendationResult> GetRecommendationsAsync(List<Comic> userCollection, int comicCount, int mangaCount)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                _logger.LogWarning("Gemini API key is not configured. Cannot fetch recommendations.");
                return new RecommendationResult();
            }
            if (userCollection == null || !userCollection.Any())
            {
                return new RecommendationResult(); // Return empty if no collection
            }

            // Analyze collection for preferences
            var topAuthors = userCollection.Where(c => !string.IsNullOrEmpty(c.Author))
                                           .GroupBy(c => c.Author)
                                           .OrderByDescending(g => g.Count())
                                           .Select(g => g.Key)
                                           .Take(3) // Reduced for brevity in prompt
                                           .ToList();

            var topSeries = userCollection.Where(c => !string.IsNullOrEmpty(c.Series) && c.Series.ToLower() != "manga")
                                          .GroupBy(c => c.Series)
                                          .OrderByDescending(g => g.Count())
                                          .Select(g => g.Key)
                                          .Take(3) // Reduced for brevity in prompt
                                          .ToList();

            var promptBuilder = new StringBuilder("Based on a user who has shown interest in ");
            if (topAuthors.Any()) promptBuilder.Append($"authors like {string.Join(", ", topAuthors)}; ");
            if (topSeries.Any()) promptBuilder.Append($"comic series like {string.Join(", ", topSeries)}; ");
            
            promptBuilder.Append($"Please recommend {comicCount} new comic book titles (Western style, not manga) and {mangaCount} new manga titles they might enjoy. ");
            promptBuilder.Append("For each title, provide a brief 1-2 sentence reason why it's recommended based on the user's collection. ");
            promptBuilder.Append("Format the response as follows: Start with 'Recommended Comics:', then list each comic as '1. [Comic Title]: [Brief Description]'. Each on a new line. Then, on a new line, start with 'Recommended Manga:', followed by a similar numbered list for manga titles '1. [Manga Title]: [Brief Description]'.");

            string prompt = promptBuilder.ToString();
            _logger.LogInformation("Gemini Recommendation Prompt: {Prompt}", prompt);

            try
            {
                var requestUrl = $"{_geminiApiBaseUrl}/{_geminiModelForText}?key={ApiKey}";
                var requestPayload = new GeminiRequest { Contents = new[] { new Content { Parts = new[] { new Part { Text = prompt } } } } };

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestPayload);
                response.EnsureSuccessStatusCode();

                var geminiApiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                string? responseText = geminiApiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("Gemini recommendation response was empty or null.");
                    return new RecommendationResult();
                }

                return ParseRecommendations(responseText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recommendations from Gemini API.");
                return new RecommendationResult();
            }
        }

        private GeminiResponse SimulateGeminiTextResponse(string title, string? series, string? author)
        {
            // Simulate a plausible Gemini response structure with more positive/negative traits
            var simulatedText = $"Positive Traits:\n"
                                + "Incredibly captivating storyline for '{title}' that keeps readers hooked from start to finish.\n"
                                + "Deep and well-developed characters with relatable motivations and growth arcs.\n"
                                + "Stunning and detailed artwork that beautifully complements the narrative tone.\n"
                                + "Excellent world-building, creating a rich and immersive experience.";
            if (!string.IsNullOrWhiteSpace(series) && series.ToLower() != "n/a")
            {
                simulatedText += $"\nSeamlessly integrates into the broader '{series}' lore.";
            }
            simulatedText += "\n\nNegative Traits:\n"
                           + "The pacing can feel a bit rushed in the final chapters, leaving some subplots underdeveloped.\n"
                           + "Some secondary characters could have benefited from more screen time or development.\n"
                           + "Certain plot twists might be predictable for seasoned readers of the genre.";
            if (title.Contains("Space")) // Just an example to vary the simulated response
            {
                simulatedText += "\nThe depiction of space travel physics is not always accurate.";
            }
            simulatedText += "\n(This is a simulated summary from Gemini)";
            
            return new GeminiResponse
            {
                Candidates = new[]
                {
                    new Candidate
                    {
                        Content = new Content
                        {
                            Parts = new[] { new Part { Text = simulatedText } },
                            Role = "model"
                        }
                    }
                }
            };
        }

        private GeminiResponse SimulateGeminiEnrichmentResponse(Comic comicToEnrich)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrWhiteSpace(comicToEnrich.Author)) sb.AppendLine("Author: Jane Doe (Simulated)");
            if (string.IsNullOrWhiteSpace(comicToEnrich.Publisher)) sb.AppendLine("Publisher: Simulated Comics Inc.");
            if (comicToEnrich.PublicationDate == DateTime.MinValue) sb.AppendLine("PublicationDate: 2022-01-15 (Simulated)");
            if (string.IsNullOrWhiteSpace(comicToEnrich.Description)) sb.AppendLine("Description: A simulated tale of adventure and discovery, where heroes rise to meet unforeseen challenges. This comic explores themes of friendship and courage in a richly imagined world.");
            if (sb.Length == 0) sb.AppendLine("No missing data found to simulate enrichment for.");

            return new GeminiResponse
            {
                Candidates = new[]
                {
                    new Candidate
                    {
                        Content = new Content
                        {
                            Parts = new[] { new Part { Text = sb.ToString().Trim() } },
                            Role = "model"
                        }
                    }
                }
            };
        }

        private RecommendationResult ParseRecommendations(string responseText)
        {
            var result = new RecommendationResult();
            var lines = responseText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            bool comicsSection = false;
            bool mangaSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                if (trimmedLine.StartsWith("Recommended Comics:", StringComparison.OrdinalIgnoreCase))
                {
                    comicsSection = true;
                    mangaSection = false;
                    _logger.LogInformation("Parsing Recommended Comics section.");
                    continue;
                }
                if (trimmedLine.StartsWith("Recommended Manga:", StringComparison.OrdinalIgnoreCase))
                {
                    comicsSection = false;
                    mangaSection = true;
                    _logger.LogInformation("Parsing Recommended Manga section.");
                    continue;
                }

                // Regex to match "1. Title: Description"
                // It captures the title before the first colon, and the description after.
                var match = Regex.Match(trimmedLine, @"^\d+\.\s*(.+?):\s*(.+)$");
                if (match.Success && match.Groups.Count == 3)
                {
                    string title = match.Groups[1].Value.Trim();
                    string description = match.Groups[2].Value.Trim();

                    // Remove all asterisks and re-trim
                    title = title.Replace("*", "").Trim();
                    description = description.Replace("*", "").Trim();
                    
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
                    {
                        _logger.LogWarning($"Skipped recommendation due to empty title or description after cleaning: '{trimmedLine}'");
                        continue;
                    }

                    if (comicsSection)
                    {
                        result.RecommendedComics.Add(new RecommendedTitle { Title = title, Description = description });
                        _logger.LogInformation($"Added comic recommendation: '{title}' - '{description}'");
                    }
                    else if (mangaSection)
                    {
                        result.RecommendedManga.Add(new RecommendedTitle { Title = title, Description = description });
                        _logger.LogInformation($"Added manga recommendation: '{title}' - '{description}'");
                    }
                }
                else
                {
                    _logger.LogWarning($"Could not parse recommendation line: '{trimmedLine}'");
                }
            }
            _logger.LogInformation($"Parsed Recommendations - Comics: {result.RecommendedComics.Count}, Manga: {result.RecommendedManga.Count}");
            return result;
        }

        private void ParseAndApplyEnrichment(Comic comic, string geminiResponseText)
        {
            _logger.LogInformation($"Attempting to parse Gemini enrichment response: {geminiResponseText}");
            var lines = geminiResponseText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool updated = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("Author:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(comic.Author))
                {
                    var value = line.Substring("Author:".Length).Trim();
                    comic.Author = value.Equals("Not found", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
                    if (!string.IsNullOrEmpty(comic.Author)) updated = true;
                }
                else if (line.StartsWith("Publisher:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(comic.Publisher))
                {
                    var value = line.Substring("Publisher:".Length).Trim();
                    comic.Publisher = value.Equals("Not found", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
                    if (!string.IsNullOrEmpty(comic.Publisher)) updated = true;
                }
                else if (line.StartsWith("PublicationDate:", StringComparison.OrdinalIgnoreCase) && comic.PublicationDate == DateTime.MinValue)
                {
                    var dateStr = line.Substring("PublicationDate:".Length).Trim();
                    if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                    {
                        comic.PublicationDate = parsedDate;
                        updated = true;
                    }
                    // If "Not found" or unparseable, PublicationDate remains DateTime.MinValue
                }
                else if (line.StartsWith("Description:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(comic.Description))
                {
                    var value = line.Substring("Description:".Length).Trim();
                    comic.Description = value.Equals("Not found", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
                    if (!string.IsNullOrEmpty(comic.Description)) updated = true;
                }
            }
            if (updated) _logger.LogInformation($"Comic '{comic.Title}' was updated with enriched data from Gemini.");
            else _logger.LogInformation($"No new data applied from Gemini enrichment for '{comic.Title}'. Response might have been 'Not found' or data already existed.");
        }

        private class GeminiRequest
        {
            [JsonPropertyName("contents")]
            public Content[]? Contents { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public Part[]? Parts { get; set; }

            [JsonPropertyName("role")]
            public string? Role { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public Candidate[]? Candidates { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }
    }
}
