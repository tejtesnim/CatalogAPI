
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCatalog.Api;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;
using Xunit;

namespace ProductCatalog.Tests;

public class ProductIntegrationTests :
    IClassFixture<WebApplicationFactory<ApiAssemblyMarker>>
{
    private readonly WebApplicationFactory<ApiAssemblyMarker> _factory;

    public ProductIntegrationTests(
        WebApplicationFactory<ApiAssemblyMarker> factory)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__CatalogDbConnection")
            ?? "Server=localhost,1433;Database=CatalogTestDb;User Id=sa;Password=DevOpsPass2026!#;TrustServerCertificate=True;";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                });
            });
        });
    }

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

        var postResponse = await client.PostAsJsonAsync(
            "/api/create-product",
            product
        );

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var persistedProduct = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == uniqueName);

        Assert.NotNull(persistedProduct);
        Assert.Equal(uniqueName, persistedProduct.Name);
        Assert.Equal(1299.99m, persistedProduct.Price);
    }
}
