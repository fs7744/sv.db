using RestfulSample.Controllers;
using SV.Db;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
DictionaryConnectionStringProvider.Instance.Add("demo", ConnectionStringProvider.SQLite, "Data Source=InMemorySample;Mode=Memory;Cache=Shared");
DictionaryConnectionStringProvider.Instance.Add("es", ConnectionStringProvider.Elasticsearch, "http://rat.xxx.lt");
builder.Services.AddSQLite().AddElasticsearch().AddConnectionStringProvider(i => DictionaryConnectionStringProvider.Instance);
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
    )
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();