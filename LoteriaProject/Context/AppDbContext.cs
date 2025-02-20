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
        public DbSet<User> Users { get; set; }
        public DbSet<Patron> Patrons { get; set; }
        public DbSet<AstroPatron> AstroPatrons { get; set; }
        public DbSet<Sorteo> Sorteos { get; set; }
        public DbSet<SorteoPatron> SorteosPatrons { get; set; }
    }
}
