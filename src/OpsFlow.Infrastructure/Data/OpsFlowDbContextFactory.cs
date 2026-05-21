using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpsFlow.Infrastructure.Data;

public sealed class OpsFlowDbContextFactory : IDesignTimeDbContextFactory<OpsFlowDbContext>
{
    public OpsFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpsFlowDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OpsFlowDb")
            ?? "Server=localhost,1433;Database=OpsFlowDb;User Id=sa;Password=OpsFlow_dev_2026!;TrustServerCertificate=True;Encrypt=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new OpsFlowDbContext(optionsBuilder.Options);
    }
}
