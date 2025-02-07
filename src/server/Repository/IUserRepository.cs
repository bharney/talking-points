using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Models;

namespace talking_points.Repository
{
    public interface IUserRepository
    {
        Task<IEnumerable<ApplicationUser>> GetUsersAsync();
        Task<bool> DeleteUserAsync(Guid id);
        Task<ApplicationUser> InsertUserAsync(ApplicationUser user);
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<ApplicationUser> GetUserByIdAsync(Guid userGuid);
    }
}