using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Data;
using OpsFlow.Api.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OpsFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OpsFlowDb")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext, DateTime.UtcNow);
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "OpsFlow.Api" }))
    .WithName("HealthCheck");

app.Run();

public partial class Program;
