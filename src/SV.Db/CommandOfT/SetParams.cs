using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        private static readonly ConcurrentDictionary<Type, IParamsSetter?> paramsSetterCache = new();
        private static readonly MethodInfo? method = typeof(RecordFactory).GetMethod("GetParamsSetter");

        public static void SetParams(this DbCommand cmd, object? args = null)
        {
            if (args != null)
            {
                var t = args.GetType();
                var setter = paramsSetterCache.GetOrAdd(t, type =>
                {
                    return method.MakeGenericMethod(type).Invoke(null, null) as IParamsSetter;
                });
                setter.SetParams(cmd, args);
            }
        }

        internal static void InternalSetParams<T>(this DbCommand cmd, T args)
        {
            RecordFactory.GetParamsSetter<T>().SetParams(cmd, args);
        }

        public static void SetParams<T>(this DbCommand cmd, T args) where T : new()
        {
            RecordFactory.GetParamsSetter<T>().SetParams(cmd, args);
        }

        public static void SetParams(this DbBatchCommand cmd, object? args = null)
        {
            if (args != null)
            {
                var t = args.GetType();
                var setter = paramsSetterCache.GetOrAdd(t, type =>
                {
                    return method.MakeGenericMethod(type).Invoke(null, null) as IParamsSetter;
                });
                setter.SetParams(cmd, args);
            }
        }

        public static void SetParams<T>(this DbBatchCommand cmd, T args) where T : new()
        {
            RecordFactory.GetParamsSetter<T>().SetParams(cmd, args);
        }

        internal static void InternalSetParams<T>(this DbBatchCommand cmd, T args)
        {
            RecordFactory.GetParamsSetter<T>().SetParams(cmd, args);
        }
    }
}