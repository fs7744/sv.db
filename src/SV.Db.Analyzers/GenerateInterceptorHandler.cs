using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SV.Db.Analyzers
{
    public static class GenerateInterceptorHandler
    {
        internal static string GenerateCode(Dictionary<string, GeneratedMapping> map)
        {
            var kvs = map.Where(i => i.Value.NeedInterceptor).ToArray();
            if(kvs.Length == 0 ) return string.Empty;

            return $@"
namespace SV.Db
{{
    file static class GeneratedInterceptors_{Guid.NewGuid():N}
    {{
        
    }}
}}


namespace System.Runtime.CompilerServices
{{
    // this type is needed by the compiler to implement interceptors - it doesn't need to
    // come from the runtime itself, though

    [global::System.Diagnostics.Conditional(""DEBUG"")] // not needed post-build, so: evaporate
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
    sealed file class InterceptsLocationAttribute : global::System.Attribute
    {{
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {{
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }}
    }}
}}
";
        }
    }
}
