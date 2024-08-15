using System.Data;
using System.Data.Common;
using System.Globalization;

namespace SV.Db
{
    public class ScalarFactory<T>
    {
        protected virtual T ReadScalar(IDataReader reader)
        {
            if (reader is DbDataReader db)
            {
                return DBUtils.GetFieldValue<T>(db, 0);
            }
            return reader.IsDBNull(0) ? default(T) : (T)reader.GetValue(0);
        }

        public virtual T Read(IDataReader reader)
        {
            if (reader.Read())
            {
                if (reader.GetFieldType(0) != typeof(T))
                {
                    return reader.IsDBNull(0) ? default(T) : (T)Convert.ChangeType(reader.GetValue(0), Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
                }
                return ReadScalar(reader);
            }
            return default;
        }

        public virtual List<T> ReadBuffed(IDataReader reader)
        {
            List<T> result = new();
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
                        result.Add(reader.IsDBNull(0) ? default(T) : (T)Convert.ChangeType(reader.GetValue(0), Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture));
                    }
                    while (reader.Read());
                }
            }

            return result;
        }

        public virtual IEnumerable<T> ReadUnBuffed(IDataReader reader)
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
                        yield return reader.IsDBNull(0) ? default(T) : (T)Convert.ChangeType(reader.GetValue(0), Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
                    }
                    while (reader.Read());
                }
            }
        }
    }

    public class ScalarFactoryEnum<T> : ScalarFactory<T>
    {
        public override T Read(IDataReader reader)
        {
            if (reader.Read())
            {
                return ReadScalar(reader);
            }
            return default;
        }

        protected override T ReadScalar(IDataReader reader)
        {
            if (reader.GetFieldType(0) == typeof(string))
            {
                return reader.IsDBNull(0) ? default(T) : (T)Enum.Parse(typeof(T), reader.GetString(0));
            }
            else
            {
                return reader.IsDBNull(0) ? default(T) : (T)Enum.ToObject(typeof(T), reader.GetValue(0));
            }
        }

        public override List<T> ReadBuffed(IDataReader reader)
        {
            List<T> result = new();
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

        public override IEnumerable<T> ReadUnBuffed(IDataReader reader)
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
    }

    public class ScalarFactoryEnumNull<T> : ScalarFactory<T?> where T : struct
    {
        public override T? Read(IDataReader reader)
        {
            if (reader.Read())
            {
                return ReadScalar(reader);
            }
            return default;
        }

        protected override T? ReadScalar(IDataReader reader)
        {
            if (reader.GetFieldType(0) == typeof(string))
            {
                return reader.IsDBNull(0) ? default(T) : (T)Enum.Parse(typeof(T), reader.GetString(0));
            }
            else
            {
                return reader.IsDBNull(0) ? default(T) : (T)Enum.ToObject(typeof(T), reader.GetValue(0));
            }
        }

        public override List<T?> ReadBuffed(IDataReader reader)
        {
            List<T?> result = new();
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

        public override IEnumerable<T?> ReadUnBuffed(IDataReader reader)
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
    }

    public class ScalarFactoryString : ScalarFactory<string>
    {
        protected override string ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetString(0);
        }
    }

    public class ScalarFactoryInt : ScalarFactory<int>
    {
        protected override int ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt32(0);
        }
    }

    public class ScalarFactoryIntNull : ScalarFactory<int?>
    {
        protected override int? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt32(0);
        }
    }

    public class ScalarFactoryBoolean : ScalarFactory<bool>
    {
        protected override bool ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetBoolean(0);
        }
    }

    public class ScalarFactoryBooleanNull : ScalarFactory<bool?>
    {
        protected override bool? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetBoolean(0);
        }
    }

    public class ScalarFactoryFloat : ScalarFactory<float>
    {
        protected override float ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetFloat(0);
        }
    }

    public class ScalarFactoryFloatNull : ScalarFactory<float?>
    {
        protected override float? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetFloat(0);
        }
    }

    public class ScalarFactoryDouble : ScalarFactory<double>
    {
        protected override double ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDouble(0);
        }
    }

    public class ScalarFactoryDoubleNull : ScalarFactory<double?>
    {
        protected override double? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDouble(0);
        }
    }

    public class ScalarFactoryDecimal : ScalarFactory<decimal>
    {
        protected override decimal ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDecimal(0);
        }
    }

    public class ScalarFactoryDecimalNull : ScalarFactory<decimal?>
    {
        protected override decimal? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDecimal(0);
        }
    }

    public class ScalarFactoryDateTime : ScalarFactory<DateTime>
    {
        protected override DateTime ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDateTime(0);
        }
    }

    public class ScalarFactoryDateTimeNull : ScalarFactory<DateTime?>
    {
        protected override DateTime? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDateTime(0);
        }
    }

    public class ScalarFactoryGuid : ScalarFactory<Guid>
    {
        protected override Guid ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetGuid(0);
        }
    }

    public class ScalarFactoryGuidNull : ScalarFactory<Guid?>
    {
        protected override Guid? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetGuid(0);
        }
    }

    public class ScalarFactoryLong : ScalarFactory<long>
    {
        protected override long ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt64(0);
        }
    }

    public class ScalarFactoryLongNull : ScalarFactory<long?>
    {
        protected override long? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt64(0);
        }
    }

    public class ScalarFactoryShort : ScalarFactory<short>
    {
        protected override short ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt16(0);
        }
    }

    public class ScalarFactoryShortNull : ScalarFactory<short?>
    {
        protected override short? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt16(0);
        }
    }

    public class ScalarFactoryByte : ScalarFactory<byte>
    {
        protected override byte ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetByte(0);
        }
    }

    public class ScalarFactoryByteNull : ScalarFactory<byte?>
    {
        protected override byte? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetByte(0);
        }
    }

    public class ScalarFactoryChar : ScalarFactory<char>
    {
        protected override char ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetChar(0);
        }
    }

    public class ScalarFactoryCharNull : ScalarFactory<char?>
    {
        protected override char? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetChar(0);
        }
    }
}