using System.Data;
using Dapper;

namespace Products.API.Data;

public class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
    public override Guid Parse(object value) => value switch
    {
        string s => Guid.Parse(s),
        byte[] bytes => new Guid(bytes),
        _ => Guid.Parse(value.ToString()!)
    };
}
