using FettiBot.Model.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace FettiBot.Model
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
        public DbSet<Client> Clients { get; set; } = null!;

    }
}