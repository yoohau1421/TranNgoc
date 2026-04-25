using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TranNgoc.Models;

namespace TranNgoc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<MasterData> MasterData { get; set; }
    }
}
