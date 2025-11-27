using DatingApp.ProfileService.Core.Application.DTOs;
using DatingApp.ProfileService.Core.Application.Services;
using DatingApp.ProfileService.Core.Application.Validators;
using DatingApp.ProfileService.Core.Domain.Interfaces;
using DatingApp.ProfileService.Infrastructure.Messaging;
using DatingApp.ProfileService.Infrastructure.Messaging.Consumers;
using DatingApp.ProfileService.Infrastructure.Persistence;
using DatingApp.ProfileService.Infrastructure.Persistence.Repositories;
using DatingApp.ProfileService.Infrastructure.Security;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ====================
// Logging Configuration (Serilog)
// ====================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ====================
// Database (PostgreSQL + EF Core)
// ====================
builder.Services.AddDbContext<ProfileDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProfileDb")));

// ====================
// Domain Layer Services
// ====================
// Repositories
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();

// Encryption Service (Singleton for performance)
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// Initialize EncryptedStringConverter with encryption service
var serviceProvider = builder.Services.BuildServiceProvider();
var encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
EncryptedStringConverter.Initialize(encryptionService);

// ====================
// Application Layer Services
// ====================
builder.Services.AddScoped<IProfileService, ProfileService>();

// FluentValidation Validators
builder.Services.AddScoped<IValidator<CreateProfileRequest>, CreateProfileRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();

// ====================
// MassTransit (Message Bus)
// ====================
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<UserRegisteredConsumer>();

    // Configure RabbitMQ
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // Configure retry policy
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

        // Configure consumers
        cfg.ReceiveEndpoint("profile-service-user-registered", e =>
        {
            e.ConfigureConsumer<UserRegisteredConsumer>(context);
        });
    });
});

// Message Publisher
builder.Services.AddScoped<IMessagePublisher, MassTransitMessagePublisher>();

// ====================
// Authentication (JWT Bearer)
// ====================
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("JWT Secret not configured")))
        };
    });

builder.Services.AddAuthorization();

// ====================
// API Controllers
// ====================
builder.Services.AddControllers();

// ====================
// API Documentation (Swagger)
// ====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Profile Service API", Version = "v1" });

    // JWT Bearer authentication in Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ====================
// Health Checks
// ====================
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("ProfileDb")
        ?? throw new InvalidOperationException("ProfileDb connection string not configured"));

// ====================
// CORS (if needed for frontend)
// ====================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ====================
// Build Application
// ====================
var app = builder.Build();

// ====================
// Middleware Pipeline
// ====================

// Swagger UI (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS Redirection
app.UseHttpsRedirection();

// CORS
app.UseCors();

// Serilog Request Logging
app.UseSerilogRequestLogging();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Health Checks
app.MapHealthChecks("/health");

// ====================
// Database Migration (Development Only)
// ====================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
    await dbContext.Database.MigrateAsync();
    Log.Information("Database migrations applied");
}

// ====================
// Run Application
// ====================
try
{
    Log.Information("Starting Profile Service API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Profile Service API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
