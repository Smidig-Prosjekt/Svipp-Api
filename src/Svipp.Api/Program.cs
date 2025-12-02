using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// Auth: JWT (skip for design-time tools like EF migrations)
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? configuration["JWT_SECRET"] ?? "design-time-secret-key-for-ef-migrations-only";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["JWT_ISSUER"];
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["JWT_AUDIENCE"];

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

// Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
