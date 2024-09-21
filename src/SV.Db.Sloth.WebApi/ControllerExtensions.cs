using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SV.Db;
using SV.Db.Sloth;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ControllerExtensions
    {
        public static IDictionary<string, StringValues> GetQueryParams(this ControllerBase controller)
        {
            return controller.HttpContext.Request.Query.ToDictionary(StringComparer.OrdinalIgnoreCase);
        }

        public static PageResult<dynamic> QueryByParams<T>(this ControllerBase controller)
        {
            var factory = controller.HttpContext.RequestServices.GetRequiredService<IConnectionFactory>();
            var ps = controller.GetQueryParams();
            (var key, var statement) = From.ParseByParams<T>(ps).Build();
            return factory.ExecuteQuery<dynamic>(key, statement);
        }

        public static Task<PageResult<dynamic>> QueryByParamsAsync<T>(this ControllerBase controller)
        {
            var factory = controller.HttpContext.RequestServices.GetRequiredService<IConnectionFactory>();
            var ps = controller.GetQueryParams();
            (var key, var statement) = From.ParseByParams<T>(ps).Build();
            return factory.ExecuteQueryAsync<dynamic>(key, statement);
        }
    }
}