using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    internal static class RecordFactoryCache<T>
    {
        public static IRecordFactory<T>? Cache { get; set; }
    }

    public static class RecordFactory
    {
        private static Func<object?> cacheFactory = () => null;

        private static readonly Dictionary<Type, DbType> dbTypeMapping = new Dictionary<Type, DbType>()
        {
            { typeof(long),             DbType.Int64 },
            { typeof(bool),             DbType.Boolean },
            { typeof(string),           DbType.String },
            { typeof(DateTime),         DbType.DateTime },
            { typeof(DateTimeOffset),         DbType.DateTimeOffset },
            { typeof(decimal),          DbType.Decimal},
            { typeof(double),           DbType.Double },
            { typeof(int),              DbType.Int32},
            { typeof(float),            DbType.Single  },
            { typeof(short),            DbType.Int16  },
            { typeof(byte),             DbType.Byte  },
            { typeof(Guid),             DbType.Guid  },
            { typeof(long?),             DbType.Int64 },
            { typeof(bool?),             DbType.Boolean },
            { typeof(DateTime?),         DbType.DateTime },
            { typeof(DateTimeOffset?),         DbType.DateTimeOffset },
            { typeof(decimal?),          DbType.Decimal},
            { typeof(double?),           DbType.Double },
            { typeof(int?),              DbType.Int32},
            { typeof(float?),            DbType.Single  },
            { typeof(short?),            DbType.Int16  },
            { typeof(byte?),             DbType.Byte  },
            { typeof(Guid?),             DbType.Guid  },
            { typeof(DBNull),             DbType.String  }
        };

        static RecordFactory()
        {
            RecordFactoryCache<object>.Cache = new DynamicRecordFactory<object>();
            RecordFactoryCache<IDataRecord>.Cache = new DynamicRecordFactory<IDataRecord>();
            RecordFactoryCache<DbDataRecord>.Cache = new DynamicRecordFactory<DbDataRecord>();
            RecordFactoryCache<IDictionary<string, object?>>.Cache = new DynamicRecordFactory<IDictionary<string, object?>>();
            RecordFactoryCache<IReadOnlyDictionary<string, object?>>.Cache = new DynamicRecordFactory<IReadOnlyDictionary<string, object?>>();
            RecordFactoryCache<Dictionary<string, object?>>.Cache = new DictionaryRecord();
            RecordFactoryCache<ConcurrentDictionary<string, object?>>.Cache = new ConcurrentDictionaryRecord();

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

        public static void RegisterDbTypeMapping<T>(DbType dbType)
        {
            dbTypeMapping[typeof(T)] = dbType;
        }

        [MethodImpl(DBUtils.Optimization)]
        internal static T? Read<T>(this DbDataReader reader)
        {
            var t = GetRecordFactory<T>();
            return t.Read(reader);
        }

        [MethodImpl(DBUtils.Optimization)]
        internal static IEnumerable<T?> ReadEnumerable<T>(this DbDataReader reader, int estimateRow = 0, bool useBuffer = true)
        {
            var t = GetRecordFactory<T>();
            return useBuffer
                    ? t.ReadBuffed(reader, estimateRow)
                    : t.ReadUnBuffed(reader);
        }

        [MethodImpl(DBUtils.Optimization)]
        internal static IEnumerable<T?> ReadUnBuffedEnumerable<T>(this DbDataReader reader)
        {
            var t = GetRecordFactory<T>();
            return t.ReadUnBuffed(reader);
        }

        [MethodImpl(DBUtils.Optimization)]
        internal static IAsyncEnumerable<T?> ReadEnumerableAsync<T>(this DbDataReader reader, CancellationToken cancellationToken = default)
        {
            var t = GetRecordFactory<T>();
            return t.ReadUnBuffedAsync(reader, cancellationToken);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static IRecordFactory<T> GetRecordFactory<T>()
        {
            var t = RecordFactoryCache<T>.Cache;
            if (t == null)
            {
                var ty = typeof(T);
                if (ty.IsEnum)
                {
                    t = RecordFactoryCache<T>.Cache = (ScalarRecordFactory<T>?)Activator.CreateInstance(typeof(ScalarRecordFactoryEnum<>).MakeGenericType(ty));
                }
                else if (ty.IsGenericType && typeof(Nullable<>) == ty.GetGenericTypeDefinition() && Nullable.GetUnderlyingType(ty).IsEnum)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    t = RecordFactoryCache<T>.Cache = (ScalarRecordFactory<T>?)Activator.CreateInstance(typeof(ScalarRecordFactoryEnumNull<>).MakeGenericType(Nullable.GetUnderlyingType(ty)));
#pragma warning restore CS8604 // Possible null reference argument.
                }
                else
                {
                    t = (IRecordFactory<T>?)cacheFactory?.Invoke();
                    if (t == null)
                        ThrowHelper.ThrowNotSupportedException();
                }
            }

            return t!;
        }

        [MethodImpl(DBUtils.Optimization)]
        public static IParamsSetter<T> GetParamsSetter<T>()
        {
            var t = GetRecordFactory<T>() as IParamsSetter<T>;
            if (t == null)
                ThrowHelper.ThrowNotSupportedException();
            return t;
        }

        [MethodImpl(DBUtils.Optimization)]
        public static DbType GetDbType<T>()
        {
            return GetDbType(typeof(T));
        }

        [MethodImpl(DBUtils.Optimization)]
        public static DbType GetDbType(Type type)
        {
            return dbTypeMapping[type];
        }
    }
}