using System;
using System.ComponentModel.DataAnnotations;

namespace ComicCollector.Models
{
    public class Comic
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string CoverImageUrl { get; set; }

        public DateTime? PublishDate { get; set; }

        public string Publisher { get; set; }

        public int? IssueNumber { get; set; }

        public string ApiId { get; set; }

        public string Characters { get; set; }

        public string Authors { get; set; }

        // Relazioni
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public bool IsFavorite { get; set; } = false;
    }
}