using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpsFlow.Infrastructure.Data;
using OpsFlow.Infrastructure.Data.Seed;

namespace OpsFlow.Api.Tests.Auth;

public sealed class OpsFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "OpsFlow.Api.Tests";
    public const string JwtAudience = "OpsFlow.Api.Tests";
    public const string JwtSigningKey = "test-only-signing-key-change-me-minimum-32-chars";
    private readonly string _databaseName = $"opsflow-auth-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["Jwt:AccessTokenMinutes"] = "30",
                ["ConnectionStrings:OpsFlowDb"] = "Server=(local);Database=OpsFlowTests;"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OpsFlowDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<OpsFlowDbContext>>();

            services.AddDbContext<OpsFlowDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        await DatabaseSeeder.SeedAsync(
            dbContext,
            new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
