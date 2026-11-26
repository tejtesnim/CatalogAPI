
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;
using Xunit;

namespace ProductCatalog.Tests;

public class ProductIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        // Récupération de la chaîne de connexion
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__CatalogDbConnection")
            ?? "Server=localhost,1433;Database=CatalogTestDb;User Id=sa;Password=DevOpsPass2026!#;TrustServerCertificate=True;";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Supprimer la configuration précédente du DbContext
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();

                // Utiliser le vrai SQL Server
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                });
            });
        });
    }

    // Test d'intégration : vérifier la persistance dans SQL Server
    [Fact]
    public async Task AddProduct_And_VerifyPersistenceInRealDatabase()
    {
        var client = _factory.CreateClient();

        var uniqueName =
            "Serveur-Rack-" + Guid.NewGuid().ToString("N").Substring(0, 6);

        var product = new Product
        {
            Name = uniqueName,
            Price = 1299.99m
        };

        // 1. Insertion via l'endpoint API
        var postResponse = await client.PostAsJsonAsync(
            "/api/create-product",
            product
        );

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        // 2. Vérification directe dans SQL Server
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var persistedProduct = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == uniqueName);

        // 3. Vérification de la persistance
        Assert.NotNull(persistedProduct);
        Assert.Equal(uniqueName, persistedProduct.Name);
        Assert.Equal(1299.99m, persistedProduct.Price);
    }
}
