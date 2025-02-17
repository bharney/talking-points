using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using talking_points.Models;

namespace talking_points.Repository
{
    public interface IUserContext
    {
        Task<string> GenerateToken(ApplicationUser model);
        Task<ApplicationUser> GetCurrentUser();
        ApplicationUser NewGuestUser();
        void RemoveUserGuidCookies();
        void SetUserGuidCookies(Guid userGuid);
        Task<ApplicationUser> GetLoggedInUser();
        Guid? GetUserGuidFromCookies();
    }
}