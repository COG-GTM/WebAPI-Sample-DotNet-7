using Application.Interfaces;
using Application.Service.Interfaces;
using Application.Service;
using Infrastructure.DbContexts;
using Infrastructure.UoW;
using Microsoft.EntityFrameworkCore;
using WebApi.GraphQL.Queries;
using WebApi.GraphQL.Mutations;
using WebApi.GraphQL.Subscriptions;
using WebApi.GraphQL.Types;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<SampleDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseNodaTime()));
builder.Services.AddHealthChecks().AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"), name: "SampleDB");

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEducationService, EducationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<EducationQueries>()
    .AddMutationType<EducationMutations>()
    .AddSubscriptionType<EducationSubscriptions>()
    .AddType<EducationType>()
    .AddType<EducationStatisticsType>()
    .AddInMemorySubscriptions();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseWebSockets();

app.MapControllers();
app.MapGraphQL();
app.MapBananaCakePop("/graphql-ui");

app.Run();
