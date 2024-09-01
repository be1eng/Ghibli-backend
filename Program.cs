using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Configuración del servidor Kestrel para escuchar en todas las interfaces
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5071);  // Escucha en todas las interfaces en el puerto 5071
});


// Configurar conexión a MySQL
builder.Services.AddDbContext<GhibliDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8, 0, 21))));

// Configurar Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Configuration"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

// Añadir CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", // Puedes cambiar "AllowAll", por otro cuando sea produccion
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Configuración del Cliente HTTP para la API de Ghibli
builder.Services.AddHttpClient("GhibliClient", c =>
{
    c.BaseAddress = new Uri("https://ghibli.rest/");
});


// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar CORS
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware para forzar el redireccionamiento a /api/
app.Use(async (context, next) =>
{
    if (context?.Request?.Path.StartsWithSegments("/api") == false)
    {
        context.Response.Redirect("/api/");
    }
    else
    {
        await next();
    }
});



// Ruta raíz que devuelve un mensaje de bienvenida
app.MapGet("/api/", () => "Bienvenido a la API Ghibli v2!")
    .WithName("GetWelcomeMessage")
    .WithOpenApi();


app.MapGroup("/api").MapGet("/films/{id?}", async (IHttpClientFactory clientFactory, IDistributedCache cache, string? id) =>
{
    var client = clientFactory.CreateClient("GhibliClient");
    string cacheKey = string.IsNullOrEmpty(id) ? "films" : $"films_{id}";
    string cachedFilms = await cache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedFilms))
    {
        var cachedData = System.Text.Json.JsonSerializer.Deserialize<object>(cachedFilms);
        return Results.Ok(cachedData);
    }

    string requestUri = string.IsNullOrEmpty(id) ? "films" : $"films?id={id}";
    var response = await client.GetAsync(requestUri);
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        return Results.Problem(detail: errorContent, statusCode: (int)response.StatusCode, title: "Fallo al obtener datos del API de Ghibli");
    }
    
    var responseData = await response.Content.ReadFromJsonAsync<object>();
    if (responseData == null) 
    {
        return Results.Problem("No se recibieron datos del API de Ghibli", statusCode: 204);
    }
    await cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(responseData), new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    });

    return Results.Ok(responseData);
})
.WithName("GetFilmsById")
.Produces(200, typeof(object))
.ProducesProblem(404);

app.MapGroup("/api").MapGet("/locations/{id?}", async (IHttpClientFactory clientFactory, IDistributedCache cache, string? id) =>
{
    var client = clientFactory.CreateClient("GhibliClient");
    string cacheKey = string.IsNullOrEmpty(id) ? "locations" : $"locations_{id}";
    string cachedLocations = await cache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedLocations))
    {
        var cachedData = System.Text.Json.JsonSerializer.Deserialize<object>(cachedLocations);
        return Results.Ok(cachedData);
    }

    string requestUri = string.IsNullOrEmpty(id) ? "locations" : $"locations?id={id}";
    var response = await client.GetAsync(requestUri);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(detail: await response.Content.ReadAsStringAsync(), statusCode: (int)response.StatusCode);
    }

    var responseData = await response.Content.ReadFromJsonAsync<object>();
    if (responseData == null) return Results.Problem("No data received from Ghibli API", statusCode: 204);

    await cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(responseData), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

    return Results.Ok(responseData);
})
.WithName("GetLocationsById")
.Produces(200, typeof(object))
.ProducesProblem(404);

app.MapGroup("/api").MapGet("/people/{id}", async (IHttpClientFactory clientFactory, IDistributedCache cache, string id) =>
{
    var client = clientFactory.CreateClient("GhibliClient");
    string cacheKey = $"people_{id}";
    string cachedPeople = await cache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedPeople))
    {
        var cachedData = System.Text.Json.JsonSerializer.Deserialize<object>(cachedPeople);
        return Results.Ok(cachedData);
    }

    string requestUri = $"people/{id}";
    var response = await client.GetAsync(requestUri);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(detail: await response.Content.ReadAsStringAsync(), statusCode: (int)response.StatusCode);
    }

    var responseData = await response.Content.ReadFromJsonAsync<object>();
    if (responseData == null) return Results.Problem("No data received from Ghibli API", statusCode: 204);

    await cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(responseData), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

    return Results.Ok(responseData);
})
.WithName("GetPeopleById")
.Produces(200, typeof(object))
.ProducesProblem(404);


app.MapGroup("/api").MapGet("/species/{id?}", async (IHttpClientFactory clientFactory, IDistributedCache cache, string? id) =>
{
    var client = clientFactory.CreateClient("GhibliClient");
    string cacheKey = string.IsNullOrEmpty(id) ? "species" : $"species_{id}";
    string cachedSpecies = await cache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedSpecies))
    {
        var cachedData = System.Text.Json.JsonSerializer.Deserialize<object>(cachedSpecies);
        return Results.Ok(cachedData);
    }

    string requestUri = string.IsNullOrEmpty(id) ? "species" : $"species?id={id}";
    var response = await client.GetAsync(requestUri);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(detail: await response.Content.ReadAsStringAsync(), statusCode: (int)response.StatusCode);
    }

    var responseData = await response.Content.ReadFromJsonAsync<object>();
    if (responseData == null) return Results.Problem("No data received from Ghibli API", statusCode: 204);

    await cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(responseData), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

    return Results.Ok(responseData);
})
.WithName("GetSpeciesById")
.Produces(200, typeof(object))
.ProducesProblem(404);


app.MapGroup("/api").MapGet("/vehicles/{id?}", async (IHttpClientFactory clientFactory, IDistributedCache cache, string? id) =>
{
    var client = clientFactory.CreateClient("GhibliClient");
    string cacheKey = string.IsNullOrEmpty(id) ? "vehicles" : $"vehicles_{id}";
    string cachedVehicles = await cache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedVehicles))
    {
        var cachedData = System.Text.Json.JsonSerializer.Deserialize<object>(cachedVehicles);
        return Results.Ok(cachedData);
    }

    string requestUri = string.IsNullOrEmpty(id) ? "vehicles" : $"vehicles?id={id}";
    var response = await client.GetAsync(requestUri);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(detail: await response.Content.ReadAsStringAsync(), statusCode: (int)response.StatusCode);
    }

    var responseData = await response.Content.ReadFromJsonAsync<object>();
    if (responseData == null) return Results.Problem("No data received from Ghibli API", statusCode: 204);

    await cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(responseData), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

    return Results.Ok(responseData);
})
.WithName("GetVehiclesById")
.Produces(200, typeof(object))
.ProducesProblem(404);

app.MapGroup("/api").MapPost("/comments", async (GhibliDbContext dbContext, Comment comment) =>
{
    dbContext.Comments.Add(comment);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/comments/{comment.Id}", comment);
})
.WithName("AddComment")
.Produces<Comment>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status400BadRequest);


app.Run();
