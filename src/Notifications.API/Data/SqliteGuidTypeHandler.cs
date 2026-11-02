using System.Data;
using Dapper;

namespace Notifications.API.Data;

/// <summary>Convierte los GUID almacenados como TEXT por SQLite.</summary>
public sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
    /// <inheritdoc />
    public override Guid Parse(object value) => value switch
    {
        Guid guid => guid,
        string text => Guid.Parse(text),
        byte[] bytes => new Guid(bytes),
        _ => throw new DataException("El identificador persistido no tiene un formato GUID válido.")
    };
}
