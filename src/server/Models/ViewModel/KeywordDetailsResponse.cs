using System.Collections.Generic;
using talking_points.Models;

namespace talking_points.Models.ViewModel
{
    public class KeywordDetailsResponse
    {
        public Keywords Keyword { get; set; }
        public List<ArticleDetails> Articles { get; set; }
        public int TotalArticles { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasPreviousPage => CurrentPage > 1;
    }
}
