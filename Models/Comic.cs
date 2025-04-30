using System;
using System.ComponentModel.DataAnnotations;

namespace ComicCollector.Models
{
    public class Comic
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Il titolo è richiesto")]
        public string Title { get; set; }

        [Required(ErrorMessage = "La serie è richiesta")]
        public string Series { get; set; }

        [Required(ErrorMessage = "Il numero è richiesto")]
        public int IssueNumber { get; set; }

        [Required(ErrorMessage = "L'autore è richiesto")]
        public string Author { get; set; }

        [Required(ErrorMessage = "L'editore è richiesto")]
        public string Publisher { get; set; }

        [Required(ErrorMessage = "La data di pubblicazione è richiesta")]
        public DateTime PublicationDate { get; set; }

        public int PageCount { get; set; }

        public string CoverImage { get; set; }

        public string Description { get; set; }

        public string Notes { get; set; }

        public string UserId { get; set; }

        // Proprietà per tenere traccia di quando è stato creato e aggiornato
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}