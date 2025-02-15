using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace talking_points.Repository
{
    public class KeywordRepository : IKeywordRepository
    {
        private ApplicationDbContext _Context;
        private ILogger _Logger;
        public KeywordRepository(ApplicationDbContext context, ILoggerFactory loggerFactory)
        {
            _Context = context;
            _Logger = loggerFactory.CreateLogger("KeywordRepository");
        }
        public async Task<IEnumerable<Keywords>> GetAll()
        {
            return await _Context.Set<Keywords>().ToListAsync();
        }

        // The id corresponds to articleDetails.Id
        public async Task<List<Keywords>?> Get(Guid id)
        {
           var keywords = await _Context.Set<Keywords>().ToListAsync();
           return keywords.Where(x => x.ArticleId == id).ToList();  
        }
        public async Task<Keywords> Insert(Keywords keyword)
        {
            await _Context.Set<Keywords>().AddAsync(keyword);
            await _Context.SaveChangesAsync();
            return keyword;
        }
        public async Task<bool> Update(Keywords keyword)
        {
            _Context.Set<Keywords>().Update(keyword);
            return await _Context.SaveChangesAsync() > 0;
        }
        public async Task<bool> Delete(Guid id)
        {
            var keyword = await _Context.Set<Keywords>().FindAsync(id);
            if (keyword == null)
            {
                return false;
            }
            _Context.Set<Keywords>().Remove(keyword);
            return await _Context.SaveChangesAsync() > 0;
        }
    }
}
