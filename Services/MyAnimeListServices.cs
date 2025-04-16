using ComicCollector.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ComicCollector.Services
{
    public class MyAnimeListService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.jikan.moe/v4";

        public MyAnimeListService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Manga>> SearchManga(string query, int limit = 20)
        {
            var url = $"{_baseUrl}/manga?q={query}&limit={limit}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<JikanResponse>(content);

            var mangas = new List<Manga>();

            if (results?.Data != null)
            {
                foreach (var item in results.Data)
                {
                    var manga = new Manga
                    {
                        Title = item.Title,
                        JapaneseTitle = item.TitleJapanese,
                        Description = item.Synopsis,
                        CoverImageUrl = item.Images?.JPG?.ImageUrl,
                        PublishDate = item.Published?.From,
                        Author = FormatAuthors(item.Authors),
                        Status = item.Status,
                        Volumes = item.Volumes,
                        Chapters = item.Chapters,
                        ApiId = item.MalId.ToString(),
                        Genres = FormatGenres(item.Genres)
                    };

                    mangas.Add(manga);
                }
            }

            return mangas;
        }

        public async Task<Manga> GetMangaDetails(int malId)
        {
            var url = $"{_baseUrl}/manga/{malId}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<JikanDetailResponse>(content);

            if (result?.Data != null)
            {
                var item = result.Data;
                return new Manga
                {
                    Title = item.Title,
                    JapaneseTitle = item.TitleJapanese,
                    Description = item.Synopsis,
                    CoverImageUrl = item.Images?.JPG?.ImageUrl,
                    PublishDate = item.Published?.From,
                    Author = FormatAuthors(item.Authors),
                    Status = item.Status,
                    Volumes = item.Volumes,
                    Chapters = item.Chapters,
                    ApiId = item.MalId.ToString(),
                    Genres = FormatGenres(item.Genres)
                };
            }

            return null;
        }

        private string FormatAuthors(List<JikanAuthor> authors)
        {
            if (authors == null || authors.Count == 0)
                return string.Empty;

            return string.Join(", ", authors.ConvertAll(a => a.Person?.Name));
        }

        private string FormatGenres(List<JikanGenre> genres)
        {
            if (genres == null || genres.Count == 0)
                return string.Empty;

            return string.Join(", ", genres.ConvertAll(g => g.Name));
        }

        // Classi per la deserializzazione
        private class JikanResponse
        {
            public List<JikanManga> Data { get; set; }
        }

        private class JikanDetailResponse
        {
            public JikanManga Data { get; set; }
        }

        private class JikanManga
        {
            public int MalId { get; set; }
            public string Title { get; set; }
            public string TitleJapanese { get; set; }
            public string Synopsis { get; set; }
            public JikanImages Images { get; set; }
            public JikanPublished Published { get; set; }
            public List<JikanAuthor> Authors { get; set; }
            public string Status { get; set; }
            public int? Volumes { get; set; }
            public int? Chapters { get; set; }
            public List<JikanGenre> Genres { get; set; }
        }

        private class JikanImages
        {
            public JikanImage JPG { get; set; }
        }

        private class JikanImage
        {
            public string ImageUrl { get; set; }
        }

        private class JikanPublished
        {
            public DateTime? From { get; set; }
        }

        private class JikanAuthor
        {
            public JikanPerson Person { get; set; }
        }

        private class JikanPerson
        {
            public string Name { get; set; }
        }

        private class JikanGenre
        {
            public string Name { get; set; }
        }
    }
}