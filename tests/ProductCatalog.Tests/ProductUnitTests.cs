
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCatalog.Api;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;
using Xunit;

namespace ProductCatalog.Tests;

public class ProductUnitTests
{
    private readonly WebApplicationFactory<ApiAssemblyMarker> _factory;

    public ProductUnitTests()
    {
        var databaseName = "UnitTestingDb_" + Guid.NewGuid();

        _factory = new WebApplicationFactory<ApiAssemblyMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    [Fact]
    public async Task AddProduct_ReturnsCreated_WhenProductIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();

        var product = new Product
        {
            Name = "Clavier Mecanique",
            Price = 89.50m
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/create-product",
            product
        );

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdProduct =
            await response.Content.ReadFromJsonAsync<Product>();

        Assert.NotNull(createdProduct);
        Assert.Equal("Clavier Mecanique", createdProduct.Name);
        Assert.Equal(89.50m, createdProduct.Price);
        Assert.True(createdProduct.Id > 0);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsOkAndProductList()
    {
        // Arrange
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            db.Products.Add(new Product
            {
                Name = "Ecran 27 pouces",
                Price = 250.00m
            });

            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync(
            "/api/get-all-products"
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products =
            await response.Content.ReadFromJsonAsync<List<Product>>();

        Assert.NotNull(products);
        Assert.Single(products);

        Assert.Equal("Ecran 27 pouces", products[0].Name);   
        Assert.Equal(250.00m, products[0].Price);
    }
}
