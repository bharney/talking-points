
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talking_points
{
    [Table("ArticleDetails")]
    public class ArticleDetails
    {
        [Key]
        public Guid Id { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public string Source { get; set; }
        [Required]
        public string URL { get; set; }
        public string? Abstract { get; set; }
    }

    [Table("Keywords")]
    public class Keywords
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Keyword { get; set; }
        [ForeignKey("ArticleDetails")]
        public Guid ArticleId { get; set; }
    }
}
