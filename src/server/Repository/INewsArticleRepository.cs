using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Models;

namespace talking_points.Repository
{
    public interface INewsArticleRepository
    {
        Task AddArticlesAsync(IEnumerable<NewsArticle> articles);
        Task<bool> ArticleExistsAsync(string url);
    Task<System.DateTime?> GetLatestPublishedAtAsync();
    Task<IReadOnlyList<NewsArticle>> FilterNewerUniqueAsync(IEnumerable<NewsArticle> candidates, System.DateTime? minPublishedExclusive);
    }
}
