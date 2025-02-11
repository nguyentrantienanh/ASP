using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace ASP_thuchanh1.Models
{
    public class BookGenreViewModel
    {
        public List<Book>? Books { get; set; }
        public SelectList? Genres { get; set; }
        public string? BookGenre { get; set; }
        public string? SearchString { get; set; }
    }
}
