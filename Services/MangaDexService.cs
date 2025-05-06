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

namespace ComicCollector.Services
{
    public class MangaDexService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MangaDexService> _logger;
        private readonly string _baseUrl = "https://api.mangadex.org";
        private readonly string _coverBaseUrl = "https://uploads.mangadex.org/covers";

        public MangaDexService(HttpClient httpClient, ILogger<MangaDexService> logger)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ComicCollectorApp/1.0");
            _logger = logger;
        }

        private string GetPreferredText(MangaDexMultiLangText multiLangText, string preferredLang = "en")
        {
            if (multiLangText == null) return "N/A";
            if (multiLangText.TryGetValue(preferredLang, out var text) && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
            // Fallback alla prima lingua disponibile se quella preferita non c'è o è vuota
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
                queryParams["includes[]"] = "cover_art"; // Per ottenere l'ID della copertina
                queryParams["includes[]"] = "author";    // Per ottenere l'ID dell'autore
                queryParams["includes[]"] = "artist";    // Per ottenere l'ID dell'artista

                string requestUrl = $"{_baseUrl}/manga?{queryParams.ToString()}";
                _logger.LogInformation($"Requesting MangaDex: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"MangaDex API request failed with status {response.StatusCode}: {responseContent}");
                    return mangaViewModels;
                }

                var mangaDexResponse = JsonSerializer.Deserialize<MangaDexSearchResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (mangaDexResponse?.Result != "ok" || mangaDexResponse.Data == null)
                {
                    _logger.LogError($"MangaDex API returned an error or no data: {mangaDexResponse?.Result}");
                    return mangaViewModels;
                }

                foreach (var mangaData in mangaDexResponse.Data)
                {
                    var attributes = mangaData.Attributes;
                    if (attributes == null) continue;

                    string title = GetPreferredText(attributes.Title);
                    string description = GetPreferredText(attributes.Description);

                    var coverArtRelationship = mangaData.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
                    string coverFileName = null;
                    if (coverArtRelationship != null)
                    {
                        // Per ottenere il filename della copertina, sarebbe necessaria un'altra chiamata o un parsing più complesso se l'attributo 'fileName' fosse direttamente in 'cover_art'.
                        // MangaDex API spesso richiede di fare una chiamata separata all'endpoint /cover/{cover_id}
                        // Per semplicità, assumiamo di poterlo costruire se avessimo il filename.
                        // Per questa demo, cercheremo il filename se è incluso come attributo nella relazione.
                        // Altrimenti, si potrebbe dover fare una chiamata all'endpoint /cover/{coverArtRelationship.Id}
                        // Per questa implementazione, se l'attributo fileName non è direttamente disponibile nella relazione, lasciamo l'URL vuoto.
                        // Una soluzione più robusta farebbe una chiamata aggiuntiva o userebbe un batch request se disponibile.
                        // Per ora, se la cover è un attributo della relazione (non standard ma a volte visto in includes):
                        // coverFileName = (coverArtRelationship as dynamic)?.attributes?.fileName; // Questo è speculativo
                        // Se non è così, l'URL della copertina sarà:
                        // In una vera implementazione, dovresti fare una GET a /cover/{coverArtRelationship.Id} per ottenere il fileName.
                        // Per ora, costruiamo un URL ipotetico se abbiamo l'ID del manga e l'ID della copertina.
                        // Questo è il formato corretto:
                        if (coverArtRelationship != null)
                        {
                            // Bisogna fare una chiamata a /cover/{coverArtRelationship.Id} per ottenere il filename
                            // Per ora, simuliamo o lasciamo vuoto se non implementiamo la chiamata aggiuntiva.
                            // Se il filename fosse negli attributi della relazione (non standard):
                            // var coverAttributes = coverArtRelationship.Attributes as MangaDexCoverArtAttributes;
                            // if(coverAttributes != null) coverFileName = coverAttributes.FileName;

                            // Per ora, costruiamo l'URL basato sull'ID del manga e l'ID della copertina (comune, ma non sempre il filename è l'ID della copertina)
                            // L'URL corretto è: https://uploads.mangadex.org/covers/{manga_id}/{cover_filename}
                            // Senza una chiamata a /cover/{cover_id}, non abbiamo cover_filename.
                            // Possiamo provare a usare l'ID della copertina come filename con estensione .jpg, .png etc.
                            // Per ora, lasceremo questo più semplice e potremmo non avere la copertina corretta senza la chiamata aggiuntiva.
                            // Per questa demo, assumiamo che l'ID della copertina sia il filename con .jpg
                            // coverImageUrl = $"{_coverBaseUrl}/{mangaData.Id}/{coverArtRelationship.Id}.jpg"; 
                            // Questo è un placeholder e potrebbe non funzionare.
                            // La soluzione più corretta è fare una chiamata all'endpoint /cover.
                            // Per ora, lasceremo il coverImageUrl null se non possiamo determinarlo facilmente.
                        }
                    }
                    string coverImageUrl = null;
                    var coverRel = mangaData.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
                    if (coverRel != null)
                    {
                        // Per ottenere il filename corretto, dovresti fare una richiesta a /cover/{coverRel.Id}
                        // Per questa demo, proviamo a costruire un URL comune, ma potrebbe non essere sempre .jpg
                        // L'API di MangaDex dice che il filename è in /cover/{id} -> data.attributes.fileName
                        // Per ora, lasciamo null e l'utente dovrà aggiungerlo manualmente o faremo una chiamata /cover in futuro.
                        // In alternativa, se il filename fosse negli attributi della relazione (non standard):
                        // var coverAttributes = coverRel.Attributes as MangaDexCoverArtAttributes; // Non funzionerà così
                        // if(coverAttributes != null) coverImageUrl = $"{_coverBaseUrl}/{mangaData.Id}/{coverAttributes.FileName}";
                        // Per ora, se vuoi un placeholder:
                        // coverImageUrl = $"https://via.placeholder.com/300x450?text={HttpUtility.UrlEncode(title.Substring(0, Math.Min(title.Length,10)))}";
                        // Per una soluzione più robusta, dovresti fare una chiamata GET a /cover/{coverRel.Id}
                        // e poi usare il mangaData.Id e il fileName restituito.
                        // Per questa demo, lo lasceremo vuoto, l'utente può aggiungerlo manualmente.
                    }


                    var authors = mangaData.Relationships?.Where(r => r.Type == "author" && r.Attributes?.Name != null).Select(r => r.Attributes.Name);
                    var artists = mangaData.Relationships?.Where(r => r.Type == "artist" && r.Attributes?.Name != null).Select(r => r.Attributes.Name);

                    string authorNames = "Unknown";
                    if (authors != null && authors.Any())
                    {
                        authorNames = string.Join(", ", authors);
                        if (artists != null && artists.Any() && !artists.SequenceEqual(authors)) // Aggiungi artisti se diversi dagli autori
                        {
                            authorNames += " (Story), " + string.Join(", ", artists) + " (Art)";
                        }
                    }
                    else if (artists != null && artists.Any())
                    {
                        authorNames = string.Join(", ", artists) + " (Art)";
                    }


                    mangaViewModels.Add(new MangaViewModel
                    {
                        Id = mangaData.Id,
                        Title = title,
                        Description = SanitizeHtml(description), // Sanitize se la descrizione può contenere HTML
                        CoverImageUrl = coverImageUrl, // Sarà null per ora senza la chiamata all'endpoint /cover
                        Author = authorNames,
                        Year = attributes.Year,
                        Status = attributes.Status,
                        ContentRating = attributes.ContentRating,
                        Source = "MangaDex",
                        PublicationDateForSort = attributes.Year.HasValue ? new DateTime(attributes.Year.Value, 1, 1) : DateTime.MinValue
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching manga on MangaDex.");
            }
            return mangaViewModels;
        }

        // Funzione per ottenere l'URL della copertina (richiede una chiamata separata all'endpoint /cover)
        public async Task<string> GetCoverUrlAsync(string mangaId, string coverId)
        {
            if (string.IsNullOrEmpty(mangaId) || string.IsNullOrEmpty(coverId)) return null;

            try
            {
                string requestUrl = $"{_baseUrl}/cover/{coverId}";
                var response = await _httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var coverResponse = JsonSerializer.Deserialize<MangaDexCoverArtResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (coverResponse?.Result == "ok" && coverResponse.Data?.Attributes?.FileName != null)
                    {
                        return $"{_coverBaseUrl}/{mangaId}/{coverResponse.Data.Attributes.FileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching cover filename for cover ID {coverId}");
            }
            return null; // Fallback o URL placeholder
        }

        private string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }
    }
}