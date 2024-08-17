using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    internal static class RecordFactoryCache<T>
    {
        public static IRecordFactory<T> Cache { get; set; }
    }

    public static class DbDataReaderExtensions
    {
        private static Func<object> cacheFactory;

        static DbDataReaderExtensions()
        {
            RecordFactoryCache<object>.Cache = new DynamicRecordFactory<object>();
            RecordFactoryCache<IDataRecord>.Cache = new DynamicRecordFactory<IDataRecord>();
            RecordFactoryCache<DbDataRecord>.Cache = new DynamicRecordFactory<DbDataRecord>();

            RecordFactoryCache<string>.Cache = new ScalarRecordFactoryString();
            RecordFactoryCache<int>.Cache = new ScalarRecordFactoryInt();
            RecordFactoryCache<int?>.Cache = new ScalarRecordFactoryIntNull();
            RecordFactoryCache<bool>.Cache = new ScalarRecordFactoryBoolean();
            RecordFactoryCache<bool?>.Cache = new ScalarRecordFactoryBooleanNull();
            RecordFactoryCache<float>.Cache = new ScalarRecordFactoryFloat();
            RecordFactoryCache<float?>.Cache = new ScalarRecordFactoryFloatNull();
            RecordFactoryCache<double>.Cache = new ScalarRecordFactoryDouble();
            RecordFactoryCache<double?>.Cache = new ScalarRecordFactoryDoubleNull();
            RecordFactoryCache<decimal>.Cache = new ScalarRecordFactoryDecimal();
            RecordFactoryCache<decimal?>.Cache = new ScalarRecordFactoryDecimalNull();
            RecordFactoryCache<DateTime>.Cache = new ScalarRecordFactoryDateTime();
            RecordFactoryCache<DateTime?>.Cache = new ScalarRecordFactoryDateTimeNull();
            RecordFactoryCache<Guid>.Cache = new ScalarRecordFactoryGuid();
            RecordFactoryCache<Guid?>.Cache = new ScalarRecordFactoryGuidNull();
            RecordFactoryCache<long>.Cache = new ScalarRecordFactoryLong();
            RecordFactoryCache<long?>.Cache = new ScalarRecordFactoryLongNull();
            RecordFactoryCache<short>.Cache = new ScalarRecordFactoryShort();
            RecordFactoryCache<short?>.Cache = new ScalarRecordFactoryShortNull();
            RecordFactoryCache<byte>.Cache = new ScalarRecordFactoryByte();
            RecordFactoryCache<byte?>.Cache = new ScalarRecordFactoryByteNull();
            RecordFactoryCache<char>.Cache = new ScalarRecordFactoryChar();
            RecordFactoryCache<char?>.Cache = new ScalarRecordFactoryCharNull();
        }

        public static void RegisterRecordFactory<T>(RecordFactory<T> factory)
        {
            RecordFactoryCache<T>.Cache = factory;
        }

        public static void RegisterRecordFactory<T>(Func<RecordFactory<T>> factory)
        {
            cacheFactory = factory;
        }

        public static T Read<T>(this IDataReader reader)
        {
            var t = GetRecordFactory<T>();
            return t.Read(reader);
        }

        public static IEnumerable<T> ReadEnumerable<T>(this IDataReader reader, int estimateRow = 4, bool useBuffer = true)
        {
            var t = GetRecordFactory<T>();
            return useBuffer
                    ? t.ReadBuffed(reader, estimateRow)
                    : t.ReadUnBuffed(reader);
        }

        [MethodImpl(DBUtils.Optimization)]
        private static IRecordFactory<T> GetRecordFactory<T>()
        {
            var t = RecordFactoryCache<T>.Cache;
            if (t == null)
            {
                var ty = typeof(T);
                if (ty.IsEnum)
                {
                    t = RecordFactoryCache<T>.Cache = new ScalarRecordFactoryEnum<T>();
                }
                else if (ty.IsGenericType && typeof(Nullable<>) == ty.GetGenericTypeDefinition() && Nullable.GetUnderlyingType(ty).IsEnum)
                {
                    t = RecordFactoryCache<T>.Cache = (ScalarRecordFactory<T>)Activator.CreateInstance(typeof(ScalarRecordFactoryEnumNull<>).MakeGenericType(Nullable.GetUnderlyingType(ty)));
                }
                else
                {
                    t = (IRecordFactory<T>)cacheFactory?.Invoke();
                    if (t == null)
                        ThrowHelper.ThrowNotSupportedException();
                }
            }

            return t;
        }

        public static T ExecuteScalar<T>(this IDbCommand command)
        {
            return DBUtils.As<T>(command.ExecuteScalar());
        }
    }
}