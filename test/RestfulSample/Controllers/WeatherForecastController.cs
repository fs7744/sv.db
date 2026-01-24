using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
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
            // var r = await dd.QueryAsync<string>().ToListAsync();
            return new { TotalCount = t };
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

        [HttpPut("TEST")]
        [DbSwaggerByType(typeof(UpdateOrderTransaction))]
        public async Task<object> QueryUserInfo(UpdateOrderTransaction[] transactions)
        {
            var masterNumber = transactions.First().MasterNumber;
            var ids = transactions.Select(i => i.TransactionNumber).ToArray();
            //var statement = factory.ParseByParams<UpdateOrderTransaction>(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
            //{
            //    { "MasterNumber", masterNumber.ToString() },
            //    { "TransactionNumber", $"{{{{in}}}}{System.Text.Json.JsonSerializer.Serialize(ids)}" },
            //    { "Fields", "TransactionNumber"},
            //    { "Rows", ids.Length.ToString()}
            //}, out var info);
            //return await factory.ExecuteQueryAsync<UpdateOrderTransaction>(info, statement);
            return await factory.From<UpdateOrderTransaction>().Where(i => i.MasterNumber == masterNumber && i.TransactionNumber.In(ids))
               .Limit(ids.Length)
               .Select(nameof(UpdateOrderTransaction.TransactionNumber))
               .ExecuteQueryAsync<UpdateOrderTransaction>();
            return await this.QueryByParamsAsync<UpdateOrderTransaction>();
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

        [HttpPost("db/export")]
        public async Task<object> ExportData([FromBody] DbCodeRequest req)
        {
            var c = new MySqlConnection(req.Db);
            var t = DbCodeGenerater.GetMysqlTableData(c, req.Table);
            try
            {
                c.Open();
                return await c.ExecuteQueryAsync<object>($"select {string.Join(",", t.Columns.Select(i => i.ColumnName))} from {req.Table} {(string.IsNullOrWhiteSpace(req.Where) ? "" : req.Where)}", behavior: CommandBehavior.SingleResult)
                    .ToListAsync();
            }
            finally
            {
                c.Close();
            }
        }
    }

    public class DbCodeRequest
    {
        public string Db { get; set; }
        public string? Table { get; set; }
        public string? DbType { get; set; }
        public string? Where { get; set; }
        public List<Dictionary<string, object>>? Data { get; set; }
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

    [Db(StaticInfo.Demo)]
    [Table("""
        select {Fields}
        FROM ci_order_transaction
        {where}
        """, UpdateTable = "ci_order_transaction")]
    public class UpdateOrderTransaction
    {
        [Select("TransactionNumber"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "TransactionNumber", PrimaryKey = true, NotAllowInsert = true)]
        public int TransactionNumber { get; set; }

        [Select("MasterNumber"), Where, OrderBy, Column(Type = System.Data.DbType.Int32)]
        public int MasterNumber { get; set; }

        [Select("SellerSONumber"), Where, Update]
        public string? SellerSONumber { get; set; }

        [Select("ChannelOrderNumber"), Where, Update]
        public string? ChannelOrderNumber { get; set; }

        [Select("SellerPartNumber"), Where, Update]
        public string? SellerPartNumber { get; set; }

        [Select("VendorPartNumber"), Where, Update]
        public string? VendorPartNumber { get; set; }

        [Select("ChannelSKU"), Where, Update]
        public string? ChannelSKU { get; set; }

        [Select("ChannelItemKey"), Where, Update]
        public string? ChannelItemKey { get; set; }

        [Select("ItemListingTransactionNumber"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "ItemListingTransactionNumber")]
        public int? ItemListingTransactionNumber { get; set; }

        [Select("ItemTransactionNumber"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "ItemTransactionNumber")]
        public int? ItemTransactionNumber { get; set; }

        [Select("Description"), Where, Update]
        public string? Description { get; set; }

        [Select("UnitPrice"), Where, OrderBy, Column(Type = System.Data.DbType.Decimal), Update(Field = "UnitPrice")]
        public decimal? UnitPrice { get; set; }

        [Select("OrderedQuantity"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "OrderedQuantity")]
        public int? OrderedQuantity { get; set; }

        [Select("OriginalTransactionNumber"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "OriginalTransactionNumber")]
        public int? OriginalTransactionNumber { get; set; }

        [Select("ShippedQuantity"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "ShippedQuantity")]
        public int? ShippedQuantity { get; set; }

        [Select("TaxAmount"), Where, OrderBy, Column(Type = System.Data.DbType.Decimal), Update(Field = "TaxAmount")]
        public decimal? TaxAmount { get; set; }

        [Select("ShippingCharge"), Where, OrderBy, Column(Type = System.Data.DbType.Decimal), Update(Field = "ShippingCharge")]
        public decimal? ShippingCharge { get; set; }

        [Select("Memo"), Where, Update]
        public string? Memo { get; set; }

        [Select("LastEditUser"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "LastEditUser")]
        public int? LastEditUser { get; set; }

        [Select("LastEditDate"), Where, OrderBy, Column(Type = System.Data.DbType.Int64), Update(Field = "LastEditDate")]
        public long? LastEditDate { get; set; }

        [Select("CancelQuantity"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "CancelQuantity")]
        public int? CancelQuantity { get; set; }

        [Select("OriginalItemListingTransactionNumber"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "OriginalItemListingTransactionNumber")]
        public int? OriginalItemListingTransactionNumber { get; set; }

        [Select("OriginalItemTransactionNumber"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "OriginalItemTransactionNumber")]
        public int? OriginalItemTransactionNumber { get; set; }

        [Select("Type"), Where, OrderBy, Column(Type = System.Data.DbType.Int32), Update(Field = "Type")]
        public int? Type { get; set; }

        [Select("OriginalChannelItemKey"), Where, Update]
        public string? OriginalChannelItemKey { get; set; }

        [Select("ChannelData"), Column(IsJson = true, Type = System.Data.DbType.String, CustomConvertToDbMethod = "RestfulSample.Controllers.UpdateOrderTransaction.T", CustomConvertFromDbMethod = "RestfulSample.Controllers.UpdateOrderTransaction.F"), Update(Field = "ChannelData")]
        public object? ChannelData { get; set; }

        public static string? T(object c)
        {
            return System.Text.Json.JsonSerializer.Serialize(c);
        }

        public static object? F(object? c)
        {
            return c != null ? System.Text.Json.JsonSerializer.Deserialize<object>(c.ToString()) : null;
        }
    }
}