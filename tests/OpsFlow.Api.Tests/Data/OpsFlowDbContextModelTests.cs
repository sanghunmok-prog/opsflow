using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Data;

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
        Assert.Contains(typeof(IdentityRole<Guid>), entityTypes);
        Assert.Contains(typeof(CaseType), entityTypes);
        Assert.Contains(typeof(SlaRule), entityTypes);
        Assert.Contains(typeof(OpsCase), entityTypes);
        Assert.Contains(typeof(CaseNote), entityTypes);
        Assert.Contains(typeof(StatusHistory), entityTypes);
        Assert.Contains(typeof(AssignmentHistory), entityTypes);
        Assert.Contains(typeof(ApprovalRequest), entityTypes);
        Assert.Contains(typeof(AuditLog), entityTypes);
    }

    [Fact]
    public void DbContext_uses_identity_tables_for_users_and_roles()
    {
        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseInMemoryDatabase($"opsflow-model-{Guid.NewGuid()}")
            .Options;

        using var dbContext = new OpsFlowDbContext(options);

        Assert.Equal("AspNetUsers", dbContext.Model.FindEntityType(typeof(AppUser))?.GetTableName());
        Assert.Equal("AspNetRoles", dbContext.Model.FindEntityType(typeof(IdentityRole<Guid>))?.GetTableName());
    }

    [Fact]
    public void DbContext_does_not_include_forbidden_tables_or_persisted_is_overdue()
    {
        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseInMemoryDatabase($"opsflow-model-{Guid.NewGuid()}")
            .Options;

        using var dbContext = new OpsFlowDbContext(options);
        var tableNames = dbContext.Model.GetEntityTypes()
            .Select(x => x.GetTableName())
            .Where(x => x is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var caseProperties = dbContext.Model.FindEntityType(typeof(OpsCase))!
            .GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("OutboxMessage", tableNames);
        Assert.DoesNotContain("EscalationEvent", tableNames);
        Assert.DoesNotContain("JobRun", tableNames);
        Assert.DoesNotContain("TechnicalAuditLog", tableNames);
        Assert.DoesNotContain("DashboardMetrics", tableNames);
        Assert.DoesNotContain("IsOverdue", caseProperties);
    }

    [Fact]
    public void DbContext_configures_assignment_reason_and_case_row_version()
    {
        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseInMemoryDatabase($"opsflow-model-{Guid.NewGuid()}")
            .Options;

        using var dbContext = new OpsFlowDbContext(options);
        var assignmentReason = dbContext.Model.FindEntityType(typeof(AssignmentHistory))!
            .FindProperty(nameof(AssignmentHistory.Reason));
        var rowVersion = dbContext.Model.FindEntityType(typeof(OpsCase))!
            .FindProperty(nameof(OpsCase.RowVersion));

        Assert.False(assignmentReason?.IsNullable);
        Assert.Equal(500, assignmentReason?.GetMaxLength());
        Assert.True(rowVersion?.IsConcurrencyToken);
    }
}
