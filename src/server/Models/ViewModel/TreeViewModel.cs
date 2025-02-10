using System.ComponentModel.DataAnnotations;

namespace talking_points.Models.ViewModel
{
    public class TreeViewModel
    {
        [Required]
        public ArticleDetails ArticleDetails { get; set; }
        [Required]
        public List<Keywords> Keywords { get; set; }
    }
}
