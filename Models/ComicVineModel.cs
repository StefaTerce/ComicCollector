using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ComicCollector.Models
{
    public class ComicVineResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("number_of_page_results")]
        public int NumberOfPageResults { get; set; }

        [JsonPropertyName("number_of_total_results")]
        public int NumberOfTotalResults { get; set; }

        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("results")]
        public List<ComicVineIssue> Results { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public class ComicVineIssue
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("issue_number")]
        public string IssueNumber { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("image")]
        public ComicVineImage Image { get; set; }

        [JsonPropertyName("volume")]
        public ComicVineVolume Volume { get; set; }

        [JsonPropertyName("person_credits")]
        public List<ComicVinePersonCredit> PersonCredits { get; set; }

        [JsonPropertyName("cover_date")]
        public string CoverDate { get; set; }

        [JsonPropertyName("store_date")]
        public string StoreDate { get; set; }

        [JsonPropertyName("date_added")]
        public DateTime? DateAdded { get; set; }

        [JsonPropertyName("date_last_updated")]
        public DateTime? DateLastUpdated { get; set; }
    }

    public class ComicVineImage
    {
        [JsonPropertyName("original_url")]
        public string OriginalUrl { get; set; }

        [JsonPropertyName("small_url")]
        public string SmallUrl { get; set; }

        [JsonPropertyName("medium_url")]
        public string MediumUrl { get; set; }

        [JsonPropertyName("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }
    }

    public class ComicVineVolume
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class ComicVinePersonCredit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }

    // Classe per la query di ricerca
    public class ComicVineSearchQuery
    {
        public string Query { get; set; }
        public string Resources { get; set; } = "issue";
        public int Limit { get; set; } = 20;
        public int Page { get; set; } = 1;
        public string FieldList { get; set; } = "id,name,issue_number,description,image,volume,person_credits,cover_date,store_date";

        // AGGIUNGI QUESTA PROPRIETÀ SE MANCA
        public string Sort { get; set; } // Es. "date_added:desc", "name:asc"
    }
}