﻿using System;
using System.ComponentModel.DataAnnotations;

namespace ComicCollector.Models
{
    public class Manga
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string JapaneseTitle { get; set; }

        public string Description { get; set; }

        public string CoverImageUrl { get; set; }

        public DateTime? PublishDate { get; set; }

        public string Author { get; set; }

        public string Status { get; set; }

        public int? Volumes { get; set; }

        public int? Chapters { get; set; }

        public string ApiId { get; set; }

        public string Genres { get; set; }

        // Relazioni
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public bool IsFavorite { get; set; } = false;
    }
}