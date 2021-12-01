using System;
using System.Globalization;

namespace Glitch.Business
{
    [Serializable]
    public struct Money : IEquatable<Money>, 
        IComparable<Money>, IEquatable<decimal>, 
        IComparable<decimal>, IComparable, 
        IFormattable, IConvertible
    {
        private decimal value;
        private int currencyCode;

        public Money(decimal value)
            : this(value, Currency.Current) { }

        public Money(decimal value, Currency currency)
            : this(value, currency.IsoNumber) { }

        public Money(decimal value, string currencyCode)
            : this(value, Currency.GetInstance(currencyCode)) { }

        public Money(decimal value, int currencyCode)
        {
            this.value = value;
            this.currencyCode = currencyCode;
        }

        public static Money Zero => new Money(0, Currency.Current);

        public static Money Parse(string input) => Parse(input, Currency.Current);

        public static Money Parse(string input, IFormatProvider formatProvider)
            => Parse(input, NumberStyles.Currency, formatProvider);
        
        public static Money Parse(string input, NumberStyles style)
            => Parse(input, style, Currency.Current);

        public static Money Parse(string input, NumberStyles style, IFormatProvider formatProvider)
        {
            if (TryParse(input, style, formatProvider, out Money result))
            {
                return result;
            }

            throw new FormatException(Errors.Format_Money);
        }

        public static bool TryParse(string input, out Money result)
            => TryParse(input, NumberStyles.Currency, Currency.Current, out result);

        public static bool TryParse(string input, NumberStyles styles, IFormatProvider formatProvider, out Money result)
        {
            bool success = Decimal.TryParse(input, styles, formatProvider, out decimal value);

            if (!success)
            {
                result = default;
                return false;
            }

            var culture = formatProvider.GetFormat(typeof(CultureInfo)) as CultureInfo;
            var currency = Currency.GetInstance(culture ?? CultureInfo.CurrentCulture);

            result = new Money(value, currency);
            return true;
        }

        public Money Add(Money other)
        {
            Guard.Require(currencyCode == other.currencyCode, Errors.InvalidCurrency);

            return new Money(value + other.value, currencyCode);
        }

        public Money Add(decimal value) => new Money(this.value + value, currencyCode);

        public Money Subtract(Money other)
        {
            Guard.Require(currencyCode == other.currencyCode, Errors.InvalidCurrency);

            return new Money(value - other.value, currencyCode);
        }

        public Money Subtract(decimal value) => new Money(this.value - value, currencyCode);

        public Money Multiply(Money other)
        {
            Guard.Require(currencyCode == other.currencyCode, Errors.InvalidCurrency);

            return new Money(value * other.value, currencyCode);
        }

        public Money Multiply(decimal value) => new Money(this.value * value, currencyCode);

        public Money Divide(Money other)
        {
            Guard.Require(currencyCode == other.currencyCode, Errors.InvalidCurrency);

            return new Money(value / other.value, currencyCode);
        }

        public Money Divide(decimal value) => new Money(this.value / value, currencyCode);

        public Money Remainder(Money other)
        {
            Guard.Require(currencyCode == other.currencyCode, Errors.InvalidCurrency);

            return new Money(value % other.value, currencyCode);
        }

        public Money Remainder(decimal value) => new Money(this.value % value, currencyCode);

        public Money Round()
            => new Money(Decimal.Round(value), currencyCode);

        public Money Round(int places)
            => new Money(Decimal.Round(value, places), currencyCode);

        public Money Round(MidpointRounding rounding)
            => new Money(Decimal.Round(value, rounding), currencyCode);

        public Money Round(int places, MidpointRounding rounding) 
            => new Money(Decimal.Round(value, places, rounding), currencyCode);

        public Money Floor() => new Money(Decimal.Floor(value), currencyCode);

        public Money Ceiling() => new Money(Decimal.Ceiling(value), currencyCode);

        public Money Negate() => new Money(Decimal.Negate(value), currencyCode);

        public Money Abs() => new Money(Math.Abs(value), currencyCode);

        public Money ChangeCurrency(Currency currency, decimal exchangeRate)
            => new Money(value * exchangeRate, currency);

        public Currency GetCurrency() => Currency.GetInstance(currencyCode);

        public int CompareTo(Money other)
        {
            if (currencyCode != other.currencyCode)
            {
                return currencyCode.CompareTo(other.currencyCode);
            }

            return value.CompareTo(other.value);
        }

        public int CompareTo(decimal value) => this.value.CompareTo(value);

        public int CompareTo(object obj)
        {
            switch (obj)
            {
                case Money m:
                    return CompareTo(m);
                case decimal d:
                    return CompareTo(d);
                default:
                    throw new ArgumentException(Errors.Argument_MustBeMoney);
            }
        }

        public bool Equals(Money other)
        {
            return value == other.value
                && currencyCode == other.currencyCode;
        }

        public bool Equals(decimal value) => this.value.Equals(value);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;

            if (obj is Money)
            {
                return Equals((Money)obj);
;           }
            if (obj is decimal)
            {
                return Equals((decimal)obj);
            }

            return false;
        }

        public override int GetHashCode() => HashCode.Combine(value, currencyCode);

        public override string ToString() => ToString("C");

        public string ToString(string format) => ToString(format, null);

        public string ToString(IFormatProvider formatProvider) => ToString("C", formatProvider);

        public string ToString(string format, IFormatProvider formatProvider)
        {
            format = format.ToUpperInvariant();

            if (format.StartsWith("G"))
            {
                format = format.Replace("G", "C");
            }
            if (formatProvider == null)
            {
                formatProvider = GetCurrency();
            }

            var formatter = formatProvider.GetFormat(typeof(ICustomFormatter));

            if (formatter is ICustomFormatter customFormatter)
            {
                return customFormatter.Format(format, this, formatProvider);
            }

            formatter = formatProvider.GetFormat(typeof(Currency));

            if (formatter is Currency c)
            {
                return c.Format(format, this, formatProvider);
            }

            formatter = formatProvider.GetFormat(typeof(NumberFormatInfo));

            if (formatter is NumberFormatInfo numberFormat)
            {
                return value.ToString(format, numberFormat);
            }

            return value.ToString(format, NumberFormatInfo.CurrentInfo);
        }

        public TypeCode GetTypeCode() => TypeCode.Decimal;

        public int ToInt32() => (int)value;

        public long ToInt64() => (long)value;

        public decimal ToDecimal() => value;

        public static bool operator ==(Money x, Money y) => x.Equals(y);
        public static bool operator !=(Money x, Money y) => !x.Equals(y);
        public static bool operator >(Money x, Money y) => x.CompareTo(y) == 1;
        public static bool operator <(Money x, Money y) => x.CompareTo(y) == -1;
        public static bool operator >=(Money x, Money y) => x.CompareTo(y) >= 0;
        public static bool operator <=(Money x, Money y) => x.CompareTo(y) <= 0;

        public static Money operator +(Money x, Money y) => x.Add(y);
        public static Money operator -(Money x, Money y) => x.Subtract(y);
        public static Money operator *(Money x, Money y) => x.Multiply(y);
        public static Money operator /(Money x, Money y) => x.Divide(y);
        public static Money operator %(Money x, Money y) => x.Remainder(y);

        public static Money operator -(Money x) => x.Negate();
        public static Money operator ++(Money x) => new Money(++x.value, x.currencyCode);
        public static Money operator --(Money x) => new Money(--x.value, x.currencyCode);

        public static explicit operator Money(decimal x) => new Money(x);
        public static implicit operator decimal(Money x) => x.value;

        public static explicit operator int(Money x) => (int)x.value;
        public static explicit operator short(Money x) => (short)x.value;
        public static explicit operator long(Money x) => (long)x.value;
        public static explicit operator uint(Money x) => (uint)x.value;
        public static explicit operator ushort(Money x) => (ushort)x.value;
        public static explicit operator ulong(Money x) => (ulong)x.value;
        public static explicit operator byte(Money x) => (byte)x.value;
        public static explicit operator sbyte(Money x) => (sbyte)x.value;
        public static explicit operator float(Money x) => (float)x.value;
        public static explicit operator double(Money x) => (double)x.value;

        public static explicit operator Money(int x) => new Money(x);
        public static explicit operator Money(short x) => new Money(x);
        public static explicit operator Money(long x) => new Money(x);
        public static explicit operator Money(uint x) => new Money(x);
        public static explicit operator Money(ushort x) => new Money(x);
        public static explicit operator Money(ulong x) => new Money(x);
        public static explicit operator Money(byte x) => new Money(x);
        public static explicit operator Money(sbyte x) => new Money(x);
        public static explicit operator Money(float x) => new Money((decimal)x);
        public static explicit operator Money(double x) => new Money((decimal)x);

        public static Money operator +(Money x, decimal y) => x.Add(y);
        public static Money operator -(Money x, decimal y) => x.Subtract(y);
        public static Money operator *(Money x, decimal y) => x.Multiply(y);
        public static Money operator /(Money x, decimal y) => x.Divide(y);
        public static Money operator %(Money x, decimal y) => x.Remainder(y);

        public static bool operator ==(Money x, decimal y) => x.Equals(y);
        public static bool operator !=(Money x, decimal y) => !x.Equals(y);
        public static bool operator >(Money x, decimal y) => x.CompareTo(y) == 1;
        public static bool operator <(Money x, decimal y) => x.CompareTo(y) == -1;
        public static bool operator >=(Money x, decimal y) => x.CompareTo(y) >= 0;
        public static bool operator <=(Money x, decimal y) => x.CompareTo(y) <= 0;

        bool IConvertible.ToBoolean(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToBoolean(provider);

        char IConvertible.ToChar(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToChar(provider);

        sbyte IConvertible.ToSByte(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToSByte(provider);

        byte IConvertible.ToByte(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToByte(provider);

        short IConvertible.ToInt16(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToInt16(provider);

        ushort IConvertible.ToUInt16(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToUInt16(provider);

        int IConvertible.ToInt32(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToInt32(provider);

        uint IConvertible.ToUInt32(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToUInt32(provider);

        long IConvertible.ToInt64(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToInt64(provider);

        ulong IConvertible.ToUInt64(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToUInt64(provider);

        float IConvertible.ToSingle(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToSingle(provider);

        double IConvertible.ToDouble(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToDouble(provider);

        decimal IConvertible.ToDecimal(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToDecimal(provider);

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
            => value.CastAs<IConvertible>().ToDateTime(provider);

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
            => value.CastAs<IConvertible>().ToType(conversionType, provider);
    }
}
