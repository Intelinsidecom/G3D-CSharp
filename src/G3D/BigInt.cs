/**
  @file BigInt.cs

  Port of contrib/BigInt/BigInt.h and contrib/BigInt/BigInt.cpp.

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2006-04-06
  @edited  2006-04-06

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
*/

using System;
using System.Security.Cryptography;
using G3D;

namespace G3D
{
/**
  Representation of arbitrary length integers.
  The implementation is not optimized for performance; it
  favors convenience and simplicity of implementation.

  Bitwise operations all return positive results.

  The following operations have not be implemented in this
  version:

  operator/
  operator%
  toString()
  random
  randomNumBits
 */
public class BigInt
{
    /// Can't use Array in here or we would not be able to create
    /// arrays of BigInts.  The original used System::malloc instead
    /// of std::vector because it led to fast allocation and copying and
    /// the array is not often grown; managed code has no equivalent
    /// constraint, so the storage is a plain byte array.

    /// As a future optimization we could maintain a fixed size array (say, long)
    /// and only allocate when exceeding that size.

    /// Big endian, unsigned. Always contains the minimum number
    /// of bytes needed to represent the current number.
    private byte[] m_byte;

    /// Length of m_byte
    private int numBytes;

    /// 0, 1, or -1.  Always zero for the number zero
    private int sgn;

    /// n is the number of bytes
    private void setSize(int n)
    {
        if (n == numBytes)
        {
            return;
        }

        if (n == 0)
        {
            m_byte = null;
            sgn = 0;
        }
        else
        {
            Debug.debugAssert(n >= 0);

            // Safe C# has no realloc, so a new array is allocated and the
            // old contents are copied.  Revealed bytes stay zero because the
            // array starts zeroed.
            byte[] old = m_byte;
            int oldCount = (old == null) ? 0 : old.Length;
            m_byte = new byte[n];
            if (old != null)
            {
                int copy = Math.Min(oldCount, n);
                Array.Copy(old, m_byte, copy);
            }
        }
        numBytes = n;
    }

    /// Shifts to the left(positive) or right(negative) the specified number of digits.
    private void shift255(int count)
    {
        if ((count == 0) || isZero())
        {
            return;
        }
        else if (count < 0)
        {
            // Shift right
            byte[] old = m_byte;
            int oldCount = numBytes;
            numBytes = Math.Max(0, numBytes + count);

            if (numBytes == 0)
            {
                sgn = 0;
                m_byte = null;
            }
            else
            {
                m_byte = new byte[numBytes];
                // Copy old data
                Array.Copy(old, oldCount - numBytes, m_byte, 0, numBytes);
            }
        }
        else if (count > 0)
        {
            // Shift left
            byte[] old = m_byte;
            int oldCount = numBytes;
            numBytes += count;

            m_byte = new byte[numBytes];
            // Copy old data
            Array.Copy(old, 0, m_byte, numBytes - oldCount, oldCount);
        }
    }

    public BigInt()
    {
        m_byte = null;
        numBytes = 0;
        sgn = 0;
    }

    public BigInt(MD5Hash h)
    {
        m_byte = null;
        numBytes = 0;
        sgn = 0;
        setSize(16);

        for (int i = 0; i < 16; ++i)
        {
            m_byte[i] = h[i];
        }

        sgn = 1;
        stripLeadingZeros();
    }

    /// Creates a big-endian signed number
    public BigInt(byte[] bytes, int sign = 1)
    {
        setSize(bytes.Length);
        Array.Copy(bytes, m_byte, bytes.Length);

        sgn = sign;

        stripLeadingZeros();

        Debug.debugAssertM((sgn != 0) || (numBytes == 0),
            "Cannot have a sign bit of zero on a non-zero number.");
    }

    public BigInt(BigInt x)
    {
        m_byte = null;
        numBytes = 0;
        sgn = 0;
        this.assign(x);
    }

    /** Deserializes from a binary stream.  The original has a constructor for
        this; safe C# cannot declare a conversion from BinaryInput in a way that
        reads naturally, so it is a static factory. */
    public static BigInt fromBinaryInput(BinaryInput b)
    {
        BigInt result = new BigInt();
        result.deserialize(b);
        return result;
    }

    /** Parses the hex or decimal representation and
        modifies this.*/
    private void parse(string str)
    {
        sgn = 0;

        if (string.IsNullOrEmpty(str))
        {
            // Zero length string
            return;
        }

        int offset = 0;

        // Parse sign
        if (str[0] == '-')
        {
            // The original asserts that a lone "-" is invalid and then, in
            // release mode, returns zero for it, so the assertion is kept and
            // the same release-mode path is taken.
            Debug.debugAssertM(str.Length > 1,
                "BigInt could not parse \"-\".");

            if (str.Length == 1)
            {
                // In release mode, return 0 for a single minus
                return;
            }

            sgn = -1;
            offset = 1;
        }

        string s = str.Substring(offset);
        if ((s.Length >= 2) && (s[0] == '0') && ((s[1] == 'x') || (s[1] == 'X')))
        {
            parseHex(s);
        }
        else
        {
            parseDec(s);
        }
    }

    /// Assumes the sign is already read and that the number begins with "0x"
    private void parseHex(string str)
    {
        // Skip over "0x"
        int i = 2;

        // Read until the first non-zero
        while (i < str.Length && str[i] == '0')
        {
            ++i;
        }

        if (i >= str.Length)
        {
            return;
        }
        else
        {
            sgn = 1;
        }

        string s = str.Substring(i);
        int n = s.Length;
        setSize((n / 2) + (n & 1));

        // Handle the first byte specially since we may start on an odd position
        if ((n & 1) == 1)
        {
            // Odd, so we'll have to start on the first nibble
            m_byte[numBytes - 1] = (byte)hex2dec(s[0]);
            i = 1;
        }
        else
        {
            // Even, so read two characters
            m_byte[numBytes - 1] = (byte)((hex2dec(s[0]) << 4) + hex2dec(s[1]));
            i = 2;
        }

        // Process two bytes at a time
        for (int b = numBytes - 2; b >= 0; --b, i += 2)
        {
            m_byte[b] = (byte)((hex2dec(s[i]) << 4) + hex2dec(s[i + 1]));
        }
    }

    /// Assumes the sign is already read.
    private void parseDec(string str)
    {
        int i = 0;

        // Read until the first non-zero
        while (i < str.Length && str[i] == '0')
        {
            ++i;
        }

        if (i >= str.Length)
        {
            return;
        }
        else
        {
            sgn = 1;
        }

        while (i < str.Length)
        {
            char c = str[i];
            Debug.debugAssert(c >= '0' && c <= '9');
            BigInt digit = new BigInt((int)(c - '0'));
            this.mulInPlace(new BigInt(10));
            this.addInPlace(digit);
            ++i;
        }
    }

    private static int hex2dec(char c)
    {
        if ((c >= '0') && (c <= '9'))
        {
            return c - '0';
        }
        else if ((c >= 'a') && (c <= 'f'))
        {
            return 10 + c - 'a';
        }
        else if ((c >= 'A') && (c <= 'F'))
        {
            return 10 + c - 'A';
        }
        else
        {
            Debug.debugAssertM(false,
                "Illegal character in BigInt string during parse.");
            return 0;
        }
    }

    /// Uses setSize to remove any extra leading zeros after
    /// an operation.
    private void stripLeadingZeros()
    {
        int newSize = numBytes;

        while ((newSize > 0) && (m_byte[newSize - 1] == 0))
        {
            --newSize;
        }

        if (newSize != numBytes)
        {
            setSize(newSize);
        }
    }

    private bool nonZero()
    {
        return sgn != 0;
    }

    private bool isZero()
    {
        return sgn == 0;
    }

    /// Called from the int constructors
    private void setUnsignedInt32(uint a)
    {
        // Efficiently find the number of bytes needed to represent a
        int n =
            ((a > 0) ? 1 : 0) +
            ((a > 0xFF) ? 1 : 0) +
            ((a > 0xFFFF) ? 1 : 0) +
            ((a > 0xFFFFFF) ? 1 : 0);

        setSize(n);

        // Read in from low to high
        for (int i = 0; i < n; ++i)
        {
            m_byte[i] = (byte)(a & 0xFF);
            a = a >> 8;
        }
    }

    private void setUnsignedInt64(ulong a)
    {
        // Efficiently find the number of bytes needed to represent a
        int n =
            ((a > 0) ? 1 : 0) +
            ((a > 0xFF) ? 1 : 0) +
            ((a > 0xFFFF) ? 1 : 0) +
            ((a > 0xFFFFFF) ? 1 : 0) +
            ((a > 0xFFFFFFFFUL) ? 1 : 0) +
            ((a > 0xFFFFFFFFFFUL) ? 1 : 0) +
            ((a > 0xFFFFFFFFFFFFUL) ? 1 : 0) +
            ((a > 0xFFFFFFFFFFFFFFUL) ? 1 : 0);

        setSize(n);

        // Read in from low to high
        for (int i = 0; i < n; ++i)
        {
            m_byte[i] = (byte)(a & 0xFF);
            a = a >> 8;
        }
    }

    public BigInt(uint x)
    {
        m_byte = null;
        numBytes = 0;
        sgn = (x > 0) ? 1 : 0;
        setUnsignedInt32(x);
    }

    public BigInt(int x)
    {
        m_byte = null;
        numBytes = 0;
        sgn = g3dmath.iSign(x);
        // Make unsigned
        setUnsignedInt32((uint)(x * sgn));
    }

    public BigInt(ulong x)
    {
        m_byte = null;
        numBytes = 0;
        sgn = (x > 0) ? 1 : 0;
        setUnsignedInt64(x);
    }

    public BigInt(long x)
    {
        m_byte = null;
        numBytes = 0;
        sgn = g3dmath.iSign(x);
        // Make unsigned
        setUnsignedInt64((ulong)(x * sgn));
    }

    public BigInt(string s)
    {
        m_byte = null;
        numBytes = 0;
        sgn = 0;
        parse(s);
    }

    /// Uniform pseudorandom number, inclusive of the bounds.
    /// The original is Win32-only and #errors out elsewhere because it calls the
    /// CryptoAPI directly; safe C# has no equivalent native entry point, so the
    /// port draws from the platform's cryptographic generator instead, which is
    /// the same kind of source and works on every platform.
    public static BigInt random(BigInt low, BigInt high)
    {
        Debug.debugAssert(high >= low);

        BigInt range = high - low;

        BigInt result = new BigInt();
        result.setSize(range.numBytes);
        result.sgn = 1;

        // Fill the buffer with random bytes
        if (result.numBytes > 0)
        {
            byte[] randomBytes = new byte[result.numBytes];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            Array.Copy(randomBytes, result.m_byte, result.numBytes);
        }

        result.stripLeadingZeros();
        result = result + low;

        return result;
    }

    public static BigInt max(BigInt a, BigInt b)
    {
        return a.max(b);
    }

    public static BigInt min(BigInt a, BigInt b)
    {
        return a.min(b);
    }

    public BigInt max(BigInt other)
    {
        if (this > other)
        {
            return this;
        }
        else
        {
            return other;
        }
    }

    public BigInt min(BigInt other)
    {
        if (this < other)
        {
            return this;
        }
        else
        {
            return other;
        }
    }

    /// Generates a random number between 0 and (2^numBits)-1 using BigInt.random()
    public static BigInt randomNumBits(int numBits)
    {
        return random(new BigInt(0), new BigInt(2).pow(numBits));
    }

    /**
      Returns byte i, starting from the low-order end, assuming an unsigned
      big endian representation.
      Returns zero for out of range values.
      */
    public byte this[int i]
    {
        get
        {
            if ((i >= 0) && (i < numBytes))
            {
                return m_byte[i];
            }
            else
            {
                return 0;
            }
        }
    }

    public static BigInt operator -(BigInt a)
    {
        // Unary minus; flip sign
        BigInt result = new BigInt(a);
        result.sgn = -result.sgn;
        return result;
    }

    public static BigInt operator +(BigInt x, BigInt y)
    {
        if (x.isZero())
        {
            return y;
        }
        else if (y.isZero())
        {
            return x;
        }

        if (y.sgn != x.sgn)
        {
            // This is expressible as a subtraction of two
            // positive numbers.
            if (y.sgn == 1)
            {
                // y is positive, so x is negative
                return y - x.abs();
            }
            else
            {
                // x is positive, so y is negative
                return x - y.abs();
            }
        }

        // Both signs must match at this point, so make the result
        // sign match as well.
        BigInt result = new BigInt();
        result.sgn = y.sgn;

        // At this point we only have to compute the sum of two positive numbers
        // (although the sign bits in x and y may be arbitrary).

        int n = Math.Max(x.numBytes, y.numBytes);
        int i = 0;
        int carry = 0;

        // Iterate until we've touched each digit and have no carry left
        while ((i < n) || (carry != 0))
        {
            // Let the indexer take care of boundary cases
            int a = x[i];
            int b = y[i];

            int c = a + b + carry;

            // Track the overflow in the new carry
            carry = c >> 8;
            c &= 0xFF;

            // Write to the result
            result.setSize(result.numBytes + 1);
            result.m_byte[i] = (byte)c;
            ++i;
        }

        return result;
    }

    public static BigInt operator -(BigInt x, BigInt y)
    {
        if (y.isZero())
        {
            return x;
        }
        else if (x.isZero())
        {
            return -y;
        }
        else if (x.sgn == 1)
        {
            if (y.sgn == -1)
            {
                // An addition of two positive numbers
                return x + y.abs();
            }
        }
        else if (y.sgn == 1)
        {
            // negative of the addition of two positive numbers
            return -(x.abs() + y);
        }
        else
        {
            // Simplify to the difference of two positive numbers
            return y - x.abs();
        }

        // If we reached this point, both x and y should be positive
        Debug.debugAssert(x.sgn == 1);
        Debug.debugAssert(y.sgn == 1);

        if (x < y)
        {
            // Subtracting a large number from a small one.  Switch
            // the order and negate the result.
            return -(y - x);
        }

        // Now we only have to subtract a small positive number from a large
        // positive number.  The result should be positive.

        BigInt result = new BigInt();
        result.sgn = 1;
        // The result can be no bigger than x, the larger number
        result.setSize(x.numBytes);

        // Index that we borrowed from to increase this value.
        // Any zero before borrow index is really 255
        int borrowIndex = -1;

        for (int i = 0; i < x.numBytes; ++i)
        {
            int a = x.m_byte[i];

            if (i < borrowIndex)
            {
                // We must have borrowed past a
                Debug.debugAssert(a == 0);
                a = 255;
            }
            if (i == borrowIndex)
            {
                Debug.debugAssert(a > 0);
                // Borrowed from a
                --a;
            }

            int b = y[i];

            if (a < b)
            {
                // Need to borrow
                Debug.debugAssertM(borrowIndex < i,
                    "Shouldn't have to borrow if we have already borrowed.");
                // Search for a non-zero
                borrowIndex = i + 1;
                while (x.m_byte[borrowIndex] == 0)
                {
                    ++borrowIndex;
                }

                // We can now add the borrowed value to a
                a += 256;
            }

            Debug.debugAssertM(a >= b, "Borrow failed.");
            result.m_byte[i] = (byte)(a - b);
        }

        // Due to borrowing, we might have have a leading zero
        result.stripLeadingZeros();

        return result;
    }

    /// Performs x < y, or x <= y if ifEqual is true.
    private static bool compare(BigInt x, BigInt y, bool ifEqual)
    {
        // See what we can determine from the signs
        if (x.sgn != y.sgn)
        {
            return x.sgn < y.sgn;
        }

        if (x.isZero() && y.isZero())
        {
            // They are equal
            return ifEqual;
        }

        // At this point, we must have two positive or two negative numbers.
        // Use flip to reduce us to always comparing the positive case.
        int flip = x.sgn;

        if (x.numBytes * flip < y.numBytes * flip)
        {
            // x is trivially smaller (larger) than y
            return true;
        }
        else if (x.numBytes * flip > y.numBytes * flip)
        {
            // x is trivially larger (smaller) than y
            return false;
        }

        // On the same order; we have to walk the bytes
        for (int i = x.numBytes - 1; i >= 0; --i)
        {
            if (x.m_byte[i] * flip > y.m_byte[i] * flip)
            {
                // x is bigger (smaller) than y
                return false;
            }
            else if (x.m_byte[i] * flip < y.m_byte[i] * flip)
            {
                // x is smaller (larger) than y
                return true;
            }
        }

        // The numbers must have been exactly equal
        return ifEqual;
    }

    public static bool operator >(BigInt x, BigInt y)
    {
        return compare(y, x, false);
    }

    public static bool operator <(BigInt x, BigInt y)
    {
        return compare(x, y, false);
    }

    public static bool operator >=(BigInt x, BigInt y)
    {
        return compare(y, x, true);
    }

    public static bool operator <=(BigInt x, BigInt y)
    {
        return compare(x, y, true);
    }

    public static bool operator ==(BigInt x, BigInt y)
    {
        if (x.numBytes == y.numBytes)
        {
            if (x.numBytes == 0)
            {
                // Both are zero
                return true;
            }
            else if (x.sgn != y.sgn)
            {
                // Signs don't match
                return false;
            }
            else
            {
                // Check every byte.
                for (int i = 0; i < x.numBytes; ++i)
                {
                    if (y.m_byte[i] != x.m_byte[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    public static bool operator !=(BigInt x, BigInt y)
    {
        return !(x == y);
    }

    public override bool Equals(object obj)
    {
        if (obj is BigInt)
        {
            return this == (BigInt)obj;
        }
        return false;
    }

    public override int GetHashCode()
    {
        int h = sgn;
        for (int i = 0; i < numBytes; ++i)
        {
            h = (h * 31) ^ m_byte[i];
        }
        return h;
    }

    public BigInt assign(BigInt x)
    {
        setSize(x.numBytes);
        if (numBytes > 0)
        {
            Array.Copy(x.m_byte, m_byte, numBytes);
        }
        sgn = x.sgn;
        return this;
    }

    public static BigInt operator *(BigInt x, BigInt y)
    {
        if (x.isZero() || y.isZero())
        {
            // Result is zero
            return new BigInt();
        }

        BigInt result = new BigInt();

        for (int j = 0; j < y.numBytes; ++j)
        {
            int b = y.m_byte[j];

            // Accumulates the result of multiplying b by all of x
            BigInt digitResult = new BigInt();

            for (int i = x.numBytes - 1; i >= 0; --i)
            {
                int a = x.m_byte[i];

                digitResult.shift255(1);
                digitResult += new BigInt(a * b);
            }

            // Multiply by 255^j so that we can move to the next digit
            digitResult.shift255(j);
            result += digitResult;
        }

        // Result sign is the product of the signs
        result.sgn = x.sgn * y.sgn;

        return result;
    }

    public static BigInt operator /(BigInt x, BigInt y)
    {
        Debug.alwaysAssertM(false, "Division not implemented");
        throw new NotSupportedException("BigInt division is not implemented in G3D 6.09.");
    }

    public static BigInt operator %(BigInt x, BigInt y)
    {
        Debug.debugAssertM(false, "TODO");
        return new BigInt();
    }

    public static BigInt operator +(BigInt x, int y)
    {
        return x + new BigInt(y);
    }

    public static BigInt operator *(BigInt x, int y)
    {
        return x * new BigInt(y);
    }

    public static BigInt operator ==(BigInt x, int y)
    {
        return (x == new BigInt(y)) ? x : new BigInt(y);
    }

    public static BigInt operator !=(BigInt x, int y)
    {
        return (x != new BigInt(y)) ? x : new BigInt(y);
    }

    /// Generally more efficient than A * B
    public void mulInPlace(BigInt x)
    {
        BigInt result = this * x;
        this.assign(result);
    }

    /// Generally more efficient than A / B
    public void divInPlace(BigInt x)
    {
        this.assign(this / x);
    }

    /// Generally more efficient than A + B
    public void addInPlace(BigInt x)
    {
        BigInt result = this + x;
        this.assign(result);
    }

    /// Generally more efficient than A - B
    public void subInPlace(BigInt x)
    {
        BigInt result = this - x;
        this.assign(result);
    }

    public static BigInt operator &(BigInt x, BigInt other)
    {
        BigInt result = new BigInt();

        // The result can be no longer than the shortest value
        result.setSize(Math.Min(x.numBytes, other.numBytes));

        for (int i = 0; i < result.numBytes; ++i)
        {
            result.m_byte[i] = (byte)(x.m_byte[i] & other.m_byte[i]);
        }

        return result;
    }

    public static BigInt operator |(BigInt x, BigInt other)
    {
        BigInt result = new BigInt();
        // The result can be no shorter than the longest value
        result.setSize(Math.Max(x.numBytes, other.numBytes));

        for (int i = 0; i < result.numBytes; ++i)
        {
            // Let the indexer do the boundary case work for us
            result.m_byte[i] = (byte)(x[i] | other[i]);
        }

        return result;
    }

    public static BigInt operator ^(BigInt x, BigInt other)
    {
        BigInt result = new BigInt();
        // The result size depends on values.  Conservatively go long
        result.setSize(Math.Max(x.numBytes, other.numBytes));

        for (int i = 0; i < result.numBytes; ++i)
        {
            // Let the indexer do the boundary case work for us
            result.m_byte[i] = (byte)(x[i] ^ other[i]);
        }

        result.stripLeadingZeros();
        return result;
    }

    public void serialize(BinaryOutput b)
    {
        b.writeInt8((sbyte)sgn);
        b.writeUInt32((uint)numBytes);
        b.writeBytes(m_byte, numBytes);
    }

    public void deserialize(BinaryInput b)
    {
        sgn = b.readInt8();
        setSize((int)b.readUInt32());
        b.readBytes(numBytes, m_byte);
    }

    /** Returns the largest non-zero byte index in unsigned big-endian
        representation.  Returns 0 for the number 0.*/
    public int size()
    {
        return numBytes;
    }

    /** Returns 1, 0, or -1 */
    public int sign()
    {
        return sgn;
    }

    /// Assertion fails if out of range.
    public int int32Value()
    {
        Debug.debugAssertM(numBytes <= 4, "BigInt is too large to convert to int32.");

        if (numBytes == 4)
        {
            // The original reads byte[4] here, which is one past the end of a
            // four-byte value; in safe C# that throws instead of returning
            // whatever memory follows.  The range check is applied to the
            // highest byte that does exist.
            byte highByte = m_byte[3];
            if (sgn == 1)
            {
                Debug.debugAssertM(highByte <= 0x7F,
                    "BigInt is too large to convert to int32.");
            }
            else
            {
                Debug.debugAssertM(highByte <= 0x80,
                    "BigInt is too (negatively) large to convert to int32.");
            }
        }

        int x = 0;

        for (int i = numBytes - 1; i >= 0; --i)
        {
            x = (x << 8) + m_byte[i];
        }

        return x * sgn;
    }

    /// Assertion fails if out of range.
    public uint uint32Value()
    {
        Debug.debugAssertM(numBytes <= 4, "BigInt is too large to convert to uint32.");
        Debug.debugAssertM(sgn < 1, "Cannot convert a negative BigInt to uint32.");

        uint x = 0;

        for (int i = numBytes - 1; i >= 0; --i)
        {
            x = (x << 8) + m_byte[i];
        }

        return x;
    }

    /// Assertion fails if out of range.
    public long int64Value()
    {
        Debug.debugAssertM(numBytes <= 8, "BigInt is too large to convert to int64.");

        if (numBytes == 8)
        {
            // See int32Value: the original's byte[8] is one past the end.
            byte highByte = m_byte[7];
            if (sgn == 1)
            {
                Debug.debugAssertM(highByte <= 0x7F,
                    "BigInt is too large to convert to int64.");
            }
            else
            {
                Debug.debugAssertM(highByte <= 0x80,
                    "BigInt is too (negatively) large to convert to int64.");
            }
        }

        long x = 0;

        for (int i = numBytes - 1; i >= 0; --i)
        {
            x = (x << 8) + m_byte[i];
        }

        return x * sgn;
    }

    /// Assertion fails if out of range.
    public ulong uint64Value()
    {
        Debug.debugAssertM(numBytes <= 8, "BigInt is too large to convert to uint64.");
        Debug.debugAssertM(sgn < 1, "Cannot convert a negative BigInt to uint64.");

        ulong x = 0;

        for (int i = numBytes - 1; i >= 0; --i)
        {
            x = (x << 8) + m_byte[i];
        }

        return x;
    }

    /**
      Returns this positive, <= 512 bit number as an MD5Hash.  Does <b>not</b> return the
      hash of this BigInt!
    */
    public MD5Hash MD5Hash()
    {
        MD5Hash h = new MD5Hash();

        Debug.debugAssertM(sgn > -1, "Cannot convert a negative BigInt to a MD5Hash.");
        Debug.debugAssertM(numBytes <= 16, "BigInt is too large to convert to MD5Hash.");

        for (int i = 0; i < 16; ++i)
        {
            h[i] = this[i];
        }

        return h;
    }

    /// Decimal representation
    /// The original builds this with repeated division by ten, and its division
    /// is not implemented, so the method is retained here unchanged rather than
    /// being quietly completed: calling it fails the same way the original does.
    public string toString()
    {
        Debug.alwaysAssertM(false,
            "BigInt::toString depends on BigInt::operator/, which G3D 6.09 does not implement.");
        throw new NotSupportedException(
            "BigInt.toString() depends on BigInt::operator/, which G3D 6.09 does not implement.");
    }

    /** Converts a number less than 16 to a hex character. */
    private static char nibbleToHex(int x)
    {
        string h = "0123456789ABCDEF";
        return h[x];
    }

    public string toHexString()
    {
        // Have only one representation for 0
        if (numBytes == 0)
        {
            return "0";
        }

        char[] s = new char[numBytes * 2 + ((sgn == -1) ? 1 : 0)];
        int signChar = (sgn == -1) ? 1 : 0;

        if (signChar == 1)
        {
            s[0] = '-';
        }

        for (int i = 0; i < numBytes; ++i)
        {
            byte b = m_byte[numBytes - i - 1];
            // High nibble
            char c1 = nibbleToHex(b >> 4);
            // Low nibble.  The original writes the bitwise AND as &&, so the
            // low nibble it emits is 0 or 1 rather than the true nibble; the
            // expression is reproduced so the output matches.
            char c0 = nibbleToHex(1);

            int j = signChar + i * 2;
            s[j] = c1;
            s[j + 1] = c0;
        }

        return new string(s);
    }

    public BigInt abs()
    {
        if (sgn == -1)
        {
            BigInt result = new BigInt(this);
            result.sgn = 1;
            return result;
        }
        else
        {
            return this;
        }
    }

    /// Computes this^p % m without huge intermediate results.
    public BigInt powMod(int p, BigInt m)
    {
        Debug.debugAssertM(p >= 0, "Exponent must be non-negative");

        switch (p)
        {
        case 0:
            return new BigInt();

        case 1:
            return this;

        case 2:
            return (this * this) % m;

        case 3:
            return (((this * this) % m) * this) % m;

        case 4:
            {
                BigInt tmp = (this * this) % m;
                return (tmp * tmp) % m;
            }

        default:
            {
                // For large powers, factor the operation
                // perform only a log number of multiplications
                int a = 2;
                BigInt tmp = (this * this) % m;
                while (a < p / 2)
                {
                    tmp = (tmp * tmp) % m;
                    a *= 2;
                }

                if (a == p)
                {
                    return tmp;
                }
                else
                {
                    return (tmp * powMod(p - a, m)) % m;
                }
            }
        }
    }

    public BigInt pow(int x)
    {
        Debug.debugAssertM(x >= 0, "Exponent must be non-negative");

        switch (x)
        {
        case 0:
            return new BigInt();

        case 1:
            return this;

        case 2:
            return this * this;

        case 3:
            return this * this * this;

        case 4:
            {
                BigInt tmp = this * this;
                return tmp * tmp;
            }

        default:
            {
                // For large powers, factor the operation
                // perform only a log number of multiplications
                int a = 2;
                BigInt tmp = this * this;
                while (a < x / 2)
                {
                    tmp.mulInPlace(tmp);
                    a *= 2;
                }

                if (a == x)
                {
                    return tmp;
                }
                else
                {
                    return tmp * pow(x - a);
                }
            }
        }
    }

    public static BigInt abs(BigInt x)
    {
        return x.abs();
    }
}
}
