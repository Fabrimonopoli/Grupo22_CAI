using MiniApi.Data;
using MiniApi.Models;

namespace MiniApi.Extensions.Endpoints
{
    // ALUMNOS: clase donde se definen los endpoints para cada entidad
    public static class ItemsEndpointsExtension
    {
        public static void MapItemEndpoints(this WebApplication app)
        {
            // GET all
            app.MapGet("/items", async (ItemRepository repo) =>
            {
                var items = await repo.GetAllAsync();
                return Results.Ok(items);
            })
            .WithTags("Items");

            // GET by id
            app.MapGet("/items/{id}", async (int id, ItemRepository repo) =>
            {
                var item = await repo.GetByIdAsync(id);
                return item is not null ? Results.Ok(item) : Results.NotFound();
            })
            .WithTags("Items");

            // POST
            app.MapPost("/items", async (CreateItemRequest req, ItemRepository repo) =>
            {
                var item = await repo.CreateAsync(req);
                return Results.Ok(item);
            })
            .WithTags("Items");

            // PUT
            app.MapPut("/items/{id}", async (int id, UpdateItemRequest req, ItemRepository repo) =>
            {
                var updated = await repo.UpdateAsync(id, req);
                return updated ? Results.Ok() : Results.NotFound();
            })
            .WithTags("Items");

            // DELETE
            app.MapDelete("/items/{id}", async (int id, ItemRepository repo) =>
            {
                var deleted = await repo.DeleteAsync(id);
                return deleted ? Results.Ok() : Results.NotFound();
            })
            .WithTags("Items");
        }
    }
}