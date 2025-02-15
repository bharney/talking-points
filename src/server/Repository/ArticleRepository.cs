using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace talking_points.Repository
{
    public class ArticleRepository : IArticleRepository
    {
        private ApplicationDbContext _Context;
        private ILogger _Logger;

        public ArticleRepository(ApplicationDbContext context, ILoggerFactory loggerFactory)
        {
            _Context = context;
            _Logger = loggerFactory.CreateLogger("ArticleRepository");
        }

        public async Task<IEnumerable<ArticleDetails>> GetAll()
        {
            return await _Context.Set<ArticleDetails>().ToListAsync();
        }

        public async Task<ArticleDetails> Get(Guid id)
        {
            return await _Context.Set<ArticleDetails>().FindAsync(id);
        }

        public async Task<ArticleDetails> Insert(ArticleDetails article)
        {
            await _Context.Set<ArticleDetails>().AddAsync(article);
            await _Context.SaveChangesAsync();
            return article;
        }

        public async Task<bool> Update(ArticleDetails article)
        {
            _Context.Set<ArticleDetails>().Update(article);
            return await _Context.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(Guid id)
        {
            var article = await _Context.Set<ArticleDetails>().FindAsync(id);
            if (article == null)
            {
                return false;
            }

            _Context.Set<ArticleDetails>().Remove(article);
            return await _Context.SaveChangesAsync() > 0;
        }

        public async Task<ArticleDetails?> GetByURL(string URL)
        {
            return await _Context.Set<ArticleDetails>().FirstOrDefaultAsync(x => x.URL == URL);
        }
    }
}
