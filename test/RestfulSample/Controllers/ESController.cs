﻿using Microsoft.AspNetCore.Mvc;
using SV.Db;
using SV.Db.Sloth.Attributes;
using SV.Db.Sloth.Swagger;

namespace RestfulSample.Controllers
{
    [ApiController]
    [Route("es")]
    public class ESController : ControllerBase
    {
        private readonly IConnectionFactory factory;

        public ESController(IConnectionFactory factory)
        {
            this.factory = factory;
        }

        [DbSwaggerByType(typeof(TestLog))]
        [HttpGet]
        public async Task<object> Selects()
        {
            return await this.QueryByParamsAsync<TestLog>();
        }
    }

    [Table("dev_platform_access*")]
    [Db("es")]
    public class TestLog
    {
        [Select("verb")]
        public string? Verb { get; set; }
    }
}