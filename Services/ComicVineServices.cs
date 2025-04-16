using ComicCollector.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ComicCollector.Services
{
    public class ComicVineService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://comicvine.gamespot.com/api";

        public ComicVineService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = configuration["ApiKeys:ComicVine"] ?? "b609648fe9073f7ac39915d338fce6f9bfac4971";
        }

        public async Task<List<Comic>> SearchComics(string query, int limit = 20)
        {
            var url = $"{_baseUrl}/issues/?api_key={_apiKey}&format=json&filter=name:{query}&limit={limit}&field_list=id,name,description,image,cover_date,volume,issue_number,character_credits,person_credits";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<ComicVineResponse>(content);

            var comics = new List<Comic>();

            if (results?.Results != null)
            {
                foreach (var item in results.Results)
                {
                    var comic = new Comic
                    {
                        Title = item.Name,
                        Description = StripHtmlTags(item.Description),
                        CoverImageUrl = item.Image?.OriginalUrl,
                        PublishDate = item.CoverDate,
                        IssueNumber = item.IssueNumber,
                        ApiId = item.Id.ToString(),
                        Characters = FormatCharacters(item.CharacterCredits),
                        Authors = FormatAuthors(item.PersonCredits)
                    };

                    comics.Add(comic);
                }
            }

            return comics;
        }

        public async Task<Comic> GetComicDetails(string apiId)
        {
            var url = $"{_baseUrl}/issue/4000-{apiId}/?api_key={_apiKey}&format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ComicVineDetailResponse>(content);

            if (result?.Results != null)
            {
                var item = result.Results;
                return new Comic
                {
                    Title = item.Name,
                    Description = StripHtmlTags(item.Description),
                    CoverImageUrl = item.Image?.OriginalUrl,
                    PublishDate = item.CoverDate,
                    IssueNumber = item.IssueNumber,
                    ApiId = item.Id.ToString(),
                    Characters = FormatCharacters(item.CharacterCredits),
                    Authors = FormatAuthors(item.PersonCredits),
                    Publisher = item.Volume?.Publisher?.Name
                };
            }

            return null;
        }

        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

        private string FormatCharacters(List<ComicVineCharacter> characters)
        {
            if (characters == null || characters.Count == 0)
                return string.Empty;

            return string.Join(", ", characters.ConvertAll(c => c.Name));
        }

        private string FormatAuthors(List<ComicVinePerson> authors)
        {
            if (authors == null || authors.Count == 0)
                return string.Empty;

            return string.Join(", ", authors.ConvertAll(a => a.Name));
        }

        // Classi per la deserializzazione
        private class ComicVineResponse
        {
            public List<ComicVineIssue> Results { get; set; }
        }

        private class ComicVineDetailResponse
        {
            public ComicVineIssue Results { get; set; }
        }

        private class ComicVineIssue
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public ComicVineImage Image { get; set; }
            public DateTime? CoverDate { get; set; }
            public int? IssueNumber { get; set; }
            public ComicVineVolume Volume { get; set; }
            public List<ComicVineCharacter> CharacterCredits { get; set; }
            public List<ComicVinePerson> PersonCredits { get; set; }
        }

        private class ComicVineVolume
        {
            public string Name { get; set; }
            public ComicVinePublisher Publisher { get; set; }
        }

        private class ComicVinePublisher
        {
            public string Name { get; set; }
        }

        private class ComicVineImage
        {
            public string OriginalUrl { get; set; }
        }

        private class ComicVineCharacter
        {
            public string Name { get; set; }
        }

        private class ComicVinePerson
        {
            public string Name { get; set; }
            public string Role { get; set; }
        }
    }
}