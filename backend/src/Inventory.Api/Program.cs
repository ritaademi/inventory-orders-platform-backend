using FluentValidation.AspNetCore;
using FluentValidation;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Inventory.Api.Middleware;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<TenantResolverMiddleware>();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
