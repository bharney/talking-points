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

		public async Task AddArticlesAsync(IEnumerable<ArticleDetails> articles)
		{
			// Avoid duplicates by URL
			var urls = articles.Select(a => a.URL).ToList();
			var existingUrls = await _context.ArticleDetails
				.Where(a => urls.Contains(a.URL))
				.Select(a => a.URL)
				.ToListAsync();
			var newArticles = articles.Where(a => !existingUrls.Contains(a.URL)).ToList();
			if (newArticles.Count > 0)
			{
				await _context.ArticleDetails.AddRangeAsync(newArticles);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<bool> ArticleExistsAsync(string url)
		{
			return await _context.ArticleDetails.AnyAsync(a => a.URL == url);
		}

		public async Task<System.DateTime?> GetLatestPublishedAtAsync()
		{
			return await _context.ArticleDetails
				.Where(a => a.PublishedAt != null)
				.OrderByDescending(a => a.PublishedAt)
				.Select(a => a.PublishedAt)
				.FirstOrDefaultAsync();
		}

		public async Task<IReadOnlyList<ArticleDetails>> FilterNewerUniqueAsync(IEnumerable<ArticleDetails> candidates, System.DateTime? minPublishedExclusive)
		{
			var list = candidates.ToList();
			if (list.Count == 0) return Array.Empty<ArticleDetails>();
			var urls = list.Select(a => a.URL).Distinct().ToList();
			var existing = await _context.ArticleDetails
				.Where(a => urls.Contains(a.URL))
				.Select(a => new { a.URL, a.PublishedAt })
				.ToListAsync();
			var existingUrlSet = new HashSet<string>(existing.Select(e => e.URL));
			var filtered = list.Where(a =>
				!existingUrlSet.Contains(a.URL) &&
				(!minPublishedExclusive.HasValue || (a.PublishedAt.HasValue && a.PublishedAt > minPublishedExclusive.Value))
			).ToList();
			return filtered;
		}
	}
}
