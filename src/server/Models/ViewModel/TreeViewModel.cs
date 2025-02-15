using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talking_points.Models.ViewModel
{
    public class TreeViewModel
    {
        [Required]
        public ArticleDetails ArticleDetails { get; set; }
        [Required]
        public List<KeywordsWithCount> Keywords { get; set; }
    }

    public class KeywordsWithCount
    {
        public Guid Id { get; set; }
        public string Keyword { get; set; }
        public Guid ArticleId { get; set; }
        public int Count { get; set; }
    }
}
