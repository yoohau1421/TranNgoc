using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TranNgoc.Models;

namespace TranNgoc.Data
{
    public class MasterDataDBContext(DbContextOptions<MasterDataDBContext> options) : DbContext(options)
    {
       public DbSet<MasterData> MasterData { get; set; }
    }
}
