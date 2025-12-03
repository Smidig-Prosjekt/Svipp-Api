using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Svipp.Api.Services;
using Svipp.Infrastructure;
using System.Text;

// Fix for Npgsql DateTime handling
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Konfigurasjon
var configuration = builder.Configuration;

// Database: PostgreSQL via SvippDbContext
var connectionString = configuration.GetConnectionString("DefaultConnection")
                      ?? $"Host=localhost;Port={Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"};Database={Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "svipp_dev_db"};Username={Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "svipp_dev"};Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "svipp_dev_password"}";

builder.Services.AddDbContext<SvippDbContext>(options =>
    options.UseNpgsql(connectionString));

// Auth: JWT
// Priority: Environment variables > appsettings.json (works in all environments)
// Note: appsettings.Development.json overrides appsettings.json in Development mode
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
                ?? configuration["JWT_SECRET"] 
                ?? throw new InvalidOperationException(
                    "JWT_SECRET must be configured. " +
                    "Set it in appsettings.json, appsettings.Development.json, or as JWT_SECRET environment variable.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? configuration["JWT_ISSUER"] 
                ?? "Svipp.Api";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                  ?? configuration["JWT_AUDIENCE"] 
                  ?? "Svipp.Client";

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "JwtBearer";
        options.DefaultChallengeScheme = "JwtBearer";
    })
    .AddJwtBearer("JwtBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = key
        };
    });

builder.Services.AddAuthorization();

// Password hashing service
builder.Services.AddSingleton<PasswordHasher>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow localhost:3000 with credentials for cookie auth
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // In production, configure specific origins
            var allowedOrigins = configuration["AllowedOrigins"]?.Split(',') 
                ?? new[] { 
                    "http://localhost:3000",  // Next.js dev server
                    "https://svipp.no", 
                    "https://www.svipp.no" 
                };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Swagger with JWT support
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Svipp API",
        Version = "v1",
        Description = "API for Svipp - Hjemtransport med el-sparkesykkel"
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-migrate database in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SvippDbContext>();
    dbContext.Database.Migrate();
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
