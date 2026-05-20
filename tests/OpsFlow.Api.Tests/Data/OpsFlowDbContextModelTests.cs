using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Data;
using OpsFlow.Api.Data.Entities;

namespace OpsFlow.Api.Tests.Data;

public sealed class OpsFlowDbContextModelTests
{
    [Fact]
    public void DbContext_model_can_be_constructed()
    {
        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseInMemoryDatabase($"opsflow-model-{Guid.NewGuid()}")
            .Options;

        using var dbContext = new OpsFlowDbContext(options);
        var entityTypes = dbContext.Model.GetEntityTypes().Select(x => x.ClrType).ToHashSet();

        Assert.Contains(typeof(AppUser), entityTypes);
        Assert.Contains(typeof(CaseType), entityTypes);
        Assert.Contains(typeof(SlaRule), entityTypes);
        Assert.Contains(typeof(OpsCase), entityTypes);
        Assert.Contains(typeof(CaseNote), entityTypes);
        Assert.Contains(typeof(StatusHistory), entityTypes);
        Assert.Contains(typeof(AssignmentHistory), entityTypes);
        Assert.Contains(typeof(ApprovalRequest), entityTypes);
        Assert.Contains(typeof(AuditLog), entityTypes);
    }
}
