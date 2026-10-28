using System;
using System.Collections.Generic;

namespace G3D
{
    public enum ShaderProgramType
    {
        Float,
        Int,
        Bool,
        Float2,
        Float3,
        Float4,
        Int2,
        Int3,
        Int4,
        Bool2,
        Bool3,
        Bool4,
        Matrix2,
        Matrix3,
        Matrix4,
        Sampler1D,
        Sampler2D,
        Sampler3D,
        SamplerCube,
        SamplerRectangle
    }

    public sealed class ShaderUniformInfo
    {
        public string Name;
        public ShaderProgramType Type;
        public int Size = 1;
        public int Location = -1;
        public int TextureUnit = -1;
        public string ConstantBuffer;
        public int ByteOffset = -1;
        public int Register = -1;
        public int Slot = -1;
    }

    public sealed class ShaderAttributeInfo
    {
        public string Name;
        public int Location = -1;
        public int ComponentCount;
        public string Semantic;
        public int SemanticIndex;
    }

    public sealed class ShaderProgramInfo
    {
        public int ProgramID;
        public GraphicsShaderLanguage Language;
        public bool Success;
        public string Messages = "";
        public string VertexErrors = "";
        public string PixelErrors = "";
        public string LinkErrors = "";
        public readonly List<ShaderUniformInfo> Uniforms = new List<ShaderUniformInfo>();
        public readonly List<ShaderAttributeInfo> Attributes = new List<ShaderAttributeInfo>();
    }

    /**
      Backend-neutral helpers for the shader program reflection model.
     */
    public static class ShaderProgram
    {
        /** Converts from int and bool types to float types (e.g. GL_INT_VEC2_ARB -> GL_FLOAT_VEC2_ARB).
            Other types are left unmodified.*/
        public static ShaderProgramType canonicalType(ShaderProgramType type)
        {
            switch (type)
            {
                case ShaderProgramType.Int:
                case ShaderProgramType.Bool:
                    return ShaderProgramType.Float;

                case ShaderProgramType.Int2:
                case ShaderProgramType.Bool2:
                    return ShaderProgramType.Float2;

                case ShaderProgramType.Int3:
                case ShaderProgramType.Bool3:
                    return ShaderProgramType.Float3;

                case ShaderProgramType.Int4:
                case ShaderProgramType.Bool4:
                    return ShaderProgramType.Float4;

                default:
                    return type;
            }
        }

        /** Returns true for types that are textures (e.g., GL_TEXTURE_2D) */
        public static bool isSamplerType(ShaderProgramType type)
        {
            return type == ShaderProgramType.Sampler1D ||
                   type == ShaderProgramType.Sampler2D ||
                   type == ShaderProgramType.Sampler3D ||
                   type == ShaderProgramType.SamplerCube ||
                   type == ShaderProgramType.SamplerRectangle;
        }

        /** For printing error messages. */
        public static string typeToString(ShaderProgramType type)
        {
            switch (type)
            {
                case ShaderProgramType.Float: return "GL_FLOAT";
                case ShaderProgramType.Int: return "GL_INT";
                case ShaderProgramType.Bool: return "GL_BOOL_ARB";
                case ShaderProgramType.Float2: return "GL_FLOAT_VEC2_ARB";
                case ShaderProgramType.Float3: return "GL_FLOAT_VEC3_ARB";
                case ShaderProgramType.Float4: return "GL_FLOAT_VEC4_ARB";
                case ShaderProgramType.Int2: return "GL_INT_VEC2_ARB";
                case ShaderProgramType.Int3: return "GL_INT_VEC3_ARB";
                case ShaderProgramType.Int4: return "GL_INT_VEC4_ARB";
                case ShaderProgramType.Bool2: return "GL_BOOL_VEC2_ARB";
                case ShaderProgramType.Bool3: return "GL_BOOL_VEC3_ARB";
                case ShaderProgramType.Bool4: return "GL_BOOL_VEC4_ARB";
                case ShaderProgramType.Matrix2: return "GL_FLOAT_MAT2_ARB";
                case ShaderProgramType.Matrix3: return "GL_FLOAT_MAT3_ARB";
                case ShaderProgramType.Matrix4: return "GL_FLOAT_MAT4_ARB";
                case ShaderProgramType.Sampler1D: return "GL_SAMPLER_1D_ARB";
                case ShaderProgramType.Sampler2D: return "GL_SAMPLER_2D_ARB";
                case ShaderProgramType.Sampler3D: return "GL_SAMPLER_3D_ARB";
                case ShaderProgramType.SamplerCube: return "GL_SAMPLER_CUBE_ARB";
                case ShaderProgramType.SamplerRectangle: return "GL_SAMPLER_2D_RECT_ARB";
                default: return "** unknown type**";
            }
        }
    }
}
