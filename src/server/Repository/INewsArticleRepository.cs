using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Models;

namespace talking_points.Repository
{
	public interface INewsArticleRepository
	{
		Task AddArticlesAsync(IEnumerable<ArticleDetails> articles);
		Task<bool> ArticleExistsAsync(string url);
		Task<System.DateTime?> GetLatestPublishedAtAsync();
		Task<IReadOnlyList<ArticleDetails>> FilterNewerUniqueAsync(IEnumerable<ArticleDetails> candidates, System.DateTime? minPublishedExclusive);
	}
}
