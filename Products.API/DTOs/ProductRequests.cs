namespace MiniApi.DTOs
{
    // -- Request para crear un producto (POST) --
    public record CreateProductRequest(
        string Nombre,
        string? Descripcion,
        decimal Precio,
        int Stock,
        string Categoria
    );

    // -- Request para actualizar un producto (PUT) --
    public record UpdateProductRequest(
        string Nombre,
        string? Descripcion,
        decimal Precio,
        int Stock,
        string Categoria
    );
}
