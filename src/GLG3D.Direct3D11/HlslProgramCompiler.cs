using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;

namespace G3D.Direct3D11
{
    /**
      Compiles and reflects HLSL shader programs without requiring a Direct3D 11
      device.  The result is a backend-neutral ShaderProgramInfo describing the
      uniforms, attributes, and samplers of the program.
    */
    public static class HlslProgramCompiler
    {
        /**
          Compiles a vertex and pixel shader pair.

          @param vertexSource HLSL vertex shader source.
          @param pixelSource HLSL pixel shader source.
          @return Program description; Success is false when compilation fails and
                  Messages contains the compiler output.
        */
        public static ShaderProgramInfo compile(string vertexSource, string pixelSource)
        {
            ShaderProgramInfo info = new ShaderProgramInfo();
            info.Language = GraphicsShaderLanguage.HLSL;

            CompilationResult vertexResult;
            CompilationResult pixelResult;
            try
            {
                vertexResult = compileShader(vertexSource, "main", "vs_4_0");
                pixelResult = compileShader(pixelSource, "main", "ps_4_0");
            }
            catch (Exception exception)
            {
                info.Success = false;
                info.Messages = exception.Message;
                return info;
            }

            if (vertexResult.HasErrors || pixelResult.HasErrors)
            {
                info.Success = false;
                info.VertexErrors = vertexResult.HasErrors ? vertexResult.Message : "";
                info.PixelErrors = pixelResult.HasErrors ? pixelResult.Message : "";
                info.Messages = info.VertexErrors + info.PixelErrors;
                return info;
            }

            info.Success = true;
            reflectUniforms(vertexResult.Bytecode, false, info);
            reflectUniforms(pixelResult.Bytecode, true, info);
            reflectAttributes(vertexResult.Bytecode, info);
            scanSamplers(vertexSource, info);
            scanSamplers(pixelSource, info);
            return info;
        }

        private static readonly Regex samplerPattern =
            new Regex(@"\bTexture(1D|2D|3D|Cube)\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*register\(t(\d+)\)", RegexOptions.Compiled);

        /**
          Scans translated HLSL source for texture declarations and records the
          corresponding sampler uniforms.  The SharpDX shader-reflection resource
          API is not usable for shader model 4 bytecode in this build, so the
          sampler bindings are read directly from the translated source instead.
        */
        public static void scanSamplers(string source, ShaderProgramInfo info)
        {
            if (string.IsNullOrEmpty(source))
            {
                return;
            }
            foreach (Match match in samplerPattern.Matches(source))
            {
                ShaderUniformInfo sampler = new ShaderUniformInfo();
                sampler.Name = match.Groups[2].Value;
                sampler.Type = translateSamplerType(match.Groups[1].Value);
                sampler.TextureUnit = int.Parse(match.Groups[3].Value);
                sampler.Register = sampler.TextureUnit;
                info.Uniforms.Add(sampler);
            }
        }

        /**
          Compiles a single HLSL shader stage.

          @param source HLSL shader source.
          @param profile Shader profile such as "vs_4_0".
          @param entryPoint Entry point name.
          @return The compiler result.
        */
        public static CompilationResult compileShader(string source, string entryPoint, string profile)
        {
            IntPtr text = Marshal.StringToHGlobalAnsi(source);
            try
            {
                return ShaderBytecode.Compile(text, source.Length, entryPoint, profile, ShaderFlags.None, EffectFlags.None, null, null, null, SecondaryDataFlags.None, null);
            }
            finally
            {
                Marshal.FreeHGlobal(text);
            }
        }

        /**
          Reflects the uniform declarations of a compiled shader stage.

          @param bytecode Compiled shader bytecode.
          @param isPixelStage True for a pixel shader.
          @param info Description that receives the uniforms.
        */
        public static void reflectUniforms(ShaderBytecode bytecode, bool isPixelStage, ShaderProgramInfo info)
        {
            ShaderReflection reflection = new ShaderReflection(bytecode.Data);
            for (int bufferIndex = 0; ; ++bufferIndex)
            {
                ConstantBuffer constantBuffer;
                try
                {
                    constantBuffer = reflection.GetConstantBuffer(bufferIndex);
                }
                catch (Exception)
                {
                    break;
                }
                if (constantBuffer == null)
                {
                    break;
                }

                ConstantBufferDescription description;
                try
                {
                    description = constantBuffer.Description;
                }
                catch (Exception)
                {
                    break;
                }

                for (int variableIndex = 0; variableIndex < description.VariableCount; ++variableIndex)
                {
                    ShaderReflectionVariable variable = constantBuffer.GetVariable(variableIndex);
                    ShaderVariableDescription variableDescription = variable.Description;
                    ShaderTypeDescription typeDescription = variable.GetVariableType().Description;

                    ShaderUniformInfo uniform = new ShaderUniformInfo();
                    uniform.Name = variableDescription.Name;
                    uniform.Type = translateType(typeDescription);
                    uniform.Size = typeDescription.ElementCount;
                    uniform.ConstantBuffer = description.Name;
                    uniform.ByteOffset = variableDescription.StartOffset;
                    info.Uniforms.Add(uniform);
                }
            }

            for (int resourceIndex = 0; ; ++resourceIndex)
            {
                InputBindingDescription resource;
                try
                {
                    resource = reflection.GetResourceBindingDescription(resourceIndex);
                }
                catch (Exception)
                {
                    break;
                }
                if (string.IsNullOrEmpty(resource.Name))
                {
                    break;
                }
                if (resource.Type != ShaderInputType.Texture)
                {
                    continue;
                }

                ShaderUniformInfo sampler = new ShaderUniformInfo();
                sampler.Name = resource.Name;
                sampler.Type = translateSamplerType(resource.Dimension);
                sampler.TextureUnit = resource.BindPoint;
                sampler.Register = resource.BindPoint;
                info.Uniforms.Add(sampler);
            }
        }

        /**
          Reflects the vertex input signature of a compiled vertex shader.

          @param bytecode Compiled vertex shader bytecode.
          @param info Description that receives the attributes.
        */
        public static void reflectAttributes(ShaderBytecode bytecode, ShaderProgramInfo info)
        {
            ShaderReflection reflection = new ShaderReflection(bytecode.Data);
            for (int index = 0; ; ++index)
            {
                ShaderParameterDescription parameter;
                try
                {
                    parameter = reflection.GetInputParameterDescription(index);
                }
                catch (Exception)
                {
                    break;
                }
                if (string.IsNullOrEmpty(parameter.SemanticName))
                {
                    break;
                }

                ShaderAttributeInfo attribute = new ShaderAttributeInfo();
                attribute.Name = parameter.SemanticName;
                attribute.Location = parameter.Register;
                attribute.ComponentCount = componentCount(parameter.ComponentType, parameter.UsageMask, parameter.SemanticName);
                info.Attributes.Add(attribute);
            }
        }

        private static int componentCount(RegisterComponentType type, RegisterComponentMaskFlags mask, string semanticName)
        {
            int bits = ((int)mask & 1) != 0 ? 1 : 0;
            bits += ((int)mask & 2) != 0 ? 1 : 0;
            bits += ((int)mask & 4) != 0 ? 1 : 0;
            bits += ((int)mask & 8) != 0 ? 1 : 0;
            if (bits > 0)
            {
                return bits;
            }

            switch (type)
            {
                case RegisterComponentType.Float32:
                case RegisterComponentType.SInt32:
                case RegisterComponentType.UInt32:
                    break;
                default:
                    return 4;
            }

            switch (semanticName)
            {
                case "POSITION": return 3;
                case "NORMAL": return 3;
                case "TANGENT": return 3;
                case "BINORMAL": return 3;
                case "COLOR": return 4;
                case "TEXCOORD": return 4;
                default: return 4;
            }
        }

        private static ShaderProgramType translateSamplerType(ShaderResourceViewDimension dimension)
        {
            switch (dimension)
            {
                case ShaderResourceViewDimension.Texture1D: return ShaderProgramType.Sampler1D;
                case ShaderResourceViewDimension.Texture2D: return ShaderProgramType.Sampler2D;
                case ShaderResourceViewDimension.Texture3D: return ShaderProgramType.Sampler3D;
                case ShaderResourceViewDimension.TextureCube: return ShaderProgramType.SamplerCube;
                default: return ShaderProgramType.Sampler2D;
            }
        }

        private static ShaderProgramType translateSamplerType(string dimension)
        {
            switch (dimension)
            {
                case "1D": return ShaderProgramType.Sampler1D;
                case "2D": return ShaderProgramType.Sampler2D;
                case "3D": return ShaderProgramType.Sampler3D;
                case "Cube": return ShaderProgramType.SamplerCube;
                default: return ShaderProgramType.Sampler2D;
            }
        }

        private static ShaderProgramType translateType(ShaderTypeDescription description)
        {
            if (description.Class == ShaderVariableClass.Scalar)
            {
                if (description.Type == ShaderVariableType.Float) { return ShaderProgramType.Float; }
                if (description.Type == ShaderVariableType.Int || description.Type == ShaderVariableType.UInt) { return ShaderProgramType.Int; }
                return ShaderProgramType.Float;
            }
            if (description.Class == ShaderVariableClass.Vector)
            {
                if (description.Type == ShaderVariableType.Float)
                {
                    if (description.RowCount == 2) { return ShaderProgramType.Float2; }
                    if (description.RowCount == 3) { return ShaderProgramType.Float3; }
                    return ShaderProgramType.Float4;
                }
                if (description.Type == ShaderVariableType.Int || description.Type == ShaderVariableType.UInt)
                {
                    if (description.RowCount == 2) { return ShaderProgramType.Int2; }
                    if (description.RowCount == 3) { return ShaderProgramType.Int3; }
                    return ShaderProgramType.Int4;
                }
                return ShaderProgramType.Float4;
            }
            if (description.Class == ShaderVariableClass.MatrixRows || description.Class == ShaderVariableClass.MatrixColumns)
            {
                return description.RowCount == 3 ? ShaderProgramType.Matrix3 : ShaderProgramType.Matrix4;
            }
            return ShaderProgramType.Float4;
        }
    }
}
