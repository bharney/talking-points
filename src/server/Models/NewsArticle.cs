using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talking_points.Models
{
	[Table("NewsArticles")]
    public class NewsArticle
    {
        [Key]
        public int Id { get; set; }
    public string? SourceId { get; set; }
    public string? SourceName { get; set; }
    public string? Author { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required]
    public string Url { get; set; } = string.Empty;
    public string? UrlToImage { get; set; }
        public DateTime? PublishedAt { get; set; }
    [Required]
    public string Content { get; set; } = string.Empty;
    }
}
