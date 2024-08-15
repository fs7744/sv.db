using SV.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UT
{
    public class InstanceCacheTest
    {
        [Fact]
        public void ReferenceEqualsTest()
        {
            Assert.True(ReferenceEquals(InstanceCache<DynamicRecordFactory<object>>.Instance,InstanceCache<DynamicRecordFactory<object>>.Instance));
            Assert.True(ReferenceEquals(InstanceCache<DynamicRecordFactory<dynamic>>.Instance, InstanceCache<DynamicRecordFactory<dynamic>>.Instance));
        }
    }
}
