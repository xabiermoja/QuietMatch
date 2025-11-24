using System.Text;
using DatingApp.IdentityService.Application.Services;
using DatingApp.IdentityService.Domain.Repositories;
using DatingApp.IdentityService.Infrastructure.Data;
using DatingApp.IdentityService.Infrastructure.Repositories;
using DatingApp.IdentityService.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Infrastructure Services
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Application Services
builder.Services.AddScoped<AuthService>();

// MassTransit (Message Bus)
builder.Services.AddMassTransit(x =>
{
    // Configure RabbitMQ for local development
    if (builder.Environment.IsDevelopment())
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
            });

            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        // TODO: Configure Azure Service Bus for production
        // x.UsingAzureServiceBus((context, cfg) =>
        // {
        //     cfg.Host(builder.Configuration["AzureServiceBus:ConnectionString"]);
        //     cfg.ConfigureEndpoints(context);
        // });
    }
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "QuietMatch Identity Service API",
        Version = "v1",
        Description = "Authentication service for QuietMatch - handles OAuth social login and JWT token issuance"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline

// Logging middleware (request/response logging)
app.UseSerilogRequestLogging();

// Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

// HTTPS redirection
app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

try
{
    Log.Information("Starting IdentityService API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "IdentityService API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
