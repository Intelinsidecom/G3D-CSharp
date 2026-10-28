/**
 @file g3dmath.h
 
 Math util class.
 
 @maintainer Morgan McGuire, matrix@graphics3d.com
 @cite highestBit by Jukka Liimatta
 
 @created 2001-06-02
 @edited  2006-01-16

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
 */

using System;
using global::System.Diagnostics;

namespace G3D
{
    public static class g3dmath
    {
        public const double fuzzyEpsilon = 0.00001;
        public const double G3D_PI = 3.1415926535898;
        public const double G3D_HALF_PI = 1.5707963267949;
        public const double G3D_TWO_PI = 6.283185;

        private static readonly Random randomGenerator = new Random();

        public static double inf()
        {
            return double.PositiveInfinity;
        }

        public static double nan()
        {
            return double.NaN;
        }

        public static double pi()
        {
            return G3D_PI;
        }

        public static double halfPi()
        {
            return G3D_HALF_PI;
        }

        public static double twoPi()
        {
            return G3D_TWO_PI;
        }

        public static int iAbs(int iValue)
        {
            return iValue >= 0 ? iValue : -iValue;
        }

        public static int iCeil(double fValue)
        {
            return (int)Math.Ceiling(fValue);
        }

        public static int iClamp(int val, int low, int hi)
        {
            global::System.Diagnostics.Debug.Assert(low <= hi);
            if (val <= low)
            {
                return low;
            }
            else if (val >= hi)
            {
                return hi;
            }
            else
            {
                return val;
            }
        }

        public static double clamp(double val, double low, double hi)
        {
            global::System.Diagnostics.Debug.Assert(low <= hi);
            if (val <= low)
            {
                return low;
            }
            else if (val >= hi)
            {
                return hi;
            }
            else
            {
                return val;
            }
        }

        public static float clamp(float val, float low, float hi)
        {
            global::System.Diagnostics.Debug.Assert(low <= hi);
            if (val <= low)
            {
                return low;
            }
            else if (val >= hi)
            {
                return hi;
            }
            else
            {
                return val;
            }
        }

        public static double lerp(double a, double b, double f)
        {
            return a + (b - a) * f;
        }

        public static float lerp(float a, float b, float f)
        {
            return a + (b - a) * f;
        }

        public static int iWrap(int val, int hi)
        {
            if (val < 0)
            {
                return ((val % hi) + hi) % hi;
            }
            else
            {
                return val % hi;
            }
        }

        public static int iFloor(double fValue)
        {
            return (int)Math.Floor(fValue);
        }

        public static int iSign(int iValue)
        {
            return iValue > 0 ? 1 : (iValue < 0 ? -1 : 0);
        }

        public static int iSign(double fValue)
        {
            return fValue > 0.0 ? 1 : (fValue < 0.0 ? -1 : 0);
        }

        public static int iSign(float f)
        {
            return iSign((double)f);
        }

        public static int iRound(double fValue)
        {
            return (int)Math.Round(fValue);
        }

        public static int iRound(float f)
        {
            return (int)Math.Round((double)f);
        }

        public static int iRandom(int low, int hi)
        {
            int r = iFloor(low + (hi - low + 1) * randomGenerator.NextDouble());
            if (r > hi)
            {
                return hi;
            }
            else
            {
                return r;
            }
        }

        public static double abs(double fValue)
        {
            return Math.Abs(fValue);
        }

        public static double aCos(double fValue)
        {
            if (-1.0 < fValue)
            {
                if (fValue < 1.0)
                {
                    return Math.Acos(fValue);
                }
                else
                {
                    return 0.0;
                }
            }
            else
            {
                return G3D_PI;
            }
        }

        public static double aSin(double fValue)
        {
            if (-1.0 < fValue)
            {
                if (fValue < 1.0)
                {
                    return Math.Asin(fValue);
                }
                else
                {
                    return -G3D_HALF_PI;
                }
            }
            else
            {
                return G3D_HALF_PI;
            }
        }

        public static double aTan(double fValue)
        {
            return Math.Atan(fValue);
        }

        public static double aTan2(double fY, double fX)
        {
            return Math.Atan2(fY, fX);
        }

        public static double sign(double fValue)
        {
            if (fValue > 0.0)
            {
                return 1.0;
            }
            if (fValue < 0.0)
            {
                return -1.0;
            }
            return 0.0;
        }

        public static double square(double fValue)
        {
            return fValue * fValue;
        }

        public static bool isFinite(double x)
        {
            return !double.IsNaN(x) && !double.IsInfinity(x);
        }

        public static bool isNaN(double x)
        {
            return double.IsNaN(x);
        }

        public static int iMod3(int x)
        {
            return x % 3;
        }

        public static double unitRandom()
        {
            return randomGenerator.NextDouble();
        }

        public static double random(double low, double hi)
        {
            return low + (hi - low) * unitRandom();
        }

        public static double symmetricRandom()
        {
            return 2.0 * unitRandom() - 1.0;
        }

        public static float uniformRandom(float low = 0.0f, float hi = 1.0f)
        {
            return (hi - low) * (float)randomGenerator.NextDouble() + low;
        }

        public static float gaussRandom(float mean = 0.0f, float stdev = 1.0f)
        {
            float w;
            float x1;
            float x2;

            do
            {
                x1 = uniformRandom(-1.0f, 1.0f);
                x2 = uniformRandom(-1.0f, 1.0f);
                w = (float)(square(x1) + square(x2));
            }
            while (w > 1.0f);

            return x2 * (float)square(stdev) * (float)Math.Sqrt((-2.0f * Math.Log(w)) / w) + mean;
        }

        public static T min<T>(T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) <= 0 ? x : y;
        }

        public static T max<T>(T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) >= 0 ? x : y;
        }

        public static int iMin(int x, int y)
        {
            return x >= y ? y : x;
        }

        public static int iMax(int x, int y)
        {
            return x >= y ? x : y;
        }

        public static double sumSquares(double x, double y)
        {
            return x * x + y * y;
        }

        public static double sumSquares(double x, double y, double z)
        {
            return x * x + y * y + z * z;
        }

        public static double distance(double x, double y)
        {
            return Math.Sqrt(sumSquares(x, y));
        }

        public static double distance(double x, double y, double z)
        {
            return Math.Sqrt(sumSquares(x, y, z));
        }

        public static int highestBit(uint x)
        {
            int baseValue = 0;

            if ((x & 0xffff0000u) != 0)
            {
                baseValue = 16;
                x >>= 16;
            }
            if ((x & 0x0000ff00u) != 0)
            {
                baseValue += 8;
                x >>= 8;
            }
            if ((x & 0x000000f0u) != 0)
            {
                baseValue += 4;
                x >>= 4;
            }

            int[] lut = { -1, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3 };
            return baseValue + lut[x];
        }

        public static double eps(double a, double b)
        {
            double aa = abs(a) + 1;
            if (aa == inf())
            {
                return fuzzyEpsilon;
            }
            else
            {
                return fuzzyEpsilon * aa;
            }
        }

        public static bool fuzzyEq(double a, double b)
        {
            return (a == b) || (abs(a - b) <= eps(a, b));
        }

        public static bool fuzzyNe(double a, double b)
        {
            return !fuzzyEq(a, b);
        }

        public static bool fuzzyGt(double a, double b)
        {
            return a > b + eps(a, b);
        }

        public static bool fuzzyGe(double a, double b)
        {
            return a > b - eps(a, b);
        }

        public static bool fuzzyLt(double a, double b)
        {
            return a < b - eps(a, b);
        }

        public static bool fuzzyLe(double a, double b)
        {
            return a < b + eps(a, b);
        }

        public static float rsq(float x)
        {
            return 1.0f / (float)Math.Sqrt(x);
        }

        public static float SSErsq(float x)
        {
            return 1.0f / (float)Math.Sqrt(x);
        }

        public static int ceilPow2(uint input)
        {
            input -= 1;
            input |= input >> 16;
            input |= input >> 8;
            input |= input >> 4;
            input |= input >> 2;
            input |= input >> 1;
            return (int)(input + 1);
        }

        public static bool isPow2(int num)
        {
            return (num & -num) == num;
        }

        public static bool isOdd(int num)
        {
            return (num & 1) == 1;
        }

        public static bool isEven(int num)
        {
            return (num & 1) == 0;
        }

        public static double toRadians(double deg)
        {
            return deg * G3D_PI / 180.0;
        }

        public static double toDegrees(double rad)
        {
            return rad * 180.0 / G3D_PI;
        }

        public static bool any(float x)
        {
            return x != 0;
        }

        public static bool all(float x)
        {
            return x != 0;
        }

        public static float normalize(float v)
        {
            return v / v;
        }

        public static float dot(float a, float b)
        {
            return a * b;
        }

        public static float mul(float a, float b)
        {
            return a * b;
        }

        public static double exp2(double x)
        {
            return Math.Pow(2.0, x);
        }

        public static double rsqrt(double x)
        {
            return 1.0 / Math.Sqrt(x);
        }

        public static double sinc(double x)
        {
            double r = Math.Sin(x) / x;
            if (isNaN(r))
            {
                return 1.0;
            }
            else
            {
                return r;
            }
        }

        public static double wrap(double t, double lo, double hi)
        {
            if ((t >= lo) && (t < hi))
            {
                return t;
            }

            global::System.Diagnostics.Debug.Assert(hi > lo);

            double interval = hi - lo;
            return t - interval * iFloor((t - lo) / interval);
        }

        public static double wrap(double t, double hi)
        {
            return wrap(t, 0, hi);
        }

        public static bool isInf(double x)
        {
            return double.IsInfinity(x);
        }
    }
}
