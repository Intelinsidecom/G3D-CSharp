using System;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using DxgiFormat = SharpDX.DXGI.Format;

namespace G3D.Direct3D11
{
    internal static class TextureFormatMap
    {
        public static DxgiFormat ToDXGI(TextureFormat format)
        {
            if (format == null || format == TextureFormat.AUTO) return DxgiFormat.R8G8B8A8_UNorm;
            if (format == TextureFormat.RGB8 || format == TextureFormat.RGBA8) return DxgiFormat.R8G8B8A8_UNorm;
            if (format == TextureFormat.RGB5 || format == TextureFormat.RGB5A1) return DxgiFormat.R8G8B8A8_UNorm;
            if (format == TextureFormat.L8 || format == TextureFormat.A8) return DxgiFormat.R8_UNorm;
            if (format == TextureFormat.L16) return DxgiFormat.R16_UNorm;
            if (format == TextureFormat.L16F) return DxgiFormat.R16_Float;
            if (format == TextureFormat.L32F) return DxgiFormat.R32_Float;
            if (format == TextureFormat.LA8) return DxgiFormat.R8G8_UNorm;
            if (format == TextureFormat.LA16) return DxgiFormat.R16G16_UNorm;
            if (format == TextureFormat.LA16F) return DxgiFormat.R16G16_Float;
            if (format == TextureFormat.LA32F) return DxgiFormat.R32G32_Float;
            if (format == TextureFormat.RGB16 || format == TextureFormat.RGBA16) return DxgiFormat.R16G16B16A16_UNorm;
            if (format == TextureFormat.RGB16F || format == TextureFormat.RGBA16F) return DxgiFormat.R16G16B16A16_Float;
            if (format == TextureFormat.RGB32F || format == TextureFormat.RGBA32F) return DxgiFormat.R32G32B32A32_Float;
            if (format == TextureFormat.DEPTH16) return DxgiFormat.D16_UNorm;
            if (format == TextureFormat.DEPTH24) return DxgiFormat.D24_UNorm_S8_UInt;
            if (format == TextureFormat.DEPTH32) return DxgiFormat.D32_Float;
            if (format == TextureFormat.STENCIL8) return DxgiFormat.R8_UNorm;
            if (format == TextureFormat.RGB_DXT1 || format == TextureFormat.RGBA_DXT1) return DxgiFormat.BC1_UNorm;
            if (format == TextureFormat.RGBA_DXT3) return DxgiFormat.BC2_UNorm;
            if (format == TextureFormat.RGBA_DXT5) return DxgiFormat.BC3_UNorm;
            return DxgiFormat.R8G8B8A8_UNorm;
        }

        public static bool IsDepth(TextureFormat format)
        {
            return format != null && format.getIsDepth();
        }
    }

    internal static class ClipSpace
    {
        public static Matrix4 ToDirect3D(Matrix4 matrix)
        {
            Matrix4 result = matrix;
            result.m20 = matrix.m20 * 0.5f + matrix.m30 * 0.5f;
            result.m21 = matrix.m21 * 0.5f + matrix.m31 * 0.5f;
            result.m22 = matrix.m22 * 0.5f + matrix.m32 * 0.5f;
            result.m23 = matrix.m23 * 0.5f + matrix.m33 * 0.5f;
            result.m30 = matrix.m20;
            result.m31 = matrix.m21;
            result.m32 = matrix.m22;
            result.m33 = matrix.m23;
            return result;
        }

        public static Matrix4 Multiply(Matrix4 a, Matrix4 b)
        {
            Matrix4 result = new Matrix4();
            for (int r = 0; r < 4; ++r)
                for (int c = 0; c < 4; ++c)
                    for (int k = 0; k < 4; ++k)
                        result.setElt(r, c, result.elt(r, c) + a.elt(r, k) * b.elt(k, c));
            return result;
        }
    }
}
