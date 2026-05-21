using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure.Data;
using OpsFlow.Infrastructure.Data.Seed;

namespace OpsFlow.Api.Tests.Data;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_does_not_duplicate_existing_seeded_cases()
    {
        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseInMemoryDatabase($"opsflow-seed-{Guid.NewGuid()}")
            .Options;
        var fixedNowUtc = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);

        await using var dbContext = new OpsFlowDbContext(options);

        await DatabaseSeeder.SeedAsync(dbContext, fixedNowUtc);
        await DatabaseSeeder.SeedAsync(dbContext, fixedNowUtc.AddDays(1));

        Assert.Equal(SeedDataGenerator.CaseCount, await dbContext.Cases.CountAsync());
    }
}
