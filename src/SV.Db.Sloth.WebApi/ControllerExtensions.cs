using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Statements;
using System.Web;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ControllerExtensions
    {
        public static string ParseToQueryString(this SelectStatementBuilder builder)
        {
            var dict = From.ParseToQueryParams(builder.Build());
            return string.Join("&", dict.Select(i => $"{i.Key}={HttpUtility.UrlPathEncode(i.Value)}"));
        }

        public static string ParseToQueryString(this SelectStatement statement)
        {
            var dict = From.ParseToQueryParams(statement);
            return string.Join("&", dict.Select(i => $"{i.Key}={HttpUtility.UrlPathEncode(i.Value)}"));
        }

        public static IDictionary<string, StringValues> GetQueryParams(this ControllerBase controller)
        {
            return controller.HttpContext.Request.Query.ToDictionary(StringComparer.OrdinalIgnoreCase);
        }

        public static async ValueTask<IDictionary<string, StringValues>> GetQueryParamsByBody(this ControllerBase controller, CancellationToken cancellationToken = default)
        {
            return (await System.Text.Json.JsonSerializer.DeserializeAsync<Dictionary<string, string>>(controller.HttpContext.Request.Body, options: null, cancellationToken))
                .ToDictionary(i => i.Key, i => new StringValues( i.Value), StringComparer.OrdinalIgnoreCase);
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