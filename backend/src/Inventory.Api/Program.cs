using System.Text;
using Inventory.Api.Auth;
using Inventory.Api.Middleware;
using Inventory.Api.Seeding;
using Inventory.Domain.Users;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// Configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

if (env.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// MVC & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory API", Version = "v1" });

    // JWT Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
      {
        new OpenApiSecurityScheme {
          Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
      }
    });

    // X-Tenant-ID header
    c.AddSecurityDefinition("Tenant", new OpenApiSecurityScheme
    {
        Description = "Multi-tenancy header (X-Tenant-ID)",
        Name = "X-Tenant-ID",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
      {
        new OpenApiSecurityScheme {
          Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Tenant" }
        },
        Array.Empty<string>()
      }
    });
});

// Infrastructure (DbContext, TenantContext, interceptors, services, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Services & Auth
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// JWT auth
var jwtOpts = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOpts.Issuer),
            ValidIssuer = jwtOpts.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtOpts.Audience),
            ValidAudience = jwtOpts.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("RequireTenant", p => p.RequireClaim("tenant"));
    o.AddPolicy("AdminOnly", p => p.RequireRole("Owner", "Admin"));
    o.AddPolicy("ManageInventory", p => p.RequireRole("Owner", "Admin", "Manager"));
    o.AddPolicy("ManageOrders", p => p.RequireRole("Owner", "Admin", "Manager"));
});

var app = builder.Build();

// Seeder on startup (Dev/Local as configured)
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

// Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API V1");
        c.RoutePrefix = string.Empty; // swagger at root
    });
}

app.UseHttpsRedirection();

// Global exception handler (nëse ke ExceptionMiddleware)
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();

// Multi-tenant header middleware (vendos X-Tenant-ID -> TenantContext)
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthorization();

app.MapControllers();

// redirect root → swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// apply migrations + seed (nëse ke extensionin)
await app.MigrateAndSeedAsync();

app.Run();
