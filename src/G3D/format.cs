/**
 @file format.h
 
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @author  2000-09-09
 @edited  2005-11-03

 Copyright 2000-2005, Morgan McGuire.
 All rights reserved.
 */

using System;
using global::System.Globalization;
using global::System.Text;

namespace G3D
{
    public static class Format
    {
        public static string format(string fmt, params object[] args)
        {
            return vformat(fmt, args);
        }

        public static string vformat(string fmt, params object[] argPtr)
        {
            if (fmt == null)
            {
                throw new ArgumentNullException("fmt");
            }

            if (argPtr == null)
            {
                argPtr = new object[0];
            }

            StringBuilder result = new StringBuilder(fmt.Length + 16);
            int argIndex = 0;
            int i = 0;

            while (i < fmt.Length)
            {
                char ch = fmt[i];
                if (ch != '%')
                {
                    result.Append(ch);
                    i++;
                    continue;
                }

                i++;
                if (i >= fmt.Length)
                {
                    throw new FormatException("Incomplete format specifier.");
                }

                if (fmt[i] == '%')
                {
                    result.Append('%');
                    i++;
                    continue;
                }

                bool leftAlign = false;
                bool plusSign = false;
                bool spaceSign = false;
                bool alternate = false;
                bool zeroPad = false;

                while (i < fmt.Length)
                {
                    char flag = fmt[i];
                    if (flag == '-')
                    {
                        leftAlign = true;
                    }
                    else if (flag == '+')
                    {
                        plusSign = true;
                    }
                    else if (flag == ' ')
                    {
                        spaceSign = true;
                    }
                    else if (flag == '#')
                    {
                        alternate = true;
                    }
                    else if (flag == '0')
                    {
                        zeroPad = true;
                    }
                    else
                    {
                        break;
                    }
                    i++;
                }

                int width = -1;
                if (i < fmt.Length && fmt[i] == '*')
                {
                    width = Convert.ToInt32(nextArgument(argPtr, ref argIndex), CultureInfo.InvariantCulture);
                    if (width < 0)
                    {
                        leftAlign = true;
                        width = -width;
                    }
                    i++;
                }
                else
                {
                    while (i < fmt.Length && fmt[i] >= '0' && fmt[i] <= '9')
                    {
                        if (width < 0)
                        {
                            width = 0;
                        }
                        width = width * 10 + (fmt[i] - '0');
                        i++;
                    }
                }

                int precision = -1;
                if (i < fmt.Length && fmt[i] == '.')
                {
                    i++;
                    precision = 0;
                    if (i < fmt.Length && fmt[i] == '*')
                    {
                        precision = Convert.ToInt32(nextArgument(argPtr, ref argIndex), CultureInfo.InvariantCulture);
                        if (precision < 0)
                        {
                            precision = -1;
                        }
                        i++;
                    }
                    else
                    {
                        while (i < fmt.Length && fmt[i] >= '0' && fmt[i] <= '9')
                        {
                            precision = precision * 10 + (fmt[i] - '0');
                            i++;
                        }
                    }
                }

                while (i < fmt.Length)
                {
                    char length = fmt[i];
                    if (length == 'h' || length == 'l' || length == 'L' || length == 'j' || length == 'z' || length == 't')
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (i >= fmt.Length)
                {
                    throw new FormatException("Incomplete format specifier.");
                }

                char conversion = fmt[i];
                i++;

                if (conversion == '%')
                {
                    result.Append('%');
                    continue;
                }

                if (conversion == 'n')
                {
                    throw new NotSupportedException("The %n conversion is not supported in safe managed code.");
                }

                object value = nextArgument(argPtr, ref argIndex);

                if (conversion == 'd' || conversion == 'i' || conversion == 'u' || conversion == 'o' || conversion == 'x' || conversion == 'X')
                {
                    int zeroInsertIndex;
                    string text = formatInteger(value, conversion, plusSign, spaceSign, alternate, precision, out zeroInsertIndex);
                    bool useZero = zeroPad && !leftAlign && precision < 0;
                    appendPadded(result, text, width, leftAlign, useZero, zeroInsertIndex);
                }
                else if (conversion == 'c')
                {
                    string text = formatCharacter(value).ToString();
                    appendPadded(result, text, width, leftAlign, false, 0);
                }
                else if (conversion == 's')
                {
                    string text = formatString(value);
                    if (precision >= 0 && text.Length > precision)
                    {
                        text = text.Substring(0, precision);
                    }
                    appendPadded(result, text, width, leftAlign, false, 0);
                }
                else if (conversion == 'p')
                {
                    string text = formatPointer(value);
                    appendPadded(result, text, width, leftAlign, false, 0);
                }
                else if (conversion == 'f' || conversion == 'F' || conversion == 'e' || conversion == 'E' || conversion == 'g' || conversion == 'G')
                {
                    int zeroInsertIndex;
                    string text = formatFloat(value, conversion, plusSign, spaceSign, alternate, precision, out zeroInsertIndex);
                    bool useZero = zeroPad && !leftAlign;
                    appendPadded(result, text, width, leftAlign, useZero, zeroInsertIndex);
                }
                else if (conversion == 'a' || conversion == 'A')
                {
                    throw new NotSupportedException("The hexadecimal floating-point conversion is not supported.");
                }
                else
                {
                    throw new FormatException("Unsupported format conversion: %" + conversion);
                }
            }

            return result.ToString();
        }

        private static object nextArgument(object[] args, ref int argIndex)
        {
            if (argIndex >= args.Length)
            {
                throw new FormatException("Not enough arguments for format string.");
            }

            object value = args[argIndex];
            argIndex++;
            return value;
        }

        private static string formatInteger(object value, char conversion, bool plusSign, bool spaceSign, bool alternate, int precision, out int zeroInsertIndex)
        {
            bool signed = conversion == 'd' || conversion == 'i';
            ulong magnitude;
            string sign = string.Empty;

            if (signed)
            {
                long signedValue = toInt64(value);
                if (signedValue < 0)
                {
                    sign = "-";
                    magnitude = (ulong)(-(signedValue + 1)) + 1;
                }
                else
                {
                    magnitude = (ulong)signedValue;
                    if (plusSign)
                    {
                        sign = "+";
                    }
                    else if (spaceSign)
                    {
                        sign = " ";
                    }
                }
            }
            else
            {
                magnitude = toUInt64(value);
            }

            int radix;
            bool upper;
            if (conversion == 'o')
            {
                radix = 8;
                upper = false;
            }
            else if (conversion == 'x' || conversion == 'X')
            {
                radix = 16;
                upper = conversion == 'X';
            }
            else
            {
                radix = 10;
                upper = false;
            }

            string digits;
            if (precision == 0 && magnitude == 0)
            {
                digits = string.Empty;
            }
            else
            {
                digits = convertBase(magnitude, radix, upper);
            }

            if (precision > digits.Length)
            {
                digits = new string('0', precision - digits.Length) + digits;
            }

            string prefix = string.Empty;
            if (alternate)
            {
                if (conversion == 'o')
                {
                    if (digits.Length == 0 || digits[0] != '0')
                    {
                        prefix = "0";
                    }
                }
                else if (conversion == 'x')
                {
                    prefix = "0x";
                }
                else if (conversion == 'X')
                {
                    prefix = "0X";
                }
            }

            zeroInsertIndex = sign.Length + prefix.Length;
            return sign + prefix + digits;
        }

        private static string convertBase(ulong value, int radix, bool upper)
        {
            if (value == 0)
            {
                return "0";
            }

            string alphabet = upper ? "0123456789ABCDEF" : "0123456789abcdef";
            char[] buffer = new char[64];
            int position = buffer.Length;

            while (value != 0)
            {
                position--;
                buffer[position] = alphabet[(int)(value % (ulong)radix)];
                value /= (ulong)radix;
            }

            return new string(buffer, position, buffer.Length - position);
        }

        private static char formatCharacter(object value)
        {
            if (value is char)
            {
                return (char)value;
            }

            if (value is string)
            {
                string text = (string)value;
                if (text.Length == 0)
                {
                    throw new FormatException("%c requires a character.");
                }
                return text[0];
            }

            return (char)(byte)toInt64(value);
        }

        private static string formatString(object value)
        {
            if (value == null)
            {
                return "(null)";
            }

            string text = value as string;
            if (text != null)
            {
                return text;
            }

            if (value is char)
            {
                return ((char)value).ToString();
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string formatPointer(object value)
        {
            long address = toInt64(value);
            string digits = ((ulong)address).ToString("X", CultureInfo.InvariantCulture);
            int minimum = Environment.Is64BitProcess ? 16 : 8;
            if (digits.Length < minimum)
            {
                digits = new string('0', minimum - digits.Length) + digits;
            }
            return "0x" + digits;
        }

        private static string formatFloat(object value, char conversion, bool plusSign, bool spaceSign, bool alternate, int precision, out int zeroInsertIndex)
        {
            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            bool upper = conversion == 'F' || conversion == 'E' || conversion == 'G';
            bool negative = number < 0.0 || (number == 0.0 && double.IsNegativeInfinity(1.0 / number));
            double magnitude = Math.Abs(number);

            string sign = negative ? "-" : (plusSign ? "+" : (spaceSign ? " " : string.Empty));
            zeroInsertIndex = sign.Length;

            if (double.IsNaN(number))
            {
                return sign + (upper ? "NAN" : "nan");
            }

            if (double.IsInfinity(number))
            {
                return sign + (upper ? "INF" : "inf");
            }

            if (precision < 0)
            {
                precision = 6;
            }

            char effective = conversion;
            if (effective == 'F')
            {
                effective = 'f';
            }
            else if (effective == 'E')
            {
                effective = 'e';
            }
            else if (effective == 'G')
            {
                effective = 'g';
            }

            string body;
            if (effective == 'f')
            {
                body = magnitude.ToString("F" + precision.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                if (alternate && body.IndexOf('.') < 0)
                {
                    body += ".";
                }
            }
            else if (effective == 'e')
            {
                string zeros = precision > 0 ? new string('0', precision) : string.Empty;
                string pattern = "0." + zeros + (upper ? "E+00" : "e+00");
                body = magnitude.ToString(pattern, CultureInfo.InvariantCulture);
                if (alternate && precision == 0)
                {
                    int exponentIndex = body.IndexOfAny(new char[] { 'e', 'E' });
                    if (exponentIndex >= 0)
                    {
                        body = body.Substring(0, exponentIndex) + "." + body.Substring(exponentIndex);
                    }
                }
            }
            else
            {
                if (precision == 0)
                {
                    precision = 1;
                }

                if (alternate)
                {
                    body = formatGeneralAlternate(magnitude, precision);
                }
                else
                {
                    body = magnitude.ToString("G" + precision.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                }

                body = normalizeExponent(body, upper);
            }

            return sign + body;
        }

        private static string formatGeneralAlternate(double value, int precision)
        {
            if (value == 0.0)
            {
                return value.ToString("F" + (precision - 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            }

            int exponent = (int)Math.Floor(Math.Log10(value));
            if (exponent < -4 || exponent >= precision)
            {
                int decimals = precision - 1;
                string zeros = decimals > 0 ? new string('0', decimals) : string.Empty;
                return value.ToString("0." + zeros + "e+00", CultureInfo.InvariantCulture);
            }
            else
            {
                int decimals = precision - 1 - exponent;
                if (decimals < 0)
                {
                    decimals = 0;
                }
                string result = value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                if (decimals == 0)
                {
                    result += ".";
                }
                return result;
            }
        }

        private static string normalizeExponent(string value, bool upper)
        {
            int index = value.IndexOfAny(new char[] { 'e', 'E' });
            if (index < 0)
            {
                return value;
            }

            string mantissa = value.Substring(0, index);
            string exponent = value.Substring(index + 1);
            char exponentSign = '+';
            if (exponent.Length > 0 && (exponent[0] == '+' || exponent[0] == '-'))
            {
                exponentSign = exponent[0];
                exponent = exponent.Substring(1);
            }

            exponent = exponent.TrimStart('0');
            if (exponent.Length < 2)
            {
                exponent = new string('0', 2 - exponent.Length) + exponent;
            }

            return mantissa + (upper ? "E" : "e") + exponentSign + exponent;
        }

        private static void appendPadded(StringBuilder result, string text, int width, bool leftAlign, bool zeroPad, int zeroInsertIndex)
        {
            if (width <= text.Length)
            {
                result.Append(text);
                return;
            }

            int padding = width - text.Length;
            if (leftAlign)
            {
                result.Append(text);
                result.Append(' ', padding);
            }
            else if (zeroPad)
            {
                if (zeroInsertIndex < 0)
                {
                    zeroInsertIndex = 0;
                }
                if (zeroInsertIndex > text.Length)
                {
                    zeroInsertIndex = text.Length;
                }
                result.Append(text, 0, zeroInsertIndex);
                result.Append('0', padding);
                result.Append(text, zeroInsertIndex, text.Length - zeroInsertIndex);
            }
            else
            {
                result.Append(' ', padding);
                result.Append(text);
            }
        }

        private static long toInt64(object value)
        {
            if (value is int)
            {
                return (int)value;
            }
            if (value is long)
            {
                return (long)value;
            }
            if (value is short)
            {
                return (short)value;
            }
            if (value is sbyte)
            {
                return (sbyte)value;
            }
            if (value is byte)
            {
                return (byte)value;
            }
            if (value is ushort)
            {
                return (ushort)value;
            }
            if (value is uint)
            {
                return (uint)value;
            }
            if (value is ulong)
            {
                return unchecked((long)(ulong)value);
            }
            if (value is char)
            {
                return (char)value;
            }
            if (value is bool)
            {
                return ((bool)value) ? 1 : 0;
            }

            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        private static ulong toUInt64(object value)
        {
            if (value is int)
            {
                return unchecked((uint)(int)value);
            }
            if (value is long)
            {
                return unchecked((ulong)(long)value);
            }
            if (value is short)
            {
                return unchecked((ushort)(short)value);
            }
            if (value is sbyte)
            {
                return unchecked((byte)(sbyte)value);
            }
            if (value is byte)
            {
                return (byte)value;
            }
            if (value is ushort)
            {
                return (ushort)value;
            }
            if (value is uint)
            {
                return (uint)value;
            }
            if (value is ulong)
            {
                return (ulong)value;
            }
            if (value is char)
            {
                return (char)value;
            }
            if (value is bool)
            {
                return ((bool)value) ? 1UL : 0UL;
            }

            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }
    }
}
