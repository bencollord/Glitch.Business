using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Glitch.Business
{
    [Serializable]
    public sealed class Currency : IEquatable<Currency>, IFormatProvider, ICustomFormatter
    {
        private const string UnknownCurrencySymbol = "¤";

        private static readonly Lazy<HashSet<Currency>> lazyCache = new Lazy<HashSet<Currency>>(LoadCurrencies);

        private string name;
        private string symbol;
        private string isoCode;
        private int isoNumber;
        private int minorUnits;

        private Currency(string isoCode, int isoNumber, string name, string symbol, int minorUnits)
        {
            Guard.NotNullOrEmpty(isoCode, nameof(isoCode));
            Guard.NotLessThanZero(isoNumber, nameof(isoNumber));

            this.name = String.IsNullOrEmpty(name) ? isoCode : name;
            this.symbol = String.IsNullOrEmpty(symbol) ? UnknownCurrencySymbol : symbol;
            this.isoCode = isoCode;
            this.isoNumber = isoNumber;
            this.minorUnits = minorUnits;
        }

        public static readonly Currency None = new Currency("XXX", 999, "No Currency", UnknownCurrencySymbol, 0);

        public static Currency Current => GetInstance(CultureInfo.CurrentCulture);

        private static HashSet<Currency> Cache => lazyCache.Value;

        public static Currency GetInstance(int isoNumber)
        {
            return Cache.Where(i => i.IsoNumber == isoNumber).DefaultIfEmpty(None).Single();
        }

        public static Currency GetInstance(string isoCode)
        {
            return Cache.Where(i => i.IsoCode == isoCode).DefaultIfEmpty(None).Single();
        }

        public static Currency GetInstance(RegionInfo region)
        {
            Guard.NotNull(region, nameof(region));

            var currency = Cache.SingleOrDefault(c => StringComparer.InvariantCultureIgnoreCase.Equals(c.IsoCode, region.ISOCurrencySymbol));

            return currency ?? None;
        }

        public static Currency GetInstance(CultureInfo culture)
        {
            Guard.NotNull(culture, nameof(culture));
            Guard.Require(!culture.IsNeutralCulture, "Cannot get currency from neutral culture"); // TODO move to Errors.resx

            if (culture.Equals(CultureInfo.InvariantCulture))
            {
                return None;
            }

            return GetInstance(new RegionInfo(culture.LCID));
        }

        private static HashSet<Currency> LoadCurrencies()
        {
            var xml = XDocument.Load(@"C:\Users\bcollord\source\repos\Glitch\Glitch\Business\Currencies.xml");

            return xml.Descendants("Currency")
                .Select(x => new
                {
                    IsoCode = x.Element("IsoCode").Value,
                    IsoNumber = x.Element("IsoNumber").Value.Parse<int>(),
                    Name = x.Element("Name").Value,
                    Symbol = x.Element("Symbol")?.Value ?? UnknownCurrencySymbol,
                    MinorUnits = Int32.TryParse(x.Element("MinorUnits")?.Value, out int i) ? i : 0
                })
                .Select(x => new Currency(x.IsoCode, x.IsoNumber, x.Name, x.Symbol, x.MinorUnits))
                .ToHashSet();
        }

        public string Name => name;

        public string Symbol => symbol;

        public string IsoCode => isoCode;

        public int IsoNumber => isoNumber;

        public int MinorUnits => minorUnits;

        public bool Equals(Currency other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;

            return StringComparer.OrdinalIgnoreCase.Equals(IsoCode, other.IsoCode)
                && IsoNumber == other.IsoNumber;
        }

        public override bool Equals(object obj) => Equals(obj as Currency);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(isoNumber);
            hash.Add(isoCode, StringComparer.OrdinalIgnoreCase);

            return hash.ToHashCode();
        }

        public override string ToString() => $"{Name} ({IsoCode})";

        public string Format(string format, Money money, IFormatProvider provider)
        {
            var customFormatSpecifiers = new char[] { '0', '#', '.', ',' };
            var allowedFormatSpecifiers = new char[] { 'C', 'F', 'G', 'N' };

            var numberFormat = provider?.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo;

            if (numberFormat == null)
            {
                var culture = provider?.GetFormat(typeof(CultureInfo)) as CultureInfo ?? CultureInfo.CurrentCulture;

                numberFormat = culture.NumberFormat;
            }

            decimal value = money.ToDecimal();

            if (format.All(c => customFormatSpecifiers.Contains(c)))
            {
                var formattedValue = value.ToString(format, numberFormat);

                return AppendSymbol(formattedValue, numberFormat);
            }

            int precision = 2;

            if (format.Length > 1 && Int32.TryParse(format.Substring(1), out int i))
            {
                precision = i;
            }

            switch (format[0].ToUpperInvariant())
            {
                case 'L':
                    return String.Format($"{{0:N{precision}}} {{1}}", value, IsoCode);
                case 'I':
                    return String.Format($"{{0:F{precision}}} {{1}}", value, IsoCode);
                case 'C':
                case 'F':
                case 'G':
                case 'N':
                    return value.ToString(format, GetNumberFormat());
                default:
                    throw new FormatException(Errors.Format_Money);
            }
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(CultureInfo))
            {
                var culture = CultureInfo.CurrentCulture.Clone() as CultureInfo;
                var numberFormat = culture.NumberFormat.Clone() as NumberFormatInfo;

                numberFormat.CurrencyDecimalDigits = minorUnits;
                numberFormat.CurrencySymbol = Symbol;
                culture.NumberFormat = numberFormat;

                return culture;
            }
            if (formatType == typeof(NumberFormatInfo))
            {
                return GetNumberFormat();
            }
            if (formatType == typeof(Currency))
            {
                return this;
            }

            return null;
        }

        private NumberFormatInfo GetNumberFormat()
        {
            var numberFormat = CultureInfo.CurrentCulture.NumberFormat.Clone() as NumberFormatInfo;

            numberFormat.CurrencySymbol = Symbol;
            numberFormat.CurrencyDecimalDigits = minorUnits;

            return numberFormat;
        }

        private string AppendSymbol(string formatted, NumberFormatInfo numberFormat)
        {
            switch (numberFormat.CurrencyPositivePattern)
            {
                case 0: return $"{Symbol}{formatted}";
                case 1: return $"{formatted}{Symbol}";
                case 2: return $"{Symbol} {formatted}";
                case 3: return $"{formatted} {Symbol}";
                default:
                    Debug.Assert(false, "If we made it here, the .NET Framework is broken.");
                    throw new ArgumentOutOfRangeException();
            }
        }

        string ICustomFormatter.Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is Money m)
            {
                return Format(format, m, formatProvider);
            }
            if (arg is IFormattable f)
            {
                return f.ToString(format, CultureInfo.CurrentCulture);
            }

            return arg.ToString();
        }
    }
}
