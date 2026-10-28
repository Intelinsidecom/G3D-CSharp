using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static string md5HashToHex(MD5Hash hash)
        {
            const string digits = "0123456789abcdef";
            char[] output = new char[32];
            for (int i = 0; i < 16; ++i)
            {
                output[i * 2] = digits[hash[i] >> 4];
                output[i * 2 + 1] = digits[hash[i] & 15];
            }
            return new string(output);
        }

        private static void testCrypto()
        {
            printf("Crypto  ");

            Crypto crypto = new Crypto();
            string[] messages = new string[] {
                "",
                "a",
                "abc",
                "message digest",
                "abcdefghijklmnopqrstuvwxyz",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
                "12345678901234567890123456789012345678901234567890123456789012345678901234567890",
                "The quick brown fox jumps over the lazy dog"
            };
            string[] expectedMD5 = new string[] {
                "d41d8cd98f00b204e9800998ecf8427e",
                "0cc175b9c0f1b6a831c399e269772661",
                "900150983cd24fb0d6963f7d28e17f72",
                "f96b697d7cb7938d525a2f31aaf161d0",
                "c3fcd3d76192e4007dfb496cca67e13b",
                "d174ab98d277d9f5a5611c2c9f419d9f",
                "57edf4a22be3c955ac49da2e2107b67a",
                "9e107d9d372bb6826bd81d3542a419d6"
            };

            for (int i = 0; i < messages.Length; ++i)
            {
                byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(messages[i]);
                MD5Hash hash = crypto.md5(bytes, bytes.Length);
                CheckEqual(expectedMD5[i], md5HashToHex(hash), "Crypto.md5 RFC 1321 vector " + i);
            }

            int[] boundaryLengths = new int[] { 55, 56, 63, 64, 65 };
            string[] boundaryMD5 = new string[] {
                "ef1772b6dff9a122358552954ad0df65",
                "3b0c8ac703f828b04c6c197006d17218",
                "b06521f39153d618550606be297466d5",
                "014842d480b571495a4a0363793f7367",
                "c743a45e0d2e6a95cb859adae0248435"
            };

            for (int i = 0; i < boundaryLengths.Length; ++i)
            {
                byte[] bytes = new byte[boundaryLengths[i]];
                for (int j = 0; j < bytes.Length; ++j)
                {
                    bytes[j] = (byte)'a';
                }
                CheckEqual(boundaryMD5[i], md5HashToHex(crypto.md5(bytes, bytes.Length)),
                           "Crypto.md5 padding boundary " + boundaryLengths[i]);
            }

            MD5Hash zero = new MD5Hash();
            bool allZero = true;
            for (int i = 0; i < 16; ++i)
            {
                allZero = allZero && (zero[i] == 0);
            }
            Check(allZero, "MD5Hash default value");

            MD5Hash first = new MD5Hash();
            MD5Hash second = new MD5Hash();
            first[0] = 1;
            second[0] = 1;
            Check(first == second, "MD5Hash equality");
            Check(first.GetHashCode() == second.GetHashCode(), "MD5Hash equal hashes");
            second[1] = 2;
            Check(first != second, "MD5Hash inequality");

            bool indexOutOfRange = false;
            try
            {
                byte ignored = first[16];
            }
            catch (IndexOutOfRangeException)
            {
                indexOutOfRange = true;
            }
            Check(indexOutOfRange, "MD5Hash index bounds");

            MD5Hash serializedHash = crypto.md5(global::System.Text.Encoding.ASCII.GetBytes("abc"), 3);
            byte[] serialized = new byte[16];
            BinaryOutput output = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
            serializedHash.serialize(output);
            output.commit(serialized);
            CheckEqual(16, output.getLength(), "MD5Hash serialized length");

            BinaryInput input = new BinaryInput(serialized, 16, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            MD5Hash restored = new MD5Hash(input);
            Check(restored == serializedHash, "MD5Hash constructor deserialization");
            CheckEqual(16, input.getPosition(), "MD5Hash constructor consumed 16 bytes");

            BinaryInput secondInput = new BinaryInput(serialized, 16, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            MD5Hash restoredByMethod = new MD5Hash();
            restoredByMethod.deserialize(secondInput);
            Check(restoredByMethod == serializedHash, "MD5Hash deserialize");

            Check(Crypto.crc32(new byte[0], 0) == 0xFFFFFFFFU, "Crypto.crc32 empty input");
            Check(Crypto.crc32(global::System.Text.Encoding.ASCII.GetBytes("a"), 1) == 0x174841BCU,
                  "Crypto.crc32 a");
            Check(Crypto.crc32(global::System.Text.Encoding.ASCII.GetBytes("123456789"), 9) == 0x340BC6D9U,
                  "Crypto.crc32 123456789");
            Check(Crypto.crc32(global::System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"), 43) ==
                  0xBEB05CC6U, "Crypto.crc32 quick brown fox");

            byte[] crcBytes = global::System.Text.Encoding.ASCII.GetBytes("123456789");
            Check(Crypto.crc32(crcBytes, crcBytes.Length) == ~System.crc32(crcBytes, crcBytes.Length),
                  "Crypto.crc32 preserves the original final complement omission");

            CheckEqual(303, Crypto.numSmallPrimes(), "Crypto.numSmallPrimes");
            CheckEqual(2, Crypto.smallPrime(0), "Crypto.smallPrime first");
            CheckEqual(3, Crypto.smallPrime(1), "Crypto.smallPrime second");
            CheckEqual(541, Crypto.smallPrime(99), "Crypto.smallPrime 99");
            CheckEqual(547, Crypto.smallPrime(100), "Crypto.smallPrime 100");
            CheckEqual(1999, Crypto.smallPrime(302), "Crypto.smallPrime last");

            printf("passed\n");
        }
    }
}
