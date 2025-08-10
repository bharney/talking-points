using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using talking_points.Models;

namespace talking_points.Repository
{
    public class NewsArticleRepository : INewsArticleRepository
    {
        private readonly ApplicationDbContext _context;
        public NewsArticleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddArticlesAsync(IEnumerable<NewsArticle> articles)
        {
            // Avoid duplicates by URL
            var urls = articles.Select(a => a.Url).ToList();
            var existingUrls = await _context.NewsArticles
                .Where(a => urls.Contains(a.Url))
                .Select(a => a.Url)
                .ToListAsync();
            var newArticles = articles.Where(a => !existingUrls.Contains(a.Url)).ToList();
            if (newArticles.Count > 0)
            {
                await _context.NewsArticles.AddRangeAsync(newArticles);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ArticleExistsAsync(string url)
        {
            return await _context.NewsArticles.AnyAsync(a => a.Url == url);
        }

        public async Task<System.DateTime?> GetLatestPublishedAtAsync()
        {
            return await _context.NewsArticles
                .Where(a => a.PublishedAt != null)
                .OrderByDescending(a => a.PublishedAt)
                .Select(a => a.PublishedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<NewsArticle>> FilterNewerUniqueAsync(IEnumerable<NewsArticle> candidates, System.DateTime? minPublishedExclusive)
        {
            var list = candidates.ToList();
            if (list.Count == 0) return Array.Empty<NewsArticle>();
            var urls = list.Select(a => a.Url).Distinct().ToList();
            var existing = await _context.NewsArticles
                .Where(a => urls.Contains(a.Url))
                .Select(a => new { a.Url, a.PublishedAt })
                .ToListAsync();
            var existingUrlSet = new HashSet<string>(existing.Select(e => e.Url));
            var filtered = list.Where(a =>
                !existingUrlSet.Contains(a.Url) &&
                (!minPublishedExclusive.HasValue || (a.PublishedAt.HasValue && a.PublishedAt > minPublishedExclusive.Value))
            ).ToList();
            return filtered;
        }
    }
}
