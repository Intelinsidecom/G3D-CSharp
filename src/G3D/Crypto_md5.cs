/**
 @file Crypto_md5.cpp

 @author Morgan McGuire, matrix@graphics3d.com


 @created 2006-03-28
 @edited  2006-04-06
 */

using System;

namespace G3D {

    public partial class MD5Hash {

        public MD5Hash(BinaryInput b) {
            hashValue = new byte[16];
            deserialize(b);
        }


        public void deserialize(BinaryInput b) {
            if (b == null) {
                throw new ArgumentNullException("b");
            }
            b.readBytes(hashValue, 16);
        }


        public void serialize(BinaryOutput b) {
            if (b == null) {
                throw new ArgumentNullException("b");
            }
            b.writeBytes(hashValue, 16);
        }
    }


    public partial class Crypto {
        /* Define the state of the MD5 Algorithm. */
        private sealed class MD5State {
            public readonly uint[] count = new uint[2];  /* message length in bits, lsw first */
            public readonly uint[] abcd = new uint[4];   /* digest buffer */
            public readonly byte[] buf = new byte[64];   /* accumulate block */
        }

        private const uint T_MASK = 0xFFFFFFFF;
        private const uint T1 = T_MASK ^ 0x28955b87; /* 0xd76aa478 */
        private const uint T2 = T_MASK ^ 0x173848a9; /* 0xe8c7b756 */
        private const uint T3 = 0x242070db;
        private const uint T4 = T_MASK ^ 0x3e423111; /* 0xc1bdceee */
        private const uint T5 = T_MASK ^ 0x0a83f050; /* 0xf57c0faf */
        private const uint T6 = 0x4787c62a;
        private const uint T7 = T_MASK ^ 0x57cfb9ec; /* 0xa8304613 */
        private const uint T8 = T_MASK ^ 0x02b96afe; /* 0xfd469501 */
        private const uint T9 = 0x698098d8;
        private const uint T10 = T_MASK ^ 0x74bb0850; /* 0x8b44f7af */
        private const uint T11 = T_MASK ^ 0x0000a44e; /* 0xffff5bb1 */
        private const uint T12 = T_MASK ^ 0x76a32841; /* 0x895cd7be */
        private const uint T13 = 0x6b901122;
        private const uint T14 = T_MASK ^ 0x02678e6c; /* 0xfd987193 */
        private const uint T15 = T_MASK ^ 0x5986bc71; /* 0xa679438e */
        private const uint T16 = 0x49b40821;
        private const uint T17 = T_MASK ^ 0x09e1da9d; /* 0xf61e2562 */
        private const uint T18 = T_MASK ^ 0x3fbf4cbf; /* 0xc040b340 */
        private const uint T19 = 0x265e5a51;
        private const uint T20 = T_MASK ^ 0x16493855; /* 0xe9b6c7aa */
        private const uint T21 = T_MASK ^ 0x29d0efa2; /* 0xd62f105d */
        private const uint T22 = 0x02441453;
        private const uint T23 = T_MASK ^ 0x275e197e; /* 0xd8a1e681 */
        private const uint T24 = T_MASK ^ 0x182c0437; /* 0xe7d3fbc8 */
        private const uint T25 = 0x21e1cde6;
        private const uint T26 = T_MASK ^ 0x3cc8f829; /* 0xc33707d6 */
        private const uint T27 = T_MASK ^ 0x0b2af278; /* 0xf4d50d87 */
        private const uint T28 = 0x455a14ed;
        private const uint T29 = T_MASK ^ 0x561c16fa; /* 0xa9e3e905 */
        private const uint T30 = T_MASK ^ 0x03105c07; /* 0xfcefa3f8 */
        private const uint T31 = 0x676f02d9;
        private const uint T32 = T_MASK ^ 0x72d5b375; /* 0x8d2a4c8a */
        private const uint T33 = T_MASK ^ 0x0005c6bd; /* 0xfffa3942 */
        private const uint T34 = T_MASK ^ 0x788e097e; /* 0x8771f681 */
        private const uint T35 = 0x6d9d6122;
        private const uint T36 = T_MASK ^ 0x021ac7f3; /* 0xfde5380c */
        private const uint T37 = T_MASK ^ 0x5b4115bb; /* 0xa4beea44 */
        private const uint T38 = 0x4bdecfa9;
        private const uint T39 = T_MASK ^ 0x0944b49f; /* 0xf6bb4b60 */
        private const uint T40 = T_MASK ^ 0x4140438f; /* 0xbebfbc70 */
        private const uint T41 = 0x289b7ec6;
        private const uint T42 = T_MASK ^ 0x155ed805; /* 0xeaa127fa */
        private const uint T43 = T_MASK ^ 0x2b10cf7a; /* 0xd4ef3085 */
        private const uint T44 = 0x04881d05;
        private const uint T45 = T_MASK ^ 0x262b2fc6; /* 0xd9d4d039 */
        private const uint T46 = T_MASK ^ 0x1924661a; /* 0xe6db99e5 */
        private const uint T47 = 0x1fa27cf8;
        private const uint T48 = T_MASK ^ 0x3b53a99a; /* 0xc4ac5665 */
        private const uint T49 = T_MASK ^ 0x0bd6ddbb; /* 0xf4292244 */
        private const uint T50 = 0x432aff97;
        private const uint T51 = T_MASK ^ 0x546bdc58; /* 0xab9423a7 */
        private const uint T52 = T_MASK ^ 0x036c5fc6; /* 0xfc93a039 */
        private const uint T53 = 0x655b59c3;
        private const uint T54 = T_MASK ^ 0x70f3336d; /* 0x8f0ccc92 */
        private const uint T55 = T_MASK ^ 0x00100b82; /* 0xffeff47d */
        private const uint T56 = T_MASK ^ 0x7a7ba22e; /* 0x85845dd1 */
        private const uint T57 = 0x6fa87e4f;
        private const uint T58 = T_MASK ^ 0x01d3191f; /* 0xfe2ce6e0 */
        private const uint T59 = 0xa3014314;
        private const uint T60 = 0x4e0811a1;
        private const uint T61 = T_MASK ^ 0x08ac817d; /* 0xf7537e82 */
        private const uint T62 = 0xbd3af235; /* 0xbd3af235 */
        private const uint T63 = 0x2ad7d2bb;
        private const uint T64 = T_MASK ^ 0x14792c6e; /* 0xeb86d391 */

        private static readonly byte[] md5Pad = new byte[] {
            0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        /*
          Copyright (C) 1999, 2000, 2002 Aladdin Enterprises.  All rights reserved.

          This software is provided 'as-is', without any express or implied
          warranty.  In no event will the authors be held liable for any damages
          arising from the use of this software.

          Permission is granted to anyone to use this software for any purpose,
          including commercial applications, and to alter it and redistribute it
          freely, subject to the following restrictions:

          1. The origin of this software must not be misrepresented; you must not
             claim that you wrote the original software. If you use this software
             in a product, an acknowledgment in the product documentation would be
             appreciated but is not required.
          2. Altered source versions must be plainly marked as such, and must not be
             misrepresented as being the original software.
          3. This notice may not be removed or altered from any source distribution.

          L. Peter Deutsch
          ghost@aladdin.com

        */
        /* $Id: Crypto_md5.cpp,v 1.3 2006/04/14 22:48:35 morgan3d Exp $ */
        /*
          Independent implementation of MD5 (RFC 1321).

          This code implements the MD5 Algorithm defined in RFC 1321, whose
          text is available at
            http://www.ietf.org/rfc/rfc1321.txt
          The code is derived from the text of the RFC, including the test suite
          (section A.5) but excluding the rest of Appendix A.  It does not include
          any code or documentation that is identified in the RFC as being
          copyrighted.

          The original and principal author of md5.c is L. Peter Deutsch
          <ghost@aladdin.com>. Other authors are noted in the change history
          that follows (in reverse chronological order):

          2002-04-13 lpd Clarified derivation from RFC 1321; now handles byte order
            either statically or dynamically; added missing #include <string.h>
            in library.
          2002-03-11 lpd Corrected argument list for main(), and added int return
            type, in test program and T value program.
          2002-02-21 lpd Added missing #include <stdio.h> in test program.
          2000-07-03 lpd Patched to eliminate warnings about "constant is
            unsigned in ANSI C, signed in traditional"; made test program
            self-checking.
          1999-11-04 lpd Edited comments slightly for automatic TOC extraction.
          1999-10-18 lpd Fixed typo in header comment (ansi2knr rather than md5).
          1999-05-03 lpd Original version.
        */

        /*
         * This package supports both compile-time and run-time determination of CPU
         * byte order.  If ARCH_IS_BIG_ENDIAN is defined as 0, the code will be
         * compiled to run only on little-endian CPUs; if ARCH_IS_BIG_ENDIAN is
         * defined as non-zero, the code will be compiled to run only on big-endian
         * CPUs; if ARCH_IS_BIG_ENDIAN is not defined, the code will be compiled to
         * run on either big- or little-endian CPUs, but will run slightly less
         * efficiently on either one than if ARCH_IS_BIG_ENDIAN is defined.
         */


        /**
         Computes the MD5 hash (message digest) of a byte stream, as defined by
         http://www.ietf.org/rfc/rfc1321.txt.

         @cite Based on implementation by L. Peter Deutsch, ghost@aladdin.com
        */
        public MD5Hash md5(byte[] bytes, int numBytes) {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }
            if (numBytes < 0 || numBytes > bytes.Length) {
                throw new ArgumentOutOfRangeException("numBytes");
            }

            MD5State state = new MD5State();
            md5_init(state);
            md5_append(state, bytes, 0, numBytes);

            MD5Hash h = new MD5Hash();
            byte[] digest = new byte[16];
            md5_finish(state, digest);
            for (int i = 0; i < 16; ++i) {
                h[i] = digest[i];
            }
            return h;
        }

        private static uint rotateLeft(uint x, int n) {
            return unchecked((x << n) | (x >> (32 - n)));
        }

        private static uint F(uint x, uint y, uint z) {
            return unchecked((x & y) | (~x & z));
        }

        private static uint G(uint x, uint y, uint z) {
            return unchecked((x & z) | (y & ~z));
        }

        private static uint H(uint x, uint y, uint z) {
            return x ^ y ^ z;
        }

        private static uint I(uint x, uint y, uint z) {
            return y ^ (x | ~z);
        }

        private static void setF(ref uint a, ref uint b, ref uint c, ref uint d, uint x, int s, uint ti) {
            uint t = unchecked(a + F(b, c, d) + x + ti);
            a = unchecked(rotateLeft(t, s) + b);
        }

        private static void setG(ref uint a, ref uint b, ref uint c, ref uint d, uint x, int s, uint ti) {
            uint t = unchecked(a + G(b, c, d) + x + ti);
            a = unchecked(rotateLeft(t, s) + b);
        }

        private static void setH(ref uint a, ref uint b, ref uint c, ref uint d, uint x, int s, uint ti) {
            uint t = unchecked(a + H(b, c, d) + x + ti);
            a = unchecked(rotateLeft(t, s) + b);
        }

        private static void setI(ref uint a, ref uint b, ref uint c, ref uint d, uint x, int s, uint ti) {
            uint t = unchecked(a + I(b, c, d) + x + ti);
            a = unchecked(rotateLeft(t, s) + b);
        }

        private static void md5_process(MD5State pms, byte[] data, int dataOffset) {
            uint a = pms.abcd[0], b = pms.abcd[1];
            uint c = pms.abcd[2], d = pms.abcd[3];
            uint[] X = new uint[16];

            for (int i = 0; i < 16; ++i) {
                int x = dataOffset + i * 4;
                X[i] = (uint)data[x] |
                       ((uint)data[x + 1] << 8) |
                       ((uint)data[x + 2] << 16) |
                       ((uint)data[x + 3] << 24);
            }

            /* Round 1. */
            /* Let [abcd k s i] denote the operation
               a = b + ((a + F(b,c,d) + X[k] + T[i]) <<< s). */
            /* Do the following 16 operations. */
            setF(ref a, ref b, ref c, ref d,  X[0],  7, T1);
            setF(ref d, ref a, ref b, ref c,  X[1], 12, T2);
            setF(ref c, ref d, ref a, ref b,  X[2], 17, T3);
            setF(ref b, ref c, ref d, ref a,  X[3], 22, T4);
            setF(ref a, ref b, ref c, ref d,  X[4],  7, T5);
            setF(ref d, ref a, ref b, ref c,  X[5], 12, T6);
            setF(ref c, ref d, ref a, ref b,  X[6], 17, T7);
            setF(ref b, ref c, ref d, ref a,  X[7], 22, T8);
            setF(ref a, ref b, ref c, ref d,  X[8],  7, T9);
            setF(ref d, ref a, ref b, ref c,  X[9], 12, T10);
            setF(ref c, ref d, ref a, ref b, X[10], 17, T11);
            setF(ref b, ref c, ref d, ref a, X[11], 22, T12);
            setF(ref a, ref b, ref c, ref d, X[12],  7, T13);
            setF(ref d, ref a, ref b, ref c, X[13], 12, T14);
            setF(ref c, ref d, ref a, ref b, X[14], 17, T15);
            setF(ref b, ref c, ref d, ref a, X[15], 22, T16);

            /* Round 2. */
            /* Let [abcd k s i] denote the operation
                a = b + ((a + G(b,c,d) + X[k] + T[i]) <<< s). */
            /* Do the following 16 operations. */
            setG(ref a, ref b, ref c, ref d,  X[1],  5, T17);
            setG(ref d, ref a, ref b, ref c,  X[6],  9, T18);
            setG(ref c, ref d, ref a, ref b, X[11], 14, T19);
            setG(ref b, ref c, ref d, ref a,  X[0], 20, T20);
            setG(ref a, ref b, ref c, ref d,  X[5],  5, T21);
            setG(ref d, ref a, ref b, ref c, X[10],  9, T22);
            setG(ref c, ref d, ref a, ref b, X[15], 14, T23);
            setG(ref b, ref c, ref d, ref a,  X[4], 20, T24);
            setG(ref a, ref b, ref c, ref d,  X[9],  5, T25);
            setG(ref d, ref a, ref b, ref c, X[14],  9, T26);
            setG(ref c, ref d, ref a, ref b,  X[3], 14, T27);
            setG(ref b, ref c, ref d, ref a,  X[8], 20, T28);
            setG(ref a, ref b, ref c, ref d, X[13],  5, T29);
            setG(ref d, ref a, ref b, ref c,  X[2],  9, T30);
            setG(ref c, ref d, ref a, ref b,  X[7], 14, T31);
            setG(ref b, ref c, ref d, ref a, X[12], 20, T32);

            /* Round 3. */
            /* Let [abcd k s t] denote the operation
                a = b + ((a + H(b,c,d) + X[k] + T[i]) <<< s). */
            setH(ref a, ref b, ref c, ref d,  X[5],  4, T33);
            setH(ref d, ref a, ref b, ref c,  X[8], 11, T34);
            setH(ref c, ref d, ref a, ref b, X[11], 16, T35);
            setH(ref b, ref c, ref d, ref a, X[14], 23, T36);
            setH(ref a, ref b, ref c, ref d,  X[1],  4, T37);
            setH(ref d, ref a, ref b, ref c,  X[4], 11, T38);
            setH(ref c, ref d, ref a, ref b,  X[7], 16, T39);
            setH(ref b, ref c, ref d, ref a, X[10], 23, T40);
            setH(ref a, ref b, ref c, ref d, X[13],  4, T41);
            setH(ref d, ref a, ref b, ref c,  X[0], 11, T42);
            setH(ref c, ref d, ref a, ref b,  X[3], 16, T43);
            setH(ref b, ref c, ref d, ref a,  X[6], 23, T44);
            setH(ref a, ref b, ref c, ref d,  X[9],  4, T45);
            setH(ref d, ref a, ref b, ref c, X[12], 11, T46);
            setH(ref c, ref d, ref a, ref b, X[15], 16, T47);
            setH(ref b, ref c, ref d, ref a,  X[2], 23, T48);

            /* Round 4. */
            /* Let [abcd k s t] denote the operation
                a = b + ((a + I(b,c,d) + X[k] + T[i]) <<< s). */
            setI(ref a, ref b, ref c, ref d,  X[0],  6, T49);
            setI(ref d, ref a, ref b, ref c,  X[7], 10, T50);
            setI(ref c, ref d, ref a, ref b, X[14], 15, T51);
            setI(ref b, ref c, ref d, ref a,  X[5], 21, T52);
            setI(ref a, ref b, ref c, ref d, X[12],  6, T53);
            setI(ref d, ref a, ref b, ref c,  X[3], 10, T54);
            setI(ref c, ref d, ref a, ref b, X[10], 15, T55);
            setI(ref b, ref c, ref d, ref a,  X[1], 21, T56);
            setI(ref a, ref b, ref c, ref d,  X[8],  6, T57);
            setI(ref d, ref a, ref b, ref c, X[15], 10, T58);
            setI(ref c, ref d, ref a, ref b,  X[6], 15, T59);
            setI(ref b, ref c, ref d, ref a, X[13], 21, T60);
            setI(ref a, ref b, ref c, ref d,  X[4],  6, T61);
            setI(ref d, ref a, ref b, ref c, X[11], 10, T62);
            setI(ref c, ref d, ref a, ref b,  X[2], 15, T63);
            setI(ref b, ref c, ref d, ref a,  X[9], 21, T64);

            /* Then perform the following additions. (That is increment each
               of the four registers by the value it had before this block
               was started.) */
            unchecked {
                pms.abcd[0] += a;
                pms.abcd[1] += b;
                pms.abcd[2] += c;
                pms.abcd[3] += d;
            }
        }

        private static void md5_process(MD5State pms, byte[] data) {
            md5_process(pms, data, 0);
        }

        /* Initialize the algorithm. */
        private static void md5_init(MD5State pms) {
            pms.count[0] = pms.count[1] = 0;
            pms.abcd[0] = 0x67452301;
            pms.abcd[1] = T_MASK ^ 0x10325476;
            pms.abcd[2] = T_MASK ^ 0x67452301;
            pms.abcd[3] = 0x10325476;
        }

        /* Append a string to the message. */
        private static void md5_append(MD5State pms, byte[] data, int dataOffset, int nbytes) {
            int left = nbytes;
            int offset = (int)((pms.count[0] >> 3) & 63);
            uint nbits = unchecked((uint)nbytes << 3);

            if (nbytes <= 0) {
                return;
            }

            /* Update the message length. */
            unchecked {
                pms.count[1] += (uint)(nbytes >> 29);
                pms.count[0] += nbits;
                if (pms.count[0] < nbits) {
                    pms.count[1]++;
                }
            }

            /* Process an initial partial block. */
            if (offset != 0) {
                int copy = (offset + nbytes > 64 ? 64 - offset : nbytes);

                Array.Copy(data, dataOffset, pms.buf, offset, copy);
                if (offset + copy < 64) {
                    return;
                }
                dataOffset += copy;
                left -= copy;
                md5_process(pms, pms.buf);
            }

            /* Process full blocks. */
            for (; left >= 64; dataOffset += 64, left -= 64) {
                md5_process(pms, data, dataOffset);
            }

            /* Process a final partial block. */
            if (left != 0) {
                Array.Copy(data, dataOffset, pms.buf, 0, left);
            }
        }

        /* Finish the message and return the digest. */
        private static void md5_finish(MD5State pms, byte[] digest) {
            byte[] data = new byte[8];

            /* Save the length before padding. */
            for (int i = 0; i < 8; ++i) {
                data[i] = (byte)(pms.count[i >> 2] >> ((i & 3) << 3));
            }
            /* Pad to 56 bytes mod 64. */
            int padLength = (int)((((55U - (pms.count[0] >> 3)) & 63U) + 1U));
            md5_append(pms, md5Pad, 0, padLength);
            /* Append the length. */
            md5_append(pms, data, 0, 8);
            for (int i = 0; i < 16; ++i) {
                digest[i] = (byte)(pms.abcd[i >> 2] >> ((i & 3) << 3));
            }
        }
    }

}
