using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Infrastructure.Data;

public sealed class OpsFlowDbContext(DbContextOptions<OpsFlowDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<CaseType> CaseTypes => Set<CaseType>();
    public DbSet<SlaRule> SlaRules => Set<SlaRule>();
    public DbSet<OpsCase> Cases => Set<OpsCase>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<AssignmentHistory> AssignmentHistories => Set<AssignmentHistory>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentityUsers(modelBuilder);
        ConfigureCaseTypes(modelBuilder);
        ConfigureSlaRules(modelBuilder);
        ConfigureCases(modelBuilder);
        ConfigureCaseNotes(modelBuilder);
        ConfigureStatusHistories(modelBuilder);
        ConfigureAssignmentHistories(modelBuilder);
        ConfigureApprovalRequests(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
    }

    private static void ConfigureIdentityUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<IdentityRole<Guid>>(entity => entity.ToTable("AspNetRoles"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(entity => entity.ToTable("AspNetUserRoles"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity => entity.ToTable("AspNetUserClaims"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity => entity.ToTable("AspNetUserLogins"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity => entity.ToTable("AspNetRoleClaims"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(entity => entity.ToTable("AspNetUserTokens"));
    }

    private static void ConfigureCaseTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CaseType>(entity =>
        {
            entity.ToTable("CaseTypes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });
    }

    private static void ConfigureSlaRules(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SlaRule>(entity =>
        {
            entity.ToTable("SlaRules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.TargetHours).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.CaseTypeId, x.Priority })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            entity.HasOne(x => x.CaseType)
                .WithMany(x => x.SlaRules)
                .HasForeignKey(x => x.CaseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCases(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OpsCase>(entity =>
        {
            entity.ToTable("Cases");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CaseNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.DueAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasIndex(x => x.CaseNumber).IsUnique();
            entity.HasIndex(x => new { x.Status, x.AssignedToUserId, x.DueAtUtc });
            entity.HasIndex(x => new { x.CaseTypeId, x.Priority, x.Status });
            entity.HasIndex(x => x.CreatedAtUtc);

            entity.HasOne(x => x.CaseType)
                .WithMany(x => x.Cases)
                .HasForeignKey(x => x.CaseTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedToUser)
                .WithMany(x => x.AssignedCases)
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedCases)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureCaseNotes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CaseNote>(entity =>
        {
            entity.ToTable("CaseNotes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.CaseId, x.CreatedAtUtc });

            entity.HasOne(x => x.Case)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AuthorUser)
                .WithMany(x => x.AuthoredNotes)
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureStatusHistories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatusHistory>(entity =>
        {
            entity.ToTable("StatusHistories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.CaseId, x.CreatedAtUtc });

            entity.HasOne(x => x.Case)
                .WithMany(x => x.StatusHistories)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ChangedByUser)
                .WithMany(x => x.StatusChanges)
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureAssignmentHistories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssignmentHistory>(entity =>
        {
            entity.ToTable("AssignmentHistories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.CaseId, x.CreatedAtUtc });

            entity.HasOne(x => x.Case)
                .WithMany(x => x.AssignmentHistories)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.FromUser)
                .WithMany()
                .HasForeignKey(x => x.FromUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ToUser)
                .WithMany(x => x.AssignmentsReceived)
                .HasForeignKey(x => x.ToUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.AssignedByUser)
                .WithMany(x => x.AssignmentsMade)
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureApprovalRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.ToTable("ApprovalRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.RequestReason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.DecisionReason).HasMaxLength(1000);
            entity.Property(x => x.RequestedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.Status, x.RequestedAtUtc });
            entity.HasIndex(x => new { x.CaseId, x.Status });
            entity.HasIndex(x => x.CaseId)
                .IsUnique()
                .HasFilter("[Status] = 'Pending'");

            entity.HasOne(x => x.Case)
                .WithMany(x => x.ApprovalRequests)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.RequestedByUser)
                .WithMany(x => x.ApprovalRequests)
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ReviewedByUser)
                .WithMany(x => x.ApprovalReviews)
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).HasConversion<string>().HasMaxLength(80).IsRequired();
            entity.Property(x => x.OldValuesJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.ActorUserId, x.CreatedAtUtc });

            entity.HasOne(x => x.ActorUser)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
