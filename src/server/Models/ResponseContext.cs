
using Microsoft.EntityFrameworkCore;
using talking_points.Models;
namespace talking_points
{
    public class ResponseContext : DbContext
    {
        public ResponseContext(DbContextOptions<ResponseContext> options) : base(options)
        {
        }
        public DbSet<Response> Responses => Set<Response>();
    }
}
