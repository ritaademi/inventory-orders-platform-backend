using Inventory.Api.Data;
using Inventory.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Shtoni shërbimet për controllers
builder.Services.AddControllers();

// Shtoni Swagger për dokumentimin e API-së
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Regjistro DbContext për lidhjen me bazën e të dhënave PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default")
             ?? "Host=localhost;Database=inventory;Username=postgres;Password=postgres";
    options.UseNpgsql(cs);
});

// Regjistro shërbimin StockMovementService për Dependency Injection
builder.Services.AddScoped<IStockMovementService, StockMovementService>();

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
app.MapControllers();

// Rruga kryesore e redirect për Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
