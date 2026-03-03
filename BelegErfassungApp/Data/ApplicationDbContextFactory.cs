using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BelegErfassungApp.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Nur für Design-Time (Migrations) – Connection String direkt hier eintragen
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=BelegverwaltungDb;User Id=sa;Password=DeinSAPasswort;TrustServerCertificate=True;");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
