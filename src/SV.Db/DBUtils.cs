using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    public static class DBUtils
    {
        internal const MethodImplOptions Optimization = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
        private static readonly object[] s_BoxedInt32 = [-1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        private static readonly object s_BoxedTrue = true, s_BoxedFalse = false;

        [MethodImpl(Optimization)]
        public static object AsDBValue(int value)
            => value >= -1 && value <= 10 ? s_BoxedInt32[value + 1] : value;

        [MethodImpl(Optimization)]
        public static object AsDBValue(int? value)
            => value.HasValue ? AsDBValue(value.GetValueOrDefault()) : DBNull.Value;

        [MethodImpl(Optimization)]
        public static object AsDBValue(bool value)
            => value ? s_BoxedTrue : s_BoxedFalse;

        [MethodImpl(Optimization)]
        public static object AsDBValue(bool? value)
            => value.HasValue ? (value.GetValueOrDefault() ? s_BoxedTrue : s_BoxedFalse) : DBNull.Value;

        [MethodImpl(Optimization)]
        public static object AsDBValue<T>(T? value) where T : struct
            => value.HasValue ? AsDBValue(value.GetValueOrDefault()) : DBNull.Value;

        [MethodImpl(Optimization)]
        public static object AsDBValue(object? value)
            => value ?? DBNull.Value;

        [MethodImpl(Optimization)]
        public static T As<T>(object? value)
        {
            if (value is null or DBNull)
            {
                if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                return default!;
            }
            else
            {
                if (value is T typed)
                {
                    return typed;
                }
                string? s;
                if (typeof(T) == typeof(int))
                {
                    int t = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<int, T>(ref t);
                }
                else if (typeof(T) == typeof(string))
                {
                    var t = Convert.ToString(value, CultureInfo.InvariantCulture)!;
                    return Unsafe.As<string, T>(ref t);
                }
                else if (typeof(T) == typeof(int?))
                {
                    int? t = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<int?, T>(ref t);
                }
                else if (typeof(T) == typeof(bool))
                {
                    bool t = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<bool, T>(ref t);
                }
                else if (typeof(T) == typeof(bool?))
                {
                    bool? t = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<bool?, T>(ref t);
                }
                else if (typeof(T) == typeof(float))
                {
                    float t = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<float, T>(ref t);
                }
                else if (typeof(T) == typeof(float?))
                {
                    float? t = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<float?, T>(ref t);
                }
                else if (typeof(T) == typeof(double))
                {
                    double t = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<double, T>(ref t);
                }
                else if (typeof(T) == typeof(double?))
                {
                    double? t = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<double?, T>(ref t);
                }
                else if (typeof(T) == typeof(decimal))
                {
                    decimal t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<decimal, T>(ref t);
                }
                else if (typeof(T) == typeof(decimal?))
                {
                    decimal? t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<decimal?, T>(ref t);
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    DateTime t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<DateTime, T>(ref t);
                }
                else if (typeof(T) == typeof(DateTime?))
                {
                    DateTime? t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<DateTime?, T>(ref t);
                }
                else if (typeof(T) == typeof(Guid) && (s = value as string) is not null)
                {
                    Guid t = Guid.Parse(s);
                    return Unsafe.As<Guid, T>(ref t);
                }
                else if (typeof(T) == typeof(Guid?) && (s = value as string) is not null)
                {
                    Guid? t = Guid.Parse(s);
                    return Unsafe.As<Guid?, T>(ref t);
                }
                else if (typeof(T) == typeof(long))
                {
                    long t = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<long, T>(ref t);
                }
                else if (typeof(T) == typeof(long?))
                {
                    long? t = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<long?, T>(ref t);
                }
                else if (typeof(T) == typeof(short))
                {
                    short t = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<short, T>(ref t);
                }
                else if (typeof(T) == typeof(short?))
                {
                    short? t = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<short?, T>(ref t);
                }
                else if (typeof(T) == typeof(sbyte))
                {
                    sbyte t = Convert.ToSByte(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<sbyte, T>(ref t);
                }
                else if (typeof(T) == typeof(sbyte?))
                {
                    sbyte? t = Convert.ToSByte(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<sbyte?, T>(ref t);
                }
                else if (typeof(T) == typeof(ulong))
                {
                    ulong t = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<ulong, T>(ref t);
                }
                else if (typeof(T) == typeof(ulong?))
                {
                    ulong? t = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<ulong?, T>(ref t);
                }
                else if (typeof(T) == typeof(uint))
                {
                    uint t = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<uint, T>(ref t);
                }
                else if (typeof(T) == typeof(uint?))
                {
                    uint? t = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<uint?, T>(ref t);
                }
                else if (typeof(T) == typeof(ushort))
                {
                    ushort t = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<ushort, T>(ref t);
                }
                else if (typeof(T) == typeof(ushort?))
                {
                    ushort? t = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<ushort?, T>(ref t);
                }
                else if (typeof(T) == typeof(byte))
                {
                    byte t = Convert.ToByte(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<byte, T>(ref t);
                }
                else if (typeof(T) == typeof(byte?))
                {
                    byte? t = Convert.ToByte(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<byte?, T>(ref t);
                }
                else if (typeof(T) == typeof(char))
                {
                    char t = Convert.ToChar(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<char, T>(ref t);
                }
                else if (typeof(T) == typeof(char?))
                {
                    char? t = Convert.ToChar(value, CultureInfo.InvariantCulture);
                    return Unsafe.As<char?, T>(ref t);
                }
                else
                {
                    return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
                }
            }
        }

        [MethodImpl(Optimization)]
        public static T GetFieldValue<T>(DbDataReader reader, int ordinal)
        {
            if (typeof(T) == typeof(int))
            {
                var t = reader.GetInt32(ordinal);
                return Unsafe.As<int, T>(ref t);
            }
            else if (typeof(T) == typeof(string))
            {
                var t = reader.GetString(ordinal);
                return Unsafe.As<string, T>(ref t);
            }
            else if (typeof(T) == typeof(int?))
            {
                int? t = reader.GetInt32(ordinal);
                return Unsafe.As<int?, T>(ref t);
            }
            else if (typeof(T) == typeof(bool))
            {
                bool t = reader.GetBoolean(ordinal);
                return Unsafe.As<bool, T>(ref t);
            }
            else if (typeof(T) == typeof(bool?))
            {
                bool? t = reader.GetBoolean(ordinal);
                return Unsafe.As<bool?, T>(ref t);
            }
            else if (typeof(T) == typeof(float))
            {
                float t = reader.GetFloat(ordinal);
                return Unsafe.As<float, T>(ref t);
            }
            else if (typeof(T) == typeof(float?))
            {
                float? t = reader.GetFloat(ordinal);
                return Unsafe.As<float?, T>(ref t);
            }
            else if (typeof(T) == typeof(double))
            {
                double t = reader.GetDouble(ordinal);
                return Unsafe.As<double, T>(ref t);
            }
            else if (typeof(T) == typeof(double?))
            {
                double? t = reader.GetDouble(ordinal);
                return Unsafe.As<double?, T>(ref t);
            }
            else if (typeof(T) == typeof(decimal))
            {
                decimal t = reader.GetDecimal(ordinal);
                return Unsafe.As<decimal, T>(ref t);
            }
            else if (typeof(T) == typeof(decimal?))
            {
                decimal? t = reader.GetDecimal(ordinal);
                return Unsafe.As<decimal?, T>(ref t);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime t = reader.GetDateTime(ordinal);
                return Unsafe.As<DateTime, T>(ref t);
            }
            else if (typeof(T) == typeof(DateTime?))
            {
                DateTime? t = reader.GetDateTime(ordinal);
                return Unsafe.As<DateTime?, T>(ref t);
            }
            else if (typeof(T) == typeof(Guid))
            {
                Guid t = reader.GetGuid(ordinal);
                return Unsafe.As<Guid, T>(ref t);
            }
            else if (typeof(T) == typeof(Guid?))
            {
                Guid? t = reader.GetGuid(ordinal);
                return Unsafe.As<Guid?, T>(ref t);
            }
            else if (typeof(T) == typeof(long))
            {
                long t = reader.GetInt64(ordinal);
                return Unsafe.As<long, T>(ref t);
            }
            else if (typeof(T) == typeof(long?))
            {
                long? t = reader.GetInt64(ordinal);
                return Unsafe.As<long?, T>(ref t);
            }
            else if (typeof(T) == typeof(short))
            {
                short t = reader.GetInt16(ordinal);
                return Unsafe.As<short, T>(ref t);
            }
            else if (typeof(T) == typeof(short?))
            {
                short? t = reader.GetInt16(ordinal);
                return Unsafe.As<short?, T>(ref t);
            }
            else if (typeof(T) == typeof(byte))
            {
                byte t = reader.GetByte(ordinal);
                return Unsafe.As<byte, T>(ref t);
            }
            else if (typeof(T) == typeof(byte?))
            {
                byte? t = reader.GetByte(ordinal);
                return Unsafe.As<byte?, T>(ref t);
            }
            else if (typeof(T) == typeof(char))
            {
                char t = reader.GetChar(ordinal);
                return Unsafe.As<char, T>(ref t);
            }
            else if (typeof(T) == typeof(char?))
            {
                char? t = reader.GetChar(ordinal);
                return Unsafe.As<char?, T>(ref t);
            }
            return reader.GetFieldValue<T>(ordinal);
        }
    }
}