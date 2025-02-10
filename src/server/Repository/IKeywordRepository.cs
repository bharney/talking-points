using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Models;

namespace talking_points.Repository
{
    public interface IKeywordRepository
    {
        Task<IEnumerable<Keywords>> GetAll();
        Task<List<Keywords>?> Get(Guid id);
        Task<Keywords> Insert(Keywords keyword);
        Task<bool> Update(Keywords keyword);
        Task<bool> Delete(Guid id);

    }
}