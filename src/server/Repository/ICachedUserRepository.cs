using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using talking_points.Models;
using talking_points.Repository;

namespace talking_points.Repository
{
    public interface ICachedUserRepository<T> where T : ApplicationUser 
    {
        Task<ApplicationUser> GetByIdAsync(Guid userGuid);
        Task<IEnumerable<ApplicationUser>> ListAsync();
    }
}