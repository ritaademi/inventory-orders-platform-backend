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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<InventoryDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Default")
             ?? "Host=localhost;Database=inventory;Username=postgres;Password=postgres";
    opt.UseNpgsql(cs);
});

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
