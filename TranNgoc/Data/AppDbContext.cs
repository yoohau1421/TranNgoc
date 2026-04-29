using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TranNgoc.Models;
using TranNgoc_BE.Models;

namespace TranNgoc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<MasterData> MasterData { get; set; }
        public DbSet<CompareTemplate> CompareTemplates { get; set; }
        public DbSet<CompareTemplateColumn> CompareTemplateColumns { get; set; }
        public DbSet<CompareMasterData> CompareMasterData { get; set; }
        public DbSet<CompareRuleConfig> CompareRuleConfigs { get; set; }
    }
}
