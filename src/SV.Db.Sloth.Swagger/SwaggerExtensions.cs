using Microsoft.Extensions.DependencyInjection;
using SV.Db.Sloth.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Data;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SwaggerExtensions
    {
        public static SwaggerGenOptions AddDbSwagger(this SwaggerGenOptions options)
        {
            options.OperationFilter<SwaggerDbEntityInfoOperationFilter>();
            return options;
        }

        public static string ToJsonType(this DbType DbType)
        {
            switch (DbType)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.StringFixedLength:
                case DbType.String:
                    return "string";

                case DbType.Boolean:
                    return "boolean";

                case DbType.Currency:
                case DbType.Decimal:
                case DbType.Double:
                case DbType.Int16:
                case DbType.Int32:
                case DbType.Int64:
                case DbType.Single:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.VarNumeric:
                    return "number";

                default: return "object";
            }
        }
    }
}