using Application.Interfaces;
using Application.Service.Interfaces;
using Application.Service;
using Infrastructure.DbContexts;
using Infrastructure.UoW;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<SampleDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseNodaTime()));
builder.Services.AddHealthChecks().AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"), name: "SampleDB");

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEducationService, EducationService>();

// OpenAPI document generation (built-in in .NET 10)
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-apply pending EF Core migrations on startup (with retry for Docker readiness)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    var retries = 10;
    for (var i = 0; i < retries; i++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (i < retries - 1)
        {
            app.Logger.LogWarning(ex, "Database not ready, retrying in {Seconds}s ({Attempt}/{MaxRetries})...", i + 1, i + 1, retries);
            Thread.Sleep(TimeSpan.FromSeconds(i + 1));
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
