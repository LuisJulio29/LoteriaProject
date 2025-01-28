using LoteriaProject.Model;
using Microsoft.EntityFrameworkCore;

namespace LoteriaProject.Context
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<Ticket> Tickets { get; set; }
    }
}
