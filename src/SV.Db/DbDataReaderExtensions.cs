using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    internal static class RecordFactoryCache<T>
    {
        public static RecordFactory<T> Cache { get; set; }
    }

    internal static class ScalarFactoryCache<T>
    {
        public static ScalarFactory<T> Cache { get; set; }
    }

    public static class DbDataReaderExtensions
    {
        static DbDataReaderExtensions()
        {
            RecordFactoryCache<object>.Cache = new DynamicRecordFactory<object>();
            RecordFactoryCache<IDataRecord>.Cache = new DynamicRecordFactory<IDataRecord>();
            RecordFactoryCache<DbDataRecord>.Cache = new DynamicRecordFactory<DbDataRecord>();

            ScalarFactoryCache<string>.Cache = new ScalarFactoryString();
            ScalarFactoryCache<int>.Cache = new ScalarFactoryInt();
            ScalarFactoryCache<int?>.Cache = new ScalarFactoryIntNull();
            ScalarFactoryCache<bool>.Cache = new ScalarFactoryBoolean();
            ScalarFactoryCache<bool?>.Cache = new ScalarFactoryBooleanNull();
            ScalarFactoryCache<float>.Cache = new ScalarFactoryFloat();
            ScalarFactoryCache<float?>.Cache = new ScalarFactoryFloatNull();
            ScalarFactoryCache<double>.Cache = new ScalarFactoryDouble();
            ScalarFactoryCache<double?>.Cache = new ScalarFactoryDoubleNull();
            ScalarFactoryCache<decimal>.Cache = new ScalarFactoryDecimal();
            ScalarFactoryCache<decimal?>.Cache = new ScalarFactoryDecimalNull();
            ScalarFactoryCache<DateTime>.Cache = new ScalarFactoryDateTime();
            ScalarFactoryCache<DateTime?>.Cache = new ScalarFactoryDateTimeNull();
            ScalarFactoryCache<Guid>.Cache = new ScalarFactoryGuid();
            ScalarFactoryCache<Guid?>.Cache = new ScalarFactoryGuidNull();
            ScalarFactoryCache<long>.Cache = new ScalarFactoryLong();
            ScalarFactoryCache<long?>.Cache = new ScalarFactoryLongNull();
            ScalarFactoryCache<short>.Cache = new ScalarFactoryShort();
            ScalarFactoryCache<short?>.Cache = new ScalarFactoryShortNull();
            ScalarFactoryCache<byte>.Cache = new ScalarFactoryByte();
            ScalarFactoryCache<byte?>.Cache = new ScalarFactoryByteNull();
            ScalarFactoryCache<char>.Cache = new ScalarFactoryChar();
            ScalarFactoryCache<char?>.Cache = new ScalarFactoryCharNull();
        }

        public static void RegisterRecordFactory<T>(RecordFactory<T> factory)
        {
            RecordFactoryCache<T>.Cache = factory;
        }

        public static T Read<T>(this IDataReader reader) where T : new()
        {
            var t = RecordFactoryCache<T>.Cache;
            if (t != null)
            {
                return t.Read(reader);
            }

            throw new NotSupportedException();
        }

        public static IEnumerable<T> ReadEnumerable<T>(this IDataReader reader, bool useBuffer = true) where T : new()
        {
            var t = RecordFactoryCache<T>.Cache;
            if (t != null)
            {
                return useBuffer
                    ? t.ReadBuffed(reader)
                    : t.ReadUnBuffed(reader);
            }

            throw new NotSupportedException();
        }

        public static T ReadScalar<T>(this IDataReader reader)
        {
            var t = GetScalarFactory<T>();
            return t.Read(reader);
        }

        public static IEnumerable<T> ReadScalarEnumerable<T>(this IDataReader reader, bool useBuffer = true)
        {
            var t = GetScalarFactory<T>();
            return useBuffer
                    ? t.ReadBuffed(reader)
                    : t.ReadUnBuffed(reader);
        }

        [MethodImpl(DBUtils.Optimization)]
        private static ScalarFactory<T> GetScalarFactory<T>()
        {
            var t = ScalarFactoryCache<T>.Cache;
            if (t == null)
            {
                var ty = typeof(T);
                if (ty.IsEnum)
                {
                    t = ScalarFactoryCache<T>.Cache = new ScalarFactoryEnum<T>();
                }
                else if (ty.IsGenericType && typeof(Nullable<>) == ty.GetGenericTypeDefinition() && Nullable.GetUnderlyingType(ty).IsEnum)
                {
                    t = ScalarFactoryCache<T>.Cache = (ScalarFactory<T>)Activator.CreateInstance(typeof(ScalarFactoryEnumNull<>).MakeGenericType(Nullable.GetUnderlyingType(ty)));
                }
                else
                {
                    t = ScalarFactoryCache<T>.Cache = new ScalarFactory<T>();
                }
            }

            return t;
        }
    }
}