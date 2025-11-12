using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Data;
using System.Reflection;
using System.Text.Json.Nodes;

namespace SV.Db.Sloth.Swagger
{
    public class SwaggerDbEntityInfoOperationFilter : IOperationFilter
    {
        private readonly IConnectionFactory factory;

        private MethodInfo GetDbEntityInfoOfT = typeof(IConnectionFactory).GetMethod(nameof(IConnectionFactory.GetDbEntityInfoOfT));

        public SwaggerDbEntityInfoOperationFilter(IConnectionFactory factory)
        {
            this.factory = factory;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            DbEntityInfo info = FindDbEntityInfo(context);
            if (info == null) return;
            var p = operation.Parameters;
            if (p == null)
            {
                operation.Parameters = new List<IOpenApiParameter>();
                p = operation.Parameters;
            }
            var resp = operation.Responses;
            if (resp == null)
            {
                operation.Responses = new OpenApiResponses();
                resp = operation.Responses;
            }

            p.Add(new OpenApiParameter()
            {
                In = ParameterLocation.Query,
                Name = "TotalCount",
                Schema = new OpenApiSchema()
                {
                    Type = DbType.Boolean.ToJsonType(),
                    Format = DbType.Boolean.ToString()
                }
            });
            p.Add(new OpenApiParameter()
            {
                In = ParameterLocation.Query,
                Name = "NoRows",
                Schema = new OpenApiSchema()
                {
                    Type = DbType.Boolean.ToJsonType(),
                    Format = DbType.Boolean.ToString()
                }
            });
            p.Add(new OpenApiParameter()
            {
                In = ParameterLocation.Query,
                Name = "Offset",
                Schema = new OpenApiSchema()
                {
                    Type = DbType.Int32.ToJsonType(),
                    Format = DbType.Int32.ToString()
                }
            });
            p.Add(new OpenApiParameter()
            {
                In = ParameterLocation.Query,
                Name = "Rows",
                Schema = new OpenApiSchema()
                {
                    Type = DbType.Int32.ToJsonType(),
                    Format = DbType.Int32.ToString()
                }
            });
            if (info.OrderByFields.Count > 0)
            {
                p.Add(new OpenApiParameter()
                {
                    In = ParameterLocation.Query,
                    Name = "OrderBy",
                    Schema = new OpenApiSchema()
                    {
                        Type = DbType.String.ToJsonType(),
                        Format = DbType.String.ToString()
                    },
                    Example = JsonValue.Create(string.Join(",", info.OrderByFields.Select(i => $"{i.Key}:asc")))
                });
            }
            if (info.SelectFields.Count > 0)
            {
                p.Add(new OpenApiParameter()
                {
                    In = ParameterLocation.Query,
                    Name = "Fields",
                    Schema = new OpenApiSchema()
                    {
                        Type = DbType.String.ToJsonType(),
                        Format = DbType.String.ToString()
                    },
                    Example = JsonValue.Create(string.Join(",", info.SelectFields.Where(i => i.Key != "*").Select(i => i.Key)))
                });
                resp["200"] = new OpenApiResponse()
                {
                    Content = new Dictionary<string, OpenApiMediaType>()
                     {
                         { "application/json", new OpenApiMediaType() { Example = JsonValue.Create($"{{\"totalCount\":1,\"rows\":[{{{string.Join(",",info.SelectFields.Where(i => i.Key != "*").Select(i =>
                         {
                             var dp = info.Columns.TryGetValue(i.Key, out var c) ? c.Type : DbType.String;
                             return $"\"{i.Key}\":\"{dp.ToJsonType()}\"";
                         }))}}}]}}") } }
                     }
                };
            }

            if (info.WhereFields.Count > 0)
            {
                p.Add(new OpenApiParameter()
                {
                    In = ParameterLocation.Query,
                    Name = "Where",
                    Schema = new OpenApiSchema()
                    {
                        Type = DbType.String.ToJsonType(),
                        Format = DbType.String.ToString()
                    },
                    Example = JsonValue.Create(string.Join(" and ", info.SelectFields.Select(i => $"{i.Key}=?")))
                });
                foreach (var field in info.WhereFields)
                {
                    var dp = info.Columns.TryGetValue(field.Key, out var c) ? c.Type : DbType.String;
                    p.Add(new OpenApiParameter()
                    {
                        In = ParameterLocation.Query,
                        Name = field.Key,
                        Schema = new OpenApiSchema()
                        {
                            Type = dp.ToJsonType(),
                            Format = c != null && c.IsJson ? "json" : dp.ToString()
                        },
                        Example = JsonValue.Create("{{eq}}?")
                    });
                }
            }
        }

        private DbEntityInfo FindDbEntityInfo(OperationFilterContext context)
        {
            var info = context.MethodInfo.GetCustomAttribute<DbSwaggerAttribute>();
            if (info != null)
            {
                return string.IsNullOrWhiteSpace(info.Key) ? null : factory.GetDbEntityInfo(info.Key);
            }

            var info2 = context.MethodInfo.GetCustomAttribute<DbSwaggerByTypeAttribute>();
            if (info2 != null && info2.Type != null)
            {
                var m = GetDbEntityInfoOfT.MakeGenericMethod(info2.Type);
                return (DbEntityInfo)m.Invoke(factory, null);
            }

            return null;
        }
    }
}