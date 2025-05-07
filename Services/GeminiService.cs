using ComicCollector.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json; // Required for PostAsJsonAsync and ReadFromJsonAsync
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ComicCollector.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly IOptionsMonitor<ApiKeySettings> _apiKeySettings;
        private string? ApiKey => _apiKeySettings.CurrentValue.GeminiApiKey;

        // Hypothetical Gemini API Configuration
        private readonly string _geminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models"; // Example base URL
        private readonly string _geminiModelForText = "gemini-pro:generateContent"; // Example model for text generation
        private readonly string _geminiModelForEnrichment = "gemini-pro:generateContent"; // Can be the same or different

        public GeminiService(HttpClient httpClient, ILogger<GeminiService> logger, IOptionsMonitor<ApiKeySettings> apiKeySettings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKeySettings = apiKeySettings;
        }

        public async Task<string?> GetReviewSummaryAsync(string title, string series, string? author)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                _logger.LogWarning("Gemini API key is not configured.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.LogWarning("Title is required to get a review summary.");
                return null;
            }

            try
            {
                // Construct the prompt for Gemini
                string prompt = $"For the comic or manga titled '{title}'";
                if (!string.IsNullOrWhiteSpace(series) && series.ToLower() != "n/a" && series.ToLower() != "manga")
                {
                    prompt += $" in the series '{series}'";
                }
                if (!string.IsNullOrWhiteSpace(author))
                {
                    prompt += $" by author(s) '{author}'";
                }
                prompt += ". Please provide an extensive list of its positive traits (at least 3-5 points if possible) and an extensive list of its negative traits (at least 2-4 points if possible). Format the response clearly, using bullet points under headings like 'Positive Traits:' and 'Negative Traits:'. If no specific reviews or information to form such lists are found, state that clearly.";

                _logger.LogInformation($"Sending prompt to Gemini for review summary: {prompt}");

                var requestUrl = $"{_geminiApiBaseUrl}/{_geminiModelForText}?key={ApiKey}";
                var requestPayload = new GeminiRequest { Contents = new[] { new Content { Parts = new[] { new Part { Text = prompt } } } } };

                await Task.Delay(200); // Simulate network latency
                GeminiResponse? geminiApiResponse = SimulateGeminiTextResponse(title, series, author);

                string? summary = geminiApiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(summary))
                {
                    _logger.LogWarning($"Gemini returned an empty or invalid summary for '{title}'.");
                    summary = "Review summary could not be retrieved at this time.";
                }

                _logger.LogInformation($"Gemini review summary received for '{title}'.");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting review summary from Gemini for '{title}'.");
                return "An error occurred while fetching the review summary.";
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

        private GeminiResponse SimulateGeminiTextResponse(string title, string? series, string? author)
        {
            // Simulate a plausible Gemini response structure with more positive/negative traits
            var simulatedText = $"Positive Traits:\n"
                                + "* Incredibly captivating storyline for '{title}' that keeps readers hooked from start to finish.\n"
                                + "* Deep and well-developed characters with relatable motivations and growth arcs.\n"
                                + "* Stunning and detailed artwork that beautifully complements the narrative tone.\n"
                                + "* Excellent world-building, creating a rich and immersive experience.";
            if (!string.IsNullOrWhiteSpace(series) && series.ToLower() != "n/a")
            {
                simulatedText += $"\n* Seamlessly integrates into the broader '{series}' lore.";
            }
            simulatedText += "\n\nNegative Traits:\n"
                           + "* The pacing can feel a bit rushed in the final chapters, leaving some subplots underdeveloped.\n"
                           + "* Some secondary characters could have benefited from more screen time or development.\n"
                           + "* Certain plot twists might be predictable for seasoned readers of the genre.";
            if (title.Contains("Space")) // Just an example to vary the simulated response
            {
                simulatedText += "\n* The depiction of space travel physics is not always accurate.";
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

        private void ParseAndApplyEnrichment(Comic comic, string geminiResponseText)
        {
            _logger.LogInformation($"Attempting to parse Gemini enrichment response: {geminiResponseText}");
            var lines = geminiResponseText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool updated = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("Author:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(comic.Author))
                {
                    comic.Author = line.Substring("Author:".Length).Trim();
                    if (comic.Author.Equals("Not found", StringComparison.OrdinalIgnoreCase)) comic.Author = null;
                    else updated = true;
                }
                else if (line.StartsWith("Publisher:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(comic.Publisher))
                {
                    comic.Publisher = line.Substring("Publisher:".Length).Trim();
                    if (comic.Publisher.Equals("Not found", StringComparison.OrdinalIgnoreCase)) comic.Publisher = null;
                    else updated = true;
                }
                else if (line.StartsWith("PublicationDate:", StringComparison.OrdinalIgnoreCase) && comic.PublicationDate == DateTime.MinValue)
                {
                    var dateStr = line.Substring("PublicationDate:".Length).Trim();
                    if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                    {
                        comic.PublicationDate = parsedDate;
                        updated = true;
                    }
                    else if (dateStr.Equals("Not found", StringComparison.OrdinalIgnoreCase))
                    {
                    }
                }
                else if (line.StartsWith("Description:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(comic.Description))
                {
                    comic.Description = line.Substring("Description:".Length).Trim();
                    if (comic.Description.Equals("Not found", StringComparison.OrdinalIgnoreCase)) comic.Description = null;
                    else updated = true;
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
