using RestfulSample.Controllers;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
DictionaryConnectionStringProvider.Instance.Add("demo", ConnectionStringProvider.SQLite, "Data Source=InMemorySample;Mode=Memory;Cache=Shared");
DictionaryConnectionStringProvider.Instance.Add("es", ConnectionStringProvider.Elasticsearch, "http://rat.xxx.lt");
builder.Services.AddSQLite().AddElasticsearch().AddMySql().AddConnectionStringProvider(i => DictionaryConnectionStringProvider.Instance);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.AddDbSwagger();
});

var app = builder.Build();

var f = app.Services.GetRequiredService<IConnectionFactory>();
var o = f.GetConnection(StaticInfo.Demo);
o.Open();
var a = f.GetConnection(StaticInfo.Demo);
a.ExecuteNonQuery("""
    CREATE TABLE Weather (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT,
        value text
    );

    CREATE TABLE account_profile (
        AccountId INTEGER,
        Key TEXT,
        Value text,
    LastEditDate INTEGER
    );
    """);
a.ExecuteNonQuery("""
    INSERT INTO Weather
    (name, value)
    VALUES ('Hello', '{"a":2,"d":"sdsdadad"}'),('A', '{"a":3,"c":[4,5,{"f":7}],"d":"xxxxx"}')
    """);
var dd = a.ExecuteQuery<string>("""
    SELECT *
    FROM Weather
    """).AsList();

var aa = await f.From<PriceAdjustmentListing>().Where(i => i.TransactionNumber == 494).Limit(int.MaxValue).ExecuteQueryAsync<PriceAdjustmentListing>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

[Db("ToolBox")]
[Table("""
        select {Fields}
        FROM ci_price_adjustment_listing a
        INNER JOIN ci_price_adjustment_rule r on a.RuleTransactionNumber = r.TransactionNumber
        inner join ci_item_listing c on a.ListingTransactionNumber = c.TransactionNumber and a.AccountId = c.AccountId
        left join ci_item_listing cc on a.SyncListingTransactionNumber = cc.TransactionNumber and a.AccountId = cc.AccountId
        {where}
        """, UpdateTable = "ci_price_adjustment_listing")]
public class PriceAdjustmentListing
{
    [Select("a.TransactionNumber"), OrderBy, Where, Column(Name = "TransactionNumber", Type = DbType.Int32), Update(PrimaryKey = true, NotAllowInsert = true)]
    public int? TransactionNumber { get; set; }

    [Select("a.AccountId"), OrderBy, Where, Column(Name = "AccountId", Type = DbType.Int32), Update]
    public int? AccountId { get; set; }

    [Select("a.InUser"), OrderBy, Where, Column(Name = "InUser", Type = DbType.Int32), Update]
    public int? InUser { get; set; }

    [Select("a.LastEditUser"), OrderBy, Where, Column(Name = "LastEditUser", Type = DbType.Int32), Update]
    public int? LastEditUser { get; set; }

    [Select("a.InDate"), OrderBy, Where, Column(Name = "InDate", Type = DbType.Int64), Update]
    public long? InDate { get; set; }

    [Select("a.LastEditDate"), OrderBy, Where, Column(Name = "LastEditDate", Type = DbType.Int64), Update]
    public long? LastEditDate { get; set; }

    [Select("a.ListingTransactionNumber"), OrderBy, Where, Column(Name = "ListingTransactionNumber", Type = DbType.Int32), Update]
    public int? ListingTransactionNumber { get; set; }

    [Select("a.RuleTransactionNumber"), OrderBy, Where, Column(Name = "RuleTransactionNumber", Type = DbType.Int32), Update]
    public int? RuleTransactionNumber { get; set; }

    [Select("a.SyncListingTransactionNumber"), OrderBy, Where, Column(Name = "SyncListingTransactionNumber", Type = DbType.Int32), Update]
    public int? SyncListingTransactionNumber { get; set; }

    [Select("a.Enabled"), OrderBy, Where, Column(Name = "Enabled", Type = DbType.Boolean), Update]
    public bool? Enabled { get; set; }

    [Select("a.PriceMin"), OrderBy, Where, Column(Name = "PriceMin", Type = DbType.Decimal), Update]
    public decimal? PriceMin { get; set; }

    [Select("a.PriceMax"), OrderBy, Where, Column(Name = "PriceMax", Type = DbType.Decimal), Update]
    public decimal? PriceMax { get; set; }

    [Select("c.ItemName"), OrderBy(Field = "c.ItemName"), Where(Field = "c.ItemName")]
    public string? ItemName { get; set; }

    [Select("c.ItemImage"), OrderBy(Field = "c.ItemImage"), Where(Field = "c.ItemImage")]
    public string? ItemImage { get; set; }

    [Select("c.Channel"), OrderBy(Field = "c.Channel"), Where(Field = "c.Channel")]
    public int? Channel { get; set; }

    [Select("c.StoreSKU"), OrderBy(Field = "c.StoreSKU"), Where(Field = "c.StoreSKU")]
    public string? StoreSKU { get; set; }

    [Select("c.ChannelSKU"), OrderBy(Field = "c.ChannelSKU"), Where(Field = "c.ChannelSKU")]
    public string? ChannelSKU { get; set; }

    [Select("c.ChannelItemId"), OrderBy(Field = "c.ChannelItemId"), Where(Field = "c.ChannelItemId")]
    public string? ChannelItemId { get; set; }

    [Select("c.CurrencyCode"), OrderBy(Field = "c.CurrencyCode"), Where(Field = "c.CurrencyCode")]
    public string? CurrencyCode { get; set; }

    [Select("c.MerchantID"), OrderBy(Field = "c.MerchantID"), Where(Field = "c.MerchantID")]
    public int? MerchantID { get; set; }

    [Select("cc.ItemName"), OrderBy(Field = "cc.ItemName"), Where(Field = "cc.ItemName")]
    public string? SyncItemName { get; set; }

    [Select("cc.ItemImage"), OrderBy(Field = "cc.ItemImage"), Where(Field = "cc.ItemImage")]
    public string? SyncItemImage { get; set; }

    [Select("cc.Channel"), OrderBy(Field = "cc.Channel"), Where(Field = "cc.Channel")]
    public int? SyncChannel { get; set; }

    [Select("cc.StoreSKU"), OrderBy(Field = "cc.StoreSKU"), Where(Field = "cc.StoreSKU")]
    public string? SyncStoreSKU { get; set; }

    [Select("cc.ChannelSKU"), OrderBy(Field = "cc.ChannelSKU"), Where(Field = "cc.ChannelSKU")]
    public string? SyncChannelSKU { get; set; }

    [Select("cc.MerchantID"), OrderBy(Field = "cc.MerchantID"), Where(Field = "cc.MerchantID")]
    public int? SyncMerchantID { get; set; }

    [Select("cc.ChannelItemId"), OrderBy(Field = "cc.ChannelItemId"), Where(Field = "cc.ChannelItemId")]
    public string? SyncChannelItemId { get; set; }

    [Select("cc.CurrencyCode"), OrderBy(Field = "cc.CurrencyCode"), Where(Field = "cc.CurrencyCode")]
    public string? SyncCurrencyCode { get; set; }

    public decimal CheckPrice(decimal price)
    {
        if (PriceMin.HasValue && PriceMin > 0 && price < PriceMin)
            return PriceMin.Value;
        if (PriceMax.HasValue && PriceMax > 0 && price > PriceMax)
            return PriceMax.Value;
        return price;
    }
}