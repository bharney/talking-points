using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Models;

namespace talking_points.Repository
{
    public interface IArticleRepository
    {
        Task<IEnumerable<ArticleDetails>> GetAll();
        Task<ArticleDetails> Get(Guid id);
        Task<ArticleDetails> Insert(ArticleDetails article);
        Task<bool> Update(ArticleDetails article);
        Task<bool> Delete(Guid id);
        Task<ArticleDetails?> GetByURL(string URL);

    }
}