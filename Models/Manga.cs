using System.ComponentModel.DataAnnotations;

namespace ComicCollector.Models
{
    public class Manga
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }
}