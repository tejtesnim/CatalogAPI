using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// SQL Server
var connectionString =
    builder.Configuration.GetConnectionString("CatalogDbConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Création automatique de la base de données
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Health Check
app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow
    }))
    .WithName("HealthCheck");

// GET Products
app.MapGet("/api/products", async (AppDbContext db) =>
{
    return await db.Products
        .AsNoTracking()
        .ToListAsync();
})
.WithName("GetAllProducts");

// POST Product
app.MapPost("/api/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);

    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/products/{product.Id}",
        product
    );
})
.WithName("CreateProduct");

app.MapGet("/api/get-product/{id:int}", async (int id, AppDbContext db) =>
{
    var product = await db.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product is null)
    {
        return Results.NotFound(new
        {
            message = $"Aucun produit trouvé avec l'identifiant {id}."
        });
    }

    return Results.Ok(product);
})
.WithName("GetProductById");


app.Run();