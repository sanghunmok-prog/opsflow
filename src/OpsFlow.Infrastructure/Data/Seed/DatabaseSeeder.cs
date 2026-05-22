using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OpsFlowDbContext dbContext, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var seedData = SeedDataGenerator.Generate(nowUtc);

        await EnsureIdentitySeedAsync(dbContext, seedData, cancellationToken);

        if (await dbContext.Cases.AnyAsync(cancellationToken))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

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

    private static async Task EnsureIdentitySeedAsync(
        OpsFlowDbContext dbContext,
        SeedDataSet seedData,
        CancellationToken cancellationToken)
    {
        foreach (var role in seedData.Roles)
        {
            var roleExists = await dbContext.Roles.AnyAsync(x => x.NormalizedName == role.NormalizedName, cancellationToken);
            if (!roleExists)
            {
                await dbContext.Roles.AddAsync(role, cancellationToken);
            }
        }

        foreach (var user in seedData.Users)
        {
            var existingUser = await dbContext.Users
                .SingleOrDefaultAsync(x => x.NormalizedEmail == user.NormalizedEmail, cancellationToken);

            if (existingUser is null)
            {
                await dbContext.Users.AddAsync(user, cancellationToken);
            }
            else
            {
                SetDemoPasswordHash(existingUser);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var userIdsByEmail = await dbContext.Users
            .Where(x => seedData.Users.Select(user => user.NormalizedEmail).Contains(x.NormalizedEmail))
            .ToDictionaryAsync(x => x.NormalizedEmail!, x => x.Id, cancellationToken);
        var roleIdsByName = await dbContext.Roles
            .Where(x => seedData.Roles.Select(role => role.NormalizedName).Contains(x.NormalizedName))
            .ToDictionaryAsync(x => x.NormalizedName!, x => x.Id, cancellationToken);
        var seedUsersById = seedData.Users.ToDictionary(x => x.Id);
        var seedRolesById = seedData.Roles.ToDictionary(x => x.Id);

        foreach (var userRole in seedData.UserRoles)
        {
            var normalizedEmail = seedUsersById[userRole.UserId].NormalizedEmail!;
            var normalizedRoleName = seedRolesById[userRole.RoleId].NormalizedName!;
            var actualUserId = userIdsByEmail[normalizedEmail];
            var actualRoleId = roleIdsByName[normalizedRoleName];

            var userRoleExists = await dbContext.UserRoles
                .AnyAsync(x => x.UserId == actualUserId && x.RoleId == actualRoleId, cancellationToken);
            if (!userRoleExists)
            {
                await dbContext.UserRoles.AddAsync(
                    new IdentityUserRole<Guid>
                    {
                        UserId = actualUserId,
                        RoleId = actualRoleId
                    },
                    cancellationToken);
            }
        }
    }

    private static void SetDemoPasswordHash(AppUser user)
    {
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, SeedDataGenerator.DemoPassword);
    }
}
