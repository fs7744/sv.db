using System.Data;

namespace SV.Db
{
    public abstract class ScalarRecordFactory<T> : IRecordFactory<T>
    {
        protected abstract T ReadScalar(IDataReader reader);

        public virtual T Read(IDataReader reader)
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
            return default(T);
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
                        result.Add(reader.IsDBNull(0) ? default(T) : DBUtils.As<T>(reader.GetValue(0)));
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
                        yield return reader.IsDBNull(0) ? default(T) : DBUtils.As<T>(reader.GetValue(0));
                    }
                    while (reader.Read());
                }
            }
        }
    }

    public class ScalarRecordFactoryEnum<T> : ScalarRecordFactory<T>
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

    public class ScalarRecordFactoryEnumNull<T> : ScalarRecordFactory<T?> where T : struct
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

    public class ScalarRecordFactoryString : ScalarRecordFactory<string>
    {
        protected override string ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetString(0);
        }
    }

    public class ScalarRecordFactoryInt : ScalarRecordFactory<int>
    {
        protected override int ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt32(0);
        }
    }

    public class ScalarRecordFactoryIntNull : ScalarRecordFactory<int?>
    {
        protected override int? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt32(0);
        }
    }

    public class ScalarRecordFactoryBoolean : ScalarRecordFactory<bool>
    {
        protected override bool ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetBoolean(0);
        }
    }

    public class ScalarRecordFactoryBooleanNull : ScalarRecordFactory<bool?>
    {
        protected override bool? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetBoolean(0);
        }
    }

    public class ScalarRecordFactoryFloat : ScalarRecordFactory<float>
    {
        protected override float ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetFloat(0);
        }
    }

    public class ScalarRecordFactoryFloatNull : ScalarRecordFactory<float?>
    {
        protected override float? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetFloat(0);
        }
    }

    public class ScalarRecordFactoryDouble : ScalarRecordFactory<double>
    {
        protected override double ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDouble(0);
        }
    }

    public class ScalarRecordFactoryDoubleNull : ScalarRecordFactory<double?>
    {
        protected override double? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDouble(0);
        }
    }

    public class ScalarRecordFactoryDecimal : ScalarRecordFactory<decimal>
    {
        protected override decimal ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDecimal(0);
        }
    }

    public class ScalarRecordFactoryDecimalNull : ScalarRecordFactory<decimal?>
    {
        protected override decimal? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDecimal(0);
        }
    }

    public class ScalarRecordFactoryDateTime : ScalarRecordFactory<DateTime>
    {
        protected override DateTime ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetDateTime(0);
        }
    }

    public class ScalarRecordFactoryDateTimeNull : ScalarRecordFactory<DateTime?>
    {
        protected override DateTime? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetDateTime(0);
        }
    }

    public class ScalarRecordFactoryGuid : ScalarRecordFactory<Guid>
    {
        protected override Guid ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetGuid(0);
        }
    }

    public class ScalarRecordFactoryGuidNull : ScalarRecordFactory<Guid?>
    {
        protected override Guid? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetGuid(0);
        }
    }

    public class ScalarRecordFactoryLong : ScalarRecordFactory<long>
    {
        protected override long ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt64(0);
        }
    }

    public class ScalarRecordFactoryLongNull : ScalarRecordFactory<long?>
    {
        protected override long? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt64(0);
        }
    }

    public class ScalarRecordFactoryShort : ScalarRecordFactory<short>
    {
        protected override short ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetInt16(0);
        }
    }

    public class ScalarRecordFactoryShortNull : ScalarRecordFactory<short?>
    {
        protected override short? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetInt16(0);
        }
    }

    public class ScalarRecordFactoryByte : ScalarRecordFactory<byte>
    {
        protected override byte ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetByte(0);
        }
    }

    public class ScalarRecordFactoryByteNull : ScalarRecordFactory<byte?>
    {
        protected override byte? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetByte(0);
        }
    }

    public class ScalarRecordFactoryChar : ScalarRecordFactory<char>
    {
        protected override char ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? default : reader.GetChar(0);
        }
    }

    public class ScalarRecordFactoryCharNull : ScalarRecordFactory<char?>
    {
        protected override char? ReadScalar(IDataReader reader)
        {
            return reader.IsDBNull(0) ? null : reader.GetChar(0);
        }
    }
}