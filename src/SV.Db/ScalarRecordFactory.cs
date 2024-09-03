using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    public abstract class ScalarRecordFactory<T> : IRecordFactory<T>
    {
        protected abstract T? ReadScalar(DbDataReader reader);

        public virtual T? Read(DbDataReader reader)
        {
            if (reader.Read())
            {
                if (reader.GetFieldType(0) == typeof(T))
                {
                    return ReadScalar(reader);
                }
                else
                {
                    return DBUtils.As<T>(reader.GetValue(0));
                }
            }
            return default;
        }

        public virtual List<T?> ReadBuffed(DbDataReader reader, int estimateRow = 0)
        {
            List<T?> result = new(estimateRow);
            if (reader.Read())
            {
                if (reader.GetFieldType(0) == typeof(T))
                {
                    do
                    {
                        result.Add(ReadScalar(reader));
                    }
                    while (reader.Read());
                }
                else
                {
                    do
                    {
                        result.Add(reader.IsDBNull(0) ? default : DBUtils.As<T>(reader.GetValue(0)));
                    }
                    while (reader.Read());
                }
            }

            return result;
        }

        public virtual IEnumerable<T?> ReadUnBuffed(DbDataReader reader)
        {
            if (reader.Read())
            {
                if (reader.GetFieldType(0) == typeof(T))
                {
                    do
                    {
                        yield return ReadScalar(reader);
                    }
                    while (reader.Read());
                }
                else
                {
                    do
                    {
                        yield return reader.IsDBNull(0) ? default : DBUtils.As<T>(reader.GetValue(0));
                    }
                    while (reader.Read());
                }
            }
        }

        public virtual async IAsyncEnumerable<T?> ReadUnBuffedAsync(DbDataReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                if (reader.GetFieldType(0) == typeof(T))
                {
                    do
                    {
                        yield return ReadScalar(reader);
                    }
                    while (await reader.ReadAsync(cancellationToken));
                }
                else
                {
                    do
                    {
                        yield return reader.IsDBNull(0) ? default : DBUtils.As<T>(reader.GetValue(0));
                    }
                    while (await reader.ReadAsync(cancellationToken));
                }
            }
        }
    }

    public class ScalarRecordFactoryEnum<T> : ScalarRecordFactory<T> where T : struct, Enum
    {
        public override T Read(DbDataReader reader)
        {
            if (reader.Read())
            {
                return ReadScalar(reader);
            }
            return default;
        }

        protected override T ReadScalar(DbDataReader reader)
        {
            if (reader.IsDBNull(0)) return default;
            return Enums<T>.ToEnum(reader.GetValue(0));
        }

        public override List<T> ReadBuffed(DbDataReader reader, int estimateRow = 0)
        {
            List<T> result = new(estimateRow);
            if (reader.Read())
            {
                do
                {
                    result.Add(ReadScalar(reader));
                }
                while (reader.Read());
            }

            return result!;
        }

        public override IEnumerable<T> ReadUnBuffed(DbDataReader reader)
        {
            if (reader.Read())
            {
                do
                {
                    yield return ReadScalar(reader);
                }
                while (reader.Read());
            }
        }

        public override async IAsyncEnumerable<T> ReadUnBuffedAsync(DbDataReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                do
                {
                    yield return ReadScalar(reader);
                }
                while (await reader.ReadAsync(cancellationToken));
            }
        }
    }

    public class ScalarRecordFactoryEnumNull<T> : ScalarRecordFactory<T?> where T : struct, Enum
    {
        public override T? Read(DbDataReader reader)
        {
            if (reader.Read())
            {
                return ReadScalar(reader);
            }
            return default;
        }

        protected override T? ReadScalar(DbDataReader reader)
        {
            if (reader.IsDBNull(0)) return default;
            return Enums<T>.ToEnum(reader.GetValue(0));
        }

        public override List<T?> ReadBuffed(DbDataReader reader, int estimateRow = 0)
        {
            List<T?> result = new(estimateRow);
            if (reader.Read())
            {
                do
                {
                    result.Add(ReadScalar(reader));
                }
                while (reader.Read());
            }

            return result;
        }

        public override IEnumerable<T?> ReadUnBuffed(DbDataReader reader)
        {
            if (reader.Read())
            {
                do
                {
                    yield return ReadScalar(reader);
                }
                while (reader.Read());
            }
        }

        public override async IAsyncEnumerable<T?> ReadUnBuffedAsync(DbDataReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                do
                {
                    yield return ReadScalar(reader);
                }
                while (await reader.ReadAsync(cancellationToken));
            }
        }
    }

    public class ScalarRecordFactoryString : ScalarRecordFactory<string>
    {
        protected override string? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetString(0);
        }
    }

    public class ScalarRecordFactoryInt : ScalarRecordFactory<int>
    {
        protected override int ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt32(0);
        }
    }

    public class ScalarRecordFactoryIntNull : ScalarRecordFactory<int?>
    {
        protected override int? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt32(0);
        }
    }

    public class ScalarRecordFactoryBoolean : ScalarRecordFactory<bool>
    {
        protected override bool ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetBoolean(0);
        }
    }

    public class ScalarRecordFactoryBooleanNull : ScalarRecordFactory<bool?>
    {
        protected override bool? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetBoolean(0);
        }
    }

    public class ScalarRecordFactoryFloat : ScalarRecordFactory<float>
    {
        protected override float ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetFloat(0);
        }
    }

    public class ScalarRecordFactoryFloatNull : ScalarRecordFactory<float?>
    {
        protected override float? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetFloat(0);
        }
    }

    public class ScalarRecordFactoryDouble : ScalarRecordFactory<double>
    {
        protected override double ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDouble(0);
        }
    }

    public class ScalarRecordFactoryDoubleNull : ScalarRecordFactory<double?>
    {
        protected override double? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDouble(0);
        }
    }

    public class ScalarRecordFactoryDecimal : ScalarRecordFactory<decimal>
    {
        protected override decimal ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDecimal(0);
        }
    }

    public class ScalarRecordFactoryDecimalNull : ScalarRecordFactory<decimal?>
    {
        protected override decimal? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDecimal(0);
        }
    }

    public class ScalarRecordFactoryDateTime : ScalarRecordFactory<DateTime>
    {
        protected override DateTime ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDateTime(0);
        }
    }

    public class ScalarRecordFactoryDateTimeNull : ScalarRecordFactory<DateTime?>
    {
        protected override DateTime? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDateTime(0);
        }
    }

    public class ScalarRecordFactoryGuid : ScalarRecordFactory<Guid>
    {
        protected override Guid ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetGuid(0);
        }
    }

    public class ScalarRecordFactoryGuidNull : ScalarRecordFactory<Guid?>
    {
        protected override Guid? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetGuid(0);
        }
    }

    public class ScalarRecordFactoryLong : ScalarRecordFactory<long>
    {
        protected override long ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt64(0);
        }
    }

    public class ScalarRecordFactoryLongNull : ScalarRecordFactory<long?>
    {
        protected override long? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt64(0);
        }
    }

    public class ScalarRecordFactoryShort : ScalarRecordFactory<short>
    {
        protected override short ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt16(0);
        }
    }

    public class ScalarRecordFactoryShortNull : ScalarRecordFactory<short?>
    {
        protected override short? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt16(0);
        }
    }

    public class ScalarRecordFactoryByte : ScalarRecordFactory<byte>
    {
        protected override byte ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetByte(0);
        }
    }

    public class ScalarRecordFactoryByteNull : ScalarRecordFactory<byte?>
    {
        protected override byte? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetByte(0);
        }
    }

    public class ScalarRecordFactoryChar : ScalarRecordFactory<char>
    {
        protected override char ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetChar(0);
        }
    }

    public class ScalarRecordFactoryCharNull : ScalarRecordFactory<char?>
    {
        protected override char? ReadScalar(DbDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetChar(0);
        }
    }
}