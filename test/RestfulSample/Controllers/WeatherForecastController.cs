using Microsoft.AspNetCore.Mvc;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using System.ComponentModel.DataAnnotations;

namespace RestfulSample.Controllers
{
    public static class StaticInfo
    {
        public const string Demo = nameof(Demo);
    }

    [ApiController]
    [Route("weather")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IConnectionFactory factory;

        [HttpGet] //todo [QueryByParamsSwagger(typeof(Weather))]
        public async Task<object> Selects()//[FromQuery, Required] string name)
        {
            return await this.QueryByParamsAsync<Weather>();
        }

        [HttpPost] //todo [QueryByParamsSwagger(typeof(Weather))]
        public async Task<object> PostSelects()//[FromQuery, Required] string name)
        {
            return await this.QueryByBodyAsync<Weather>();
        }

        [HttpGet("expr")]
        public async Task<object> DoSelects()
        {
            return await factory.From<Weather>().Where(i => !i.Name.Like("e")).WithTotalCount().ExecuteQueryAsync<dynamic>();
        }

        [HttpGet("querystring")]
        public object Doquerystring()
        {
            return factory.From<Weather>().Where(i => !i.Name.Like("e")).WithTotalCount().ParseToQueryString();
        }

        public WeatherForecastController(IConnectionFactory factory)
        {
            this.factory = factory;
        }

        [HttpGet("old")]
        public async Task<object> OldWay()
        {
            var a = factory.GetConnection(StaticInfo.Demo);
            using var dd = await a.ExecuteReaderAsync("""
    SELECT count(1)
    FROM Weather;
    SELECT *
    FROM Weather;
    """);
            var t = await dd.QueryFirstOrDefaultAsync<int>();
            var r = await dd.QueryAsync<string>().ToListAsync();
            return new { TotalCount = t, Rows = r };
        }
    }

    [Db(StaticInfo.Demo)]
    [Table(nameof(Weather))]
    public class Weather
    {
        [Select, Where, OrderBy]
        public string Name { get; set; }

        [Select(Field = "Value as v"), Where, OrderBy]
        public string V { get; set; }

        [Select(NotAllow = true)]
        public string Test { get; set; }
    }
}