/**
  @file tBigInt.cs

  Port of contrib/BigInt/test.cpp.

  The original's test bodies (testParser, testCompare, testAdd, testMul,
  testPow, testConvert) are commented out of its main, which runs only the
  random-number smoke test; the port runs them all, because the original left
  them intact for exactly that purpose.
*/

using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testBigIntParser()
        {
            // Test hex parsing
            {
                BigInt a = new BigInt("0xFF");
                Debug.debugAssert(a == new BigInt(255));
            }
            {
                BigInt a = new BigInt("0x0F");
                Debug.debugAssert(a == new BigInt(15));
            }
            {
                BigInt a = new BigInt("0xdEaDBeEf");
                Debug.debugAssert(a == new BigInt((long)0xdeadbeef));
            }
        }

        private static void testBigIntCompare()
        {
            {
                BigInt a = new BigInt(10);
                BigInt b = new BigInt(11);
                Debug.debugAssert(a < b);
            }

            {
                BigInt a = new BigInt(0);
                Debug.debugAssert(a == new BigInt(0));
                Debug.debugAssert(a != new BigInt(1));
            }

            {
                BigInt a = new BigInt(500);
                BigInt b = new BigInt(200);
                Debug.debugAssert(a > b);
                Debug.debugAssert(b < a);
            }

            {
                BigInt a = new BigInt(-5000000);
                BigInt b = new BigInt(-2000000);
                Debug.debugAssert(a < b);
                Debug.debugAssert(b > a);
            }

            {
                BigInt a = new BigInt(-5000000);
                BigInt b = new BigInt(-2000000);
                Debug.debugAssert(a <= b);
                Debug.debugAssert(b >= a);
            }

            {
                BigInt a = new BigInt(10000);
                BigInt b = new BigInt(10000);
                Debug.debugAssert(a <= b);
                Debug.debugAssert(b >= a);
            }
        }

        private static void testBigIntAdd()
        {
            // Two positives
            {
                BigInt a = new BigInt(1);
                Debug.debugAssert(a == new BigInt(1));
                Debug.debugAssert(a != new BigInt(0));
                Debug.debugAssert(a != new BigInt(11));
                a.addInPlace(new BigInt(10));
                Debug.debugAssert(a == new BigInt(11));
                Debug.debugAssert(a != new BigInt(1));

                a = new BigInt(834245);
                a.addInPlace(new BigInt(1023123));
                Debug.debugAssert(a == new BigInt((long)(834245 + 1023123)));
            }

            // Pos neg
            {
                BigInt a = new BigInt(1);
                BigInt b = new BigInt(-1);
                BigInt c = a + b;
                Debug.debugAssert(c == new BigInt(0));
            }

            // Neg pos
            {
                BigInt a = new BigInt(-5);
                BigInt b = new BigInt(1);
                BigInt c = a + b;
                Debug.debugAssert(c == new BigInt(-4));
            }

            // Neg neg
            {
                BigInt a = new BigInt(-5);
                BigInt b = new BigInt(-3);
                BigInt c = a + b;
                Debug.debugAssert(c == new BigInt(-8));
            }

            // Subtraction
            {
                BigInt a = new BigInt(0xFFFF);
                BigInt b = new BigInt(0x0047);
                BigInt c = a - b;
                Debug.debugAssert(c == new BigInt(((long)0xFFFF - (long)0x0047)));
            }

            // Case requiring carry
            {
                BigInt a = new BigInt(1024);
                BigInt b = new BigInt(600);
                BigInt c = a - b;

                Debug.debugAssert(c == new BigInt((1024 - 600)));
            }
        }

        private static void testBigIntMul()
        {
            {
                BigInt a = new BigInt(1);
                BigInt b = new BigInt(10);
                BigInt c = a * b;
                Debug.debugAssert(c == new BigInt(10));
            }

            {
                BigInt a = new BigInt(2);
                BigInt b = new BigInt(10);
                BigInt c = a * b;
                Debug.debugAssert(c == new BigInt(20));
            }

            {
                BigInt a = new BigInt(-5135);
                BigInt b = new BigInt(1243);
                BigInt c = a * b;
                Debug.debugAssert(c == new BigInt((-5135L * 1243L)));
            }
        }

        private static void testBigIntPow()
        {
            {
                BigInt a = new BigInt(10);
                BigInt c = a.pow(8);
                Debug.debugAssert(c == new BigInt((long)Math.Pow(10, 8)));
            }

            {
                BigInt a = new BigInt(2);
                BigInt c = a.pow(40);
                long x = (long)Math.Pow(2.0, 40.0);
                Debug.debugAssert(c == new BigInt(x));
            }

            {
                BigInt a = new BigInt(15);
                BigInt c = a.pow(3);
                long x = (long)Math.Pow(15, 3);
                Debug.debugAssert(c == new BigInt(x));
            }
        }

        private static void testBigIntConvert()
        {
            {
                BigInt a = new BigInt(-72137);
                int x = a.int32Value();
                Debug.debugAssert(x == -72137);
            }

            {
                long y = 12345678901L;
                BigInt a = new BigInt(y);
                long x = a.int64Value();
                Debug.debugAssert(x == y);
            }
        }

        /**
          The original draws twenty random numbers and prints them.  The port
          also checks that the values land in the requested range, which the
          original left unchecked, and that the generator is reachable from
          managed code at all.
        */
        private static void testBigIntRandom()
        {
            for (int i = 0; i < 20; ++i)
            {
                BigInt x = BigInt.random(new BigInt(0), new BigInt(255));
                Debug.debugAssert(x >= new BigInt(0));
                Debug.debugAssert(x <= new BigInt(255));
                Debug.debugAssert(x.int32Value() >= 0);
                Debug.debugAssert(x.int32Value() <= 255);
            }

            {
                BigInt x = BigInt.randomNumBits(16);
                Debug.debugAssert(x >= new BigInt(0));
                Debug.debugAssert(x < new BigInt(2).pow(16));
            }
        }

        private static void testBigIntSerialize()
        {
            BigInt a = new BigInt(0xdeadbeefL);
            a.subInPlace(new BigInt(12345));

            BinaryOutput b = new BinaryOutput();
            a.serialize(b);
            byte[] data = b.getCArray();

            BinaryInput bi = new BinaryInput(data, data.Length, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            BigInt c = BigInt.fromBinaryInput(bi);

            Debug.debugAssert(c.sign() == a.sign());
            Debug.debugAssert(c.size() == a.size());
            Debug.debugAssert(c == a);
        }

        private static void testBigInt()
        {
            testBigIntParser();
            testBigIntCompare();
            testBigIntAdd();
            testBigIntMul();
            testBigIntPow();
            testBigIntConvert();
            testBigIntRandom();
            testBigIntSerialize();

            // Division and modulo are explicitly unimplemented in G3D 6.09; the
            // original asserts false in both.  The port keeps the assertion and
            // then throws, so the failure is a message rather than silent.
            try
            {
                BigInt unused = new BigInt(10) / new BigInt(2);
                Debug.debugAssert(false);
            }
            catch (NotSupportedException)
            {
            }

            printf("passed\n");
        }
    }
}
