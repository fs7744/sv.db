using Microsoft.AspNetCore.Mvc;
using SV.Db;
using SV.Db.Sloth.Attributes;

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
        public async Task<object> Selects()
        {
            return await this.QueryByParamsAsync<Weather>();
        }

        public WeatherForecastController(IConnectionFactory factory)
        {
            this.factory = factory;
        }

        [HttpGet("old")]
        public async Task<object> OldWay()
        {
            var a = factory.GetConnection(StaticInfo.Demo);
            var dd = await a.ExecuteQueryAsync<string>("""
    SELECT *
    FROM Weather
    """).ToListAsync();

            return dd;
        }
    }

    [Db(StaticInfo.Demo)]
    [Table(nameof(Weather))]
    public class Weather
    {
    }
}