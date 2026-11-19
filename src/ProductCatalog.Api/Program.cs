
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration de la connexion SQL Server
var connectionString =
    builder.Configuration.GetConnectionString("CatalogDbConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Activation d'Endpoints API Explorer et Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Activation de Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Auto-création de la base de données au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Création du schéma de la base de données
    db.Database.EnsureCreated();
}

// 1. Endpoint Healthcheck
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck");

// 2. Endpoint : récupérer tous les produits
app.MapGet("/api/get-all-products", async (AppDbContext db) =>
    await db.Products.AsNoTracking().ToListAsync())
.WithName("GetAllProducts");

// 3. Endpoint : ajouter un produit
app.MapPost("/api/create-product", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/api/create-product/{product.Id}", product);
})
.WithName("CreateProduct");

// 4. Endpoint : récupérer un produit par son ID
app.MapGet("/api/get-product/{id:int}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.AsNoTracking()
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

// Démarrage de l'application
app.Run();
