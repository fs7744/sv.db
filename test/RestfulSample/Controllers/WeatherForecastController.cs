using Microsoft.AspNetCore.Mvc;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using SV.Db.Sloth.Swagger;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

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

        public WeatherForecastController(IConnectionFactory factory)
        {
            this.factory = factory;
        }

        [DbSwaggerByType(typeof(Weather))]
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
            return await factory.From<Weather>().Where(i => !i.Name.Like("e")).WithTotalCount().ExecuteQueryAsync<Weather>();
        }

        [HttpGet("querystring")]
        public object Doquerystring()
        {
            return factory.From<Weather>().Where(i => !i.Name.Like("e")).WithTotalCount().ParseToQueryString();
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

        [HttpPost("new")]
        public async Task<object> Insert([FromBody] Weather weather)
        {
            return await factory.ExecuteInsertRowAsync<Weather, int>(weather);
        }

        [HttpPost("new-batch")]
        public async Task<object> Insert([FromBody] Weather[] weather)
        {
            return await factory.ExecuteInsertAsync<Weather>(weather);
        }

        [HttpPost("update")]
        public async Task<object> Update([FromBody] Weather[] weather)
        {
            return await factory.ExecuteUpdateAsync<Weather>(weather);
        }

        [HttpGet("TEST")]
        [DbSwaggerByType(typeof(UserInfo))]
        public async Task<object> QueryUserInfo()
        {
            return await this.QueryByParamsAsync<UserInfo>();
        }

        [HttpPost("account/profile")]
        public async Task<object> UpdateAccountProfile([FromBody, Required] AccountProfile accountProfile)
        {
            return await factory.ExecuteUpdateAsync<AccountProfile>(accountProfile);
            //return await factory.ExecuteInsertAsync(accountProfile);
            //var has = (await factory.From<AccountProfile>().Where(i => i.AccountId == accountProfile.AccountId && i.Key == accountProfile.Key).Select(nameof(AccountProfile.AccountId)).Limit(1).ExecuteQueryAsync<int>()).Rows?.Count > 0;
            //if (has)
            //{
            //    return await factory.ExecuteUpdateAsync(accountProfile);
            //}
            //else
            //{
            //}
        }
    }

    [Db(StaticInfo.Demo)]
    [Table("select {Fields} from Weather a", UpdateTable = "Weather")]
    public class Weather
    {
        [Select("id"), Where, OrderBy, Update(Field = "Id", PrimaryKey = true, NotAllowInsert = true)]
        public int Id { get; set; }

        [Select("Name"), Where, OrderBy, Update]
        public string? Name { get; set; }

        [Select("Value"), Where, OrderBy, Column(IsJson = true, Type = System.Data.DbType.String, CustomConvertToDbMethod = "RestfulSample.Controllers.Weather.C"), Update(Field = "Value")]
        public object? V { get; set; }

        [Select("json_extract(Value,'$.a')")]
        public string? Vv { get; set; }

        [Select("Test", NotAllow = true)]
        public string? Test { get; set; }

        [Where(Field = """
            EXISTS(SELECT 1 FROM Weather e WHERE e.name = a.name and e.name {field} LIMIT 1)
            """)]
        public string? SKU { get; set; }

        //[Select("a.LastLoginDate"), OrderBy, Where, Column(Name = "LastLoginDate", Type = DbType.Int64), Update]
        //public long? LastLoginDate { get; set; }

        public static object C(object c)
        {
            return System.Text.Json.JsonSerializer.Serialize(c);
        }
    }

    [Db(StaticInfo.Demo)]
    [Table("""
        select {Fields}
        FROM users a
        {where}
        """, UpdateTable = "users")]
    public class UserInfo
    {
        [Select("a.Id"), OrderBy, Where, Column(Name = "Id", Type = DbType.Int32), Update(PrimaryKey = true, NotAllowInsert = true)]
        public int? Id { get; set; }

        [Select("a.DisplayName"), OrderBy, Where, Column(Name = "DisplayName"), Update]
        public string? DisplayName { get; set; }

        [Select("a.Password"), OrderBy, Where, Column(Name = "Password"), Update]
        public string? Password { get; set; }

        [Select("a.Email"), OrderBy, Where, Column(Name = "Email"), Update]
        public string? Email { get; set; }

        [Select("a.LastLoginDate"), OrderBy, Column(Name = "LastLoginDate", Type = DbType.Int64), Update]
        public long? LastLoginDate { get; set; }

        [Select("a.InDate"), OrderBy, Where, Column(Name = "InDate", Type = DbType.Int64), Update]
        public long InDate { get; set; }
    }

    [Db(StaticInfo.Demo)]
    [Table("""
        select {Fields}
        FROM account_profile a
        {where}
        """, UpdateTable = "account_profile")]
    public class AccountProfile
    {
        [Select("a.AccountId"), OrderBy, Where, Column(Name = "AccountId", Type = DbType.Int32), Update(PrimaryKey = true)]
        public int AccountId { get; set; }

        [Select("a.Key"), OrderBy, Where, Column(Name = "Key"), Update(PrimaryKey = true)]
        public string Key { get; set; }

        [Select("a.Value"), OrderBy, Where, Column(Name = "Value", Type = DbType.String, IsJson = true, CustomConvertToDbMethod = "RestfulSample.Controllers.AccountProfile.ToJsonString"), Update]
        public object Value { get; set; }

        [Select("a.LastEditDate"), OrderBy, Where, Column(Name = "LastEditDate", Type = DbType.Int64), Update]
        public long LastEditDate { get; set; }

        public static string? ToJsonString(object? v)
        {
            return v == null ? null : JsonSerializer.Serialize(v);
        }
    }
}