/**
 @file BinaryFormat.h
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @author  2005-06-03
 @edited  2005-06-03

 Copyright 2000-2005, Morgan McGuire.
 All rights reserved.
 */

namespace G3D
{
    public enum BinaryFormat
    {
        FIRST_BINFMT = 1000,

        BOOL8_BINFMT,
        UINT8_BINFMT,
        INT8_BINFMT,
        UINT16_BINFMT,
        INT16_BINFMT,
        UINT32_BINFMT,
        INT32_BINFMT,
        UINT64_BINFMT,
        INT64_BINFMT,
        UINT128_BINFMT,
        INT128_BINFMT,
        FLOAT16_BINFMT,
        FLOAT32_BINFMT,
        FLOAT64_BINFMT,
        VECTOR2_BINFMT,
        VECTOR2INT16_BINFMT,
        VECTOR3_BINFMT,
        VECTOR3INT16_BINFMT,
        VECTOR4_BINFMT,
        VECTOR4INT16_BINFMT,
        COLOR3_BINFMT,
        COLOR3UINT8_BINFMT,
        COLOR3INT16_BINFMT,
        COLOR4_BINFMT,
        COLOR4UINT8_BINFMT,
        COLOR4INT16_BINFMT,
        STRING_BINFMT,
        STRINGEVEN_BINFMT,
        STRING8_BINFMT,
        STRING16_BINFMT,
        STRING32_BINFMT,

        CHUNK_BINFMT,

        CUSTOM_BINFMT,

        LAST_BINFMT
    }

    public enum G3DEndian
    {
        G3D_BIG_ENDIAN = 0,
        G3D_LITTLE_ENDIAN = 1
    }

    public static class BinaryFormatUtil
    {
        public static int byteSize(BinaryFormat f)
        {
            switch (f)
            {
            case BinaryFormat.BOOL8_BINFMT:
            case BinaryFormat.UINT8_BINFMT:
            case BinaryFormat.INT8_BINFMT:
                return 1;

            case BinaryFormat.UINT16_BINFMT:
            case BinaryFormat.INT16_BINFMT:
                return 2;

            case BinaryFormat.FLOAT16_BINFMT:
                return 2;

            case BinaryFormat.UINT32_BINFMT:
            case BinaryFormat.INT32_BINFMT:
            case BinaryFormat.FLOAT32_BINFMT:
                return 4;

            case BinaryFormat.FLOAT64_BINFMT:
            case BinaryFormat.UINT64_BINFMT:
            case BinaryFormat.INT64_BINFMT:
                return 8;

            case BinaryFormat.INT128_BINFMT:
            case BinaryFormat.UINT128_BINFMT:
                return 16;

            case BinaryFormat.VECTOR2_BINFMT:
                return 2 * 4;

            case BinaryFormat.VECTOR2INT16_BINFMT:
                return 2 * 2;

            case BinaryFormat.VECTOR3_BINFMT:
                return 3 * 4;

            case BinaryFormat.VECTOR3INT16_BINFMT:
                return 3 * 2;

            case BinaryFormat.VECTOR4_BINFMT:
                return 4 * 4;

            case BinaryFormat.VECTOR4INT16_BINFMT:
                return 4 * 4;

            case BinaryFormat.COLOR3_BINFMT:
                return 3 * 4;

            case BinaryFormat.COLOR3UINT8_BINFMT:
                return 3 * 1;

            case BinaryFormat.COLOR3INT16_BINFMT:
                return 3 * 2;

            case BinaryFormat.COLOR4_BINFMT:
                return 4 * 4;

            case BinaryFormat.COLOR4UINT8_BINFMT:
                return 4 * 1;

            case BinaryFormat.COLOR4INT16_BINFMT:
                return 4 * 2;

            default:
                return -1;
            }
        }
    }
}
