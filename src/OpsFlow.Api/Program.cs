using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Application.Common;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Api;
using OpsFlow.Application.Approvals;
using OpsFlow.Infrastructure.Approvals;
using OpsFlow.Infrastructure;
using OpsFlow.Infrastructure.Auth;
using OpsFlow.Infrastructure.Cases;
using OpsFlow.Infrastructure.Data;
using OpsFlow.Infrastructure.Data.Seed;
using OpsFlow.Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OpsFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OpsFlowDb")));
builder.Services
    .AddIdentityCore<AppUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<OpsFlowDbContext>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICaseQueryService, EfCaseQueryService>();
builder.Services.AddScoped<ICaseCommandService, EfCaseCommandService>();
builder.Services.AddScoped<ICaseAssignmentService, EfCaseAssignmentService>();
builder.Services.AddScoped<ICaseStatusService, EfCaseStatusService>();
builder.Services.AddScoped<ICaseAccessService, EfCaseAccessService>();
builder.Services.AddScoped<ICaseNoteService, EfCaseNoteService>();
builder.Services.AddScoped<ICaseTimelineService, EfCaseTimelineService>();
builder.Services.AddScoped<IApprovalService, EfApprovalService>();
builder.Services.AddScoped<ISlaService, SlaService>();
builder.Services.AddScoped<OpsFlow.Application.Users.IUserLookupService, EfUserLookupService>();
builder.Services.AddControllers();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is required.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(OpsFlowPolicies.RequireAdmin, policy =>
        policy.RequireRole(OpsFlowRoles.Admin));
    options.AddPolicy(OpsFlowPolicies.RequireManagerOrAdmin, policy =>
        policy.RequireRole(OpsFlowRoles.Manager, OpsFlowRoles.Admin));
    options.AddPolicy(OpsFlowPolicies.RequireAnalystOrManagerOrAdmin, policy =>
        policy.RequireRole(OpsFlowRoles.Analyst, OpsFlowRoles.Manager, OpsFlowRoles.Admin));
    options.AddPolicy(OpsFlowPolicies.RequireAuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext, DateTime.UtcNow);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "OpsFlow.Api" }))
    .WithName("HealthCheck");
app.MapControllers();

app.Run();

public partial class Program;
