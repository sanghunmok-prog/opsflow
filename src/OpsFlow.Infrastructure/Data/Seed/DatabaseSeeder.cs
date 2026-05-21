using Microsoft.EntityFrameworkCore;

namespace OpsFlow.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OpsFlowDbContext dbContext, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Cases.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedData = SeedDataGenerator.Generate(nowUtc);

        await dbContext.Roles.AddRangeAsync(seedData.Roles, cancellationToken);
        await dbContext.Users.AddRangeAsync(seedData.Users, cancellationToken);
        await dbContext.UserRoles.AddRangeAsync(seedData.UserRoles, cancellationToken);
        await dbContext.CaseTypes.AddRangeAsync(seedData.CaseTypes, cancellationToken);
        await dbContext.SlaRules.AddRangeAsync(seedData.SlaRules, cancellationToken);
        await dbContext.Cases.AddRangeAsync(seedData.Cases, cancellationToken);
        await dbContext.CaseNotes.AddRangeAsync(seedData.CaseNotes, cancellationToken);
        await dbContext.StatusHistories.AddRangeAsync(seedData.StatusHistories, cancellationToken);
        await dbContext.AssignmentHistories.AddRangeAsync(seedData.AssignmentHistories, cancellationToken);
        await dbContext.ApprovalRequests.AddRangeAsync(seedData.ApprovalRequests, cancellationToken);
        await dbContext.AuditLogs.AddRangeAsync(seedData.AuditLogs, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
