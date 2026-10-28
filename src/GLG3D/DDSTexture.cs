/**
  @file DDSTexture.cpp

  @author Corey Taylor, laundrypowder@spamcon.net

  Notes:
  <UL>
  <LI>http://msdn.microsoft.com/library/default.asp?url=/library/en-us/directx9_c/directx/graphics/reference/DDSFileReference/ddsfileformat.asp
  </UL>

  @created 2004-09-30
  @edited 2006-02-30
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace G3D
{
    internal sealed class DDSTexture
    {
        public int Width;
        public int Height;
        public TextureFormat BytesFormat;
        public List<List<byte[]>> MipMaps = new List<List<byte[]>>();

        private static uint readUInt32(byte[] data, int offset)
        {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }

        private static uint fourCC(char a, char b, char c, char d)
        {
            return (uint)(a | (b << 8) | (c << 16) | (d << 24));
        }

        public static DDSTexture load(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);
            if (data.Length < 128 || data[0] != (byte)'D' || data[1] != (byte)'D' || data[2] != (byte)'S' || data[3] != (byte)' ')
            {
                throw new GImage.Error("Loading \"" + filename + "\" failed. Not a DDS file.");
            }

            uint flags = readUInt32(data, 8);
            int height = (int)readUInt32(data, 12);
            int width = (int)readUInt32(data, 16);
            uint mipMapCount = (flags & 0x20000) != 0 ? readUInt32(data, 28) : 1;
            uint pixelFlags = readUInt32(data, 80);
            uint pixelCode = readUInt32(data, 84);
            uint caps2 = readUInt32(data, 116);
            bool cubeMap = (caps2 & 0x200) != 0;
            int faceCount = cubeMap ? 6 : 1;

            if ((flags & 0x6) != 0x6 || height <= 0 || width <= 0)
            {
                throw new GImage.Error("Loading \"" + filename + "\" failed. Invalid DDS dimensions.");
            }

            DDSTexture texture = new DDSTexture();
            int blockBytes;
            if ((pixelFlags & 0x4) != 0)
            {
                if (pixelCode == fourCC('D', 'X', 'T', '1'))
                {
                    texture.BytesFormat = TextureFormat.RGBA_DXT1;
                    blockBytes = 8;
                }
                else if (pixelCode == fourCC('D', 'X', 'T', '2') || pixelCode == fourCC('D', 'X', 'T', '3'))
                {
                    texture.BytesFormat = TextureFormat.RGBA_DXT3;
                    blockBytes = 16;
                }
                else if (pixelCode == fourCC('D', 'X', 'T', '4') || pixelCode == fourCC('D', 'X', 'T', '5'))
                {
                    texture.BytesFormat = TextureFormat.RGBA_DXT5;
                    blockBytes = 16;
                }
                else
                {
                    throw new GImage.Error("Loading \"" + filename + "\" failed. Unsupported DDS FourCC format.");
                }
            }
            else if ((pixelFlags & 0x40) != 0)
            {
                uint redMask = readUInt32(data, 92);
                uint greenMask = readUInt32(data, 96);
                uint blueMask = readUInt32(data, 100);
                uint alphaMask = readUInt32(data, 108);
                if (redMask != 0x000000FF || greenMask != 0x0000FF00 || blueMask != 0x00FF0000 || alphaMask != 0)
                {
                    throw new GImage.Error("Loading \"" + filename + "\" failed. Unsupported RGBA DDS format.");
                }
                texture.BytesFormat = TextureFormat.RGB8;
                blockBytes = 0;
            }
            else
            {
                throw new GImage.Error("Loading \"" + filename + "\" failed. Unknown DDS format.");
            }

            texture.Width = width;
            texture.Height = height;
            int offset = 128;
            int levelWidth = width;
            int levelHeight = height;
            for (int level = 0; level < mipMapCount; ++level)
            {
                List<byte[]> faces = new List<byte[]>();
                for (int face = 0; face < faceCount; ++face)
                {
                    int size = blockBytes == 0
                        ? levelWidth * levelHeight * 3
                        : Math.Max(1, (levelWidth + 3) / 4) * Math.Max(1, (levelHeight + 3) / 4) * blockBytes;
                    if (offset + size > data.Length)
                    {
                        throw new GImage.Error("Loading \"" + filename + "\" failed. Truncated DDS data.");
                    }
                    byte[] faceData = new byte[size];
                    Array.Copy(data, offset, faceData, 0, size);
                    faces.Add(faceData);
                    offset += size;
                }
                texture.MipMaps.Add(faces);
                levelWidth = Math.Max(1, levelWidth / 2);
                levelHeight = Math.Max(1, levelHeight / 2);
            }
            return texture;
        }
    }
}
