using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talking_points.Models.ViewModel
{
    public class KeywordsViewModel
    {
        [Required]
        public List<ArticleDetails> ArticleDetails { get; set; }
        [Required]
        public Keywords Keywords { get; set; }
    }
}
