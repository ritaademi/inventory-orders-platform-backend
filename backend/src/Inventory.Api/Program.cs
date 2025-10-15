    using Inventory.Api.Data;
    using Inventory.Api.Services;
    using System.Text;
    using Inventory.Api.Auth;
    using Inventory.Domain.Users;
    using FluentValidation.AspNetCore;
    using FluentValidation;
    using Inventory.Infrastructure.Persistence;
    using Inventory.Infrastructure.Tenancy;
    using Microsoft.EntityFrameworkCore;
    using Inventory.Api.Middleware; 
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using Npgsql;
    using Inventory.Api.Seeding;

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

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Regjistro DbContext për lidhjen me bazën e të dhënave PostgreSQL
    builder.Services.AddDbContext<InventoryDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default")
             ?? "Host=localhost;Database=inventory;Username=postgres;Password=postgres";
    options.UseNpgsql(cs);
});


    // Regjistro shërbimin StockMovementService për Dependency Injection
    builder.Services.AddScoped<IStockMovementService, StockMovementService>();
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<DbSeeder>();

    var jwtOpts = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Key));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

    // Aktivizo Swagger në zhvillim
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API V1");
            c.RoutePrefix = string.Empty;  // Swagger në rrugën kryesore: http://localhost:port/
        });
    }

    // Aktivizo HTTPS
    app.UseHttpsRedirection();

    // Mape për controller-at
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<TenantResolverMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Rruga kryesore e redirect për Swagger
    app.MapGet("/", () => Results.Redirect("/swagger"));

    await app.MigrateAndSeedAsync();

    app.Run();
