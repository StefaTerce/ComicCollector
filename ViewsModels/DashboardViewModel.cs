using System.Collections.Generic;
using ComicCollector.Models;

namespace ComicCollector.ViewModels
{
    public class DashboardViewModel
    {
        public List<Comic> RecentComics { get; set; } = new List<Comic>();
        public List<Manga> RecentMangas { get; set; } = new List<Manga>();
        public List<Comic> FavoriteComics { get; set; } = new List<Comic>();
        public List<Manga> FavoriteMangas { get; set; } = new List<Manga>();
    }
}