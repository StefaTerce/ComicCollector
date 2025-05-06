using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ComicCollector.Models
{
    // Risposta principale della ricerca MangaDex
    public class MangaDexSearchResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } // "ok" o "error"

        [JsonPropertyName("response")]
        public string Response { get; set; } // "collection"

        [JsonPropertyName("data")]
        public List<MangaDexManga> Data { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    // Dati di un singolo manga
    public class MangaDexManga
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } // Questo sarà il nostro SourceId per MangaDex

        [JsonPropertyName("type")]
        public string Type { get; set; } // "manga"

        [JsonPropertyName("attributes")]
        public MangaDexMangaAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<MangaDexRelationship> Relationships { get; set; }
    }

    // Attributi del manga
    public class MangaDexMangaAttributes
    {
        [JsonPropertyName("title")]
        public MangaDexMultiLangText Title { get; set; }

        [JsonPropertyName("altTitles")]
        public List<MangaDexMultiLangText> AltTitles { get; set; }

        [JsonPropertyName("description")]
        public MangaDexMultiLangText Description { get; set; }

        [JsonPropertyName("originalLanguage")]
        public string OriginalLanguage { get; set; } // es. "ja"

        [JsonPropertyName("status")]
        public string Status { get; set; } // es. "ongoing", "completed"

        [JsonPropertyName("year")]
        public int? Year { get; set; } // Anno di pubblicazione originale

        [JsonPropertyName("contentRating")]
        public string ContentRating { get; set; } // es. "safe", "suggestive"

        [JsonPropertyName("tags")]
        public List<MangaDexTag> Tags { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    // Testo multilingua (usato per titolo, descrizione)
    public class MangaDexMultiLangText : Dictionary<string, string> { }

    // Tag del manga
    public class MangaDexTag
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; } // "tag"
        [JsonPropertyName("attributes")]
        public MangaDexTagAttributes Attributes { get; set; }
    }
    public class MangaDexTagAttributes
    {
        [JsonPropertyName("name")]
        public MangaDexMultiLangText Name { get; set; }
    }


    // Relazioni (autore, artista, copertina)
    public class MangaDexRelationship
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // es. "author", "artist", "cover_art"

        // Usato se type è "author" o "artist" per ottenere il nome
        [JsonPropertyName("attributes")]
        public MangaDexPersonAttributes Attributes { get; set; }
    }

    // Attributi per persone (autore/artista)
    public class MangaDexPersonAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }


    // Dati della copertina (da una richiesta separata o inclusa)
    public class MangaDexCoverArtResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; }
        [JsonPropertyName("response")]
        public string Response { get; set; }
        [JsonPropertyName("data")]
        public MangaDexCoverArtData Data { get; set; }
    }
    public class MangaDexCoverArtData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; } // "cover_art"
        [JsonPropertyName("attributes")]
        public MangaDexCoverArtAttributes Attributes { get; set; }
    }
    public class MangaDexCoverArtAttributes
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        [JsonPropertyName("volume")]
        public string Volume { get; set; }
    }


    // Classe per la query di ricerca MangaDex
    public class MangaDexSearchQuery
    {
        public string Title { get; set; }
        public List<string> Authors { get; set; } // IDs degli autori
        public List<string> Artists { get; set; } // IDs degli artisti
        public int? Year { get; set; }
        public List<string> IncludedTags { get; set; } // IDs dei tag da includere
        public string Status { get; set; } // es. "ongoing", "completed"
        public string OriginalLanguage { get; set; } // es. "ja"
        public int Limit { get; set; } = 20;
        public int Offset { get; set; } = 0;
        public string OrderCreatedAt { get; set; } // "asc" o "desc"
        public string Includes { get; set; } = "cover_art,author,artist"; // Per includere dati correlati
    }

    // ViewModel per visualizzare i manga in modo unificato nella UI
    public class MangaViewModel
    {
        public string Id { get; set; } // MangaDex Manga ID
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public string Author { get; set; } // Nomi combinati di autori/artisti
        public int? Year { get; set; }
        public string Status { get; set; }
        public string ContentRating { get; set; }
        public string Source { get; set; } = "MangaDex"; // Fisso per questo ViewModel
        public DateTime PublicationDateForSort { get; set; } // Usato per ordinamento e form
    }
}