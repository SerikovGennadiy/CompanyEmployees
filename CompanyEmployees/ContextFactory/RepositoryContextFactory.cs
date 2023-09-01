using Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompanyEmployees.ContextFactory
{
    public class RepositoryContextFactory : IDesignTimeDbContextFactory<RepositoryContext>
    {
        public RepositoryContext CreateDbContext(string[] args)
        {
            // make custom app configuration object
            var configuration =
                new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
           
            var builder = new DbContextOptionsBuilder<RepositoryContext>()
                                    .UseSqlServer(
                                            // apply our new configuration object
                                            configuration.GetConnectionString("sqlConnection"),
                                            // transfer model to real database
                                            m => m.MigrationsAssembly("CompanyEmployees"));

            return new RepositoryContext(builder.Options);
        }
    }
}
