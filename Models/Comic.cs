using System;
using System.ComponentModel.DataAnnotations;

namespace ComicCollector.Models
{
    public class Comic
    {
        // Chiave primaria generata dal database
        [Key]
        public int ComicCollectorId { get; set; }

        // ID dall'API esterna (ComicVine ID, MangaDex ID) - Può essere stringa per MangaDex
        public string? SourceId { get; set; }

        [Required(ErrorMessage = "Il titolo è richiesto")]
        public string Title { get; set; }

        [Required(ErrorMessage = "La serie è richiesta")]
        public string Series { get; set; }

        // Non tutti i manga hanno un issue number, quindi rendiamolo nullable o 0 se non applicabile
        public int? IssueNumber { get; set; }

        [Required(ErrorMessage = "L'autore è richiesto")]
        public string Author { get; set; }

        public string? Publisher { get; set; } // Può non essere sempre presente

        [Required(ErrorMessage = "La data di pubblicazione è richiesta")]
        [DataType(DataType.Date)]
        public DateTime PublicationDate { get; set; }

        public int? PageCount { get; set; }

        public string? CoverImage { get; set; } // URL dell'immagine di copertina

        [MaxLength(2000, ErrorMessage = "La descrizione non può superare i 2000 caratteri.")]
        public string? Description { get; set; }

        [MaxLength(1000, ErrorMessage = "Le note non possono superare i 1000 caratteri.")]
        public string? Notes { get; set; } // Note personali dell'utente

        // Foreign Key per ApplicationUser
        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; } // Navigation property

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Origine del dato: "Local" (aggiunto manualmente), "ComicVine", "MangaDex"
        [Required]
        [MaxLength(50)]
        public string Source { get; set; } = "Local";
    }
}