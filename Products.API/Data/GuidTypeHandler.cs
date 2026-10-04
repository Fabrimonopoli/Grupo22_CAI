using Dapper;
using System.Data;

namespace MiniApi.Data
{
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            // Forzamos mayúsculas y le decimos a SQLite que lo trate puramente como texto
            parameter.Value = value.ToString().ToUpper();
            parameter.DbType = DbType.String;
        }

        public override Guid Parse(object value)
        {
            return Guid.Parse((string)value);
        }
    }
}
