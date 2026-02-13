using Microsoft.AspNetCore.Mvc;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using SV.Db.Sloth.Elasticsearch;
using SV.Db.Sloth.Swagger;

namespace RestfulSample.Controllers
{
    [ApiController]
    [Route("es")]
    public class ESController : ControllerBase
    {
        private readonly IConnectionFactory factory;
        private readonly IEsClient es;

        public ESController(IConnectionFactory factory, IEsClient es)
        {
            this.factory = factory;
            this.es = es;
        }

        [DbSwaggerByType(typeof(TestLog))]
        [HttpGet]
        public async Task<object> Selects()
        {
            await es.BulkDeleteAsync("http://xxx/api/v1/sp-es/sp-settlement-transaction", "sp-settlement-transaction", Enumerable.Range(0, 10).Select(i => i.ToString()), 10, this.HttpContext.RequestAborted);
            //await factory.ExecuteUpdateAsync(Enumerable.Range(0, 10).Select(i => new TestLog() { Id = i.ToString(), Verb = "11333" + i, Xhost = "11dd" + i }), 3);
            return await this.QueryByParamsAsync<TestLog>();
        }
    }

    [Table("sp-settlement-transaction")]
    [Db("es")]
    public class TestLog
    {
        [Select("verb"), Where]
        public string? Verb { get; set; }

        [Select("_id"), Update(PrimaryKey = true)]
        public string Id { get; set; }

        [Select("xhost"), OrderBy, Where]
        public string? Xhost { get; set; }

        [Select("xluatime"), OrderBy, Where]
        public long? xluatime { get; set; }
    }
}