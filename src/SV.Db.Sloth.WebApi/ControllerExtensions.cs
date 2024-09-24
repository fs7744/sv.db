using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Statements;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ControllerExtensions
    {
        public static IDictionary<string, StringValues> GetQueryParams(this ControllerBase controller)
        {
            return controller.HttpContext.Request.Query.ToDictionary(StringComparer.OrdinalIgnoreCase);
        }

        public static object QueryByParams<T>(this ControllerBase controller)
        {
            var factory = controller.HttpContext.RequestServices.GetRequiredService<IConnectionFactory>();
            var ps = controller.GetQueryParams();
            DbEntityInfo info;
            SelectStatement statement;
            try
            {
                statement = factory.ParseByParams<T>(ps, out info);
            }
            catch (Exception ex)
            {
                return controller.BadRequest(new
                {
                    error = ex.Message
                });
            }
            return factory.ExecuteQuery<dynamic>(info, statement);
        }

        public static async Task<object> QueryByParamsAsync<T>(this ControllerBase controller, CancellationToken cancellationToken = default)
        {
            var factory = controller.HttpContext.RequestServices.GetRequiredService<IConnectionFactory>();
            var ps = controller.GetQueryParams();
            DbEntityInfo info;
            SelectStatement statement;
            try
            {
                statement = factory.ParseByParams<T>(ps, out info);
            }
            catch (Exception ex)
            {
                return controller.BadRequest(new
                {
                    error = ex.Message
                });
            }

            return await factory.ExecuteQueryAsync<dynamic>(info, statement, cancellationToken);
        }
    }
}