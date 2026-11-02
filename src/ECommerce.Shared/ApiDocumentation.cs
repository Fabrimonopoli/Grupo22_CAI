using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.WebUtilities;

namespace ECommerce.Shared;

public static class ApiDocumentation
{
    private static IOpenApiAny Example(object value) => OpenApiAnyFactory.CreateFromJson(
        JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    public static void Complete(OpenApiOperation operation, OperationFilterContext context, int successStatus,
        Type? successType, object? successExample, Dictionary<int, string[]> errors, object? requestExample = null)
    {
        operation.Summary ??= $"{context.ApiDescription.HttpMethod} {context.ApiDescription.RelativePath}";
        operation.Responses.Clear();
        var success = new OpenApiResponse { Description = successStatus == 204 ? "Operación completada sin cuerpo de respuesta." : "Operación completada correctamente." };
        if (successType is not null)
            success.Content["application/json"] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(successType, context.SchemaRepository),
                Example = successExample is null ? null : Example(successExample)
            };
        operation.Responses[successStatus.ToString()] = success;
        foreach (var (status, codes) in errors)
        {
            var schema = context.SchemaGenerator.GenerateSchema(typeof(ApiError), context.SchemaRepository);
            var media = new OpenApiMediaType { Schema = schema };
            foreach (var code in codes)
            {
                var message = ErrorCatalog.Entries[code].Message;
                media.Examples[code] = new OpenApiExample
                {
                    Summary = message,
                    Value = Example(new ApiError("https://www.rfc-editor.org/rfc/rfc9110", ReasonPhrases.GetReasonPhrase(status), status,
                        message, "/" + context.ApiDescription.RelativePath?.Split('?')[0], code, message,
                        "21dfd900f4db49d8a0136f91d16350ad"))
                };
            }
            operation.Responses[status.ToString()] = new OpenApiResponse
            {
                Description = string.Join("; ", codes.Select(c => $"{c}: {ErrorCatalog.Entries[c].Message}")),
                Content = new Dictionary<string, OpenApiMediaType> { ["application/problem+json"] = media }
            };
        }
        if (operation.RequestBody is not null && requestExample is not null)
            foreach (var media in operation.RequestBody.Content.Values)
                media.Example = Example(requestExample);
        foreach (var param in operation.Parameters ?? [])
        {
            if (param.Schema?.Format == "uuid")
                param.Example = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            else if (param.Name == "categoria")
                param.Example = new OpenApiString("Electrónica");
            else if (param.Name == "nombre")
                param.Example = new OpenApiString("Notebook");
        }
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Identificador de seguimiento; el servicio lo genera si se omite.",
            Schema = new OpenApiSchema { Type = "string", MaxLength = 100 }
        });
        if (context.SchemaRepository.Schemas.TryGetValue(nameof(ApiError), out var errorSchema))
            errorSchema.Required = new HashSet<string> { "type", "title", "status", "detail", "instance", "errorCode", "errorMessage", "correlationId" };
    }
}
