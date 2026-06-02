using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Users;
using OpsFlow.Domain.Constants;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Users;

public sealed class EfUserLookupService(OpsFlowDbContext dbContext) : IUserLookupService
{
    public async Task<IReadOnlyList<AnalystLookupDto>> GetActiveAnalystsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .Join(
                dbContext.UserRoles,
                user => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user, userRole })
            .Join(
                dbContext.Roles.Where(role => role.Name == OpsFlowRoles.Analyst),
                userAndRole => userAndRole.userRole.RoleId,
                role => role.Id,
                (userAndRole, _) => userAndRole.user)
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Select(user => new AnalystLookupDto(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty))
            .ToListAsync(cancellationToken);
    }
}
