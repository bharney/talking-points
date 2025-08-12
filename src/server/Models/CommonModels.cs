
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talking_points
{
	[Table("ArticleDetails")]
	public class ArticleDetails
	{
		[Key]
		public Guid Id { get; set; }
		public string Description { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Source { get; set; } = string.Empty;
		[Required]
		public string URL { get; set; }
		public string? Abstract { get; set; }

		public string? SourceName { get; set; }
		public string? Author { get; set; }
		[Required]
		public string? UrlToImage { get; set; }
		public DateTime? PublishedAt { get; set; }
		[Required]
		public string Content { get; set; } = string.Empty;
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
