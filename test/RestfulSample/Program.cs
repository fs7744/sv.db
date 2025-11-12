using RestfulSample.Controllers;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
DictionaryConnectionStringProvider.Instance.Add("demo", ConnectionStringProvider.SQLite, "Data Source=InMemorySample;Mode=Memory;Cache=Shared");
DictionaryConnectionStringProvider.Instance.Add("ToolBox", ConnectionStringProvider.MySql, "Server=172.16.170.161;Database=seller_toolbox;Uid=stdbo;Pwd=Msr5vim*Wdw;AllowUserVariables=True;UseXaTransactions=false;");
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

var aa = await f.From<ChannelCategoryAi>().Where(i => i.AiDescription == null).Limit(int.MaxValue).ExecuteQueryAsync<ChannelCategoryAi>();

var vvv = aa.Rows.Where(i => i.Channel == 7 && i.Version == "v2").ToArray();
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
        from ci_channel_category cc
        left join ci_channel_category_ai cca on cc.TransactionNumber = cca.TransactionNumber
        {where}
        """, UpdateTable = "ci_channel_category_ai")]
public class ChannelCategory
{
    [Select("cc.TransactionNumber"), OrderBy, Where, Column(Name = "TransactionNumber", Type = DbType.Int32), Update(PrimaryKey = true, NotAllowInsert = true)]
    public int? TransactionNumber { get; set; }

    [Select("cc.Name"), OrderBy, Where, Column(Name = "Name"), Update]
    public string? Name { get; set; }

    [Select("cc.Version"), OrderBy, Where, Column(Name = "Version"), Update]
    public string? Version { get; set; }

    [Select("cc.FullPath"), OrderBy, Where, Column(Name = "FullPath"), Update]
    public string? FullPath { get; set; }

    [Select("cc.IsLeaf"), OrderBy, Where, Column(Name = "IsLeaf", Type = DbType.Boolean), Update]
    public bool? IsLeaf { get; set; }

    [Select("cc.Channel"), OrderBy, Where, Column(Name = "Channel", Type = DbType.Int32), Update]
    public int? Channel { get; set; }

    [Select("cc.ParentTransactionNumber"), OrderBy, Where, Column(Name = "ParentTransactionNumber", Type = DbType.Int32), Update]
    public int? ParentTransactionNumber { get; set; }
}

[Db("ToolBox")]
[Table("""
        select {Fields}
        from ci_channel_category cc
        left join ci_channel_category_ai cca on cc.TransactionNumber = cca.TransactionNumber
        {where}
        """, UpdateTable = "ci_channel_category_ai")]
public class ChannelCategoryAi : ChannelCategory
{
    [Select("cca.AiDescription"), OrderBy, Where, Column(Name = "AiDescription"), Update]
    public string? AiDescription { get; set; }

    [Select("cca.ProductTypes"), OrderBy, Where, Column(Name = "ProductTypes"), Update]
    public string? ProductTypes { get; set; }

    [Select("cca.InDate"), OrderBy, Where, Column(Name = "InDate", Type = DbType.Int64), Update]
    public long? InDate { get; set; }
}