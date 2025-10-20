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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

if (env.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// ---------------------- SERVICES (para builder.Build) ----------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory API", Version = "v1" });

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

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<Inventory.Infrastructure.Tenancy.ITenantContext, Inventory.Infrastructure.Tenancy.TenantContext>();

// Lexo "DefaultConnection" (ose bie te "Default" si fallback)
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No connection string found (DefaultConnection/Default).");

builder.Services.AddDbContext<InventoryDbContext>(opt =>
    opt.UseNpgsql(connectionString));

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

// Infrastructure (DI shtesë nga projekti)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<Inventory.Api.Services.IStockMovementService, Inventory.Api.Services.StockMovementService>();
builder.Services.AddMemoryCache();
// CORS – DUHET të jetë para Build()
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("fe", p => p
        .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---------------------- BUILD ----------------------
var app = builder.Build();

// ---------------------- MIDDLEWARE ----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("fe");
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseMiddleware<Inventory.Infrastructure.Tenancy.TenantResolverMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

// Migrime + Seed opsionale (për PR pa DB lokale)
if (app.Configuration.GetValue<bool>("RunMigrations"))
{
    await app.MigrateAndSeedAsync();
}

app.Run();
