using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using Microsoft.Extensions.Options;
using MottuApi.Controllers;
using MottuApi.Models;
using System;
using UrlShortnerApi.Migrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();


// Get connection string from appsettings.json to connect 
builder.Services.AddDbContext<AppDbContext>(Options =>
    Options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnection")));

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<AppDbContext>();
builder.Services.AddScoped<InitialState>();

var app = builder.Build();

// Manage initial state for DB
InitialState.ValidateInitialState();

// Start url shortner routes
UrlShortnerRoutes.GenerateShortUrl(app);

app.UseHttpsRedirection();

app.UseAuthorization();

app.Run();

