/**
  @file GlslToHlslTranslator.h

  Converts the GLSL subset used by GLG3D shaders into HLSL shader model 4
  source so that the Direct3D 11 backend can execute the same shaders as
  the OpenGL backend.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace G3D
{
    /**
      Translates GLSL shader source into equivalent HLSL shader model 4 source.

      The translator supports the GLSL 1.10/1.20 constructs used by the GLG3D
      shader pipeline: attributes, varyings, uniform scalars, vectors, and
      matrices, 2D and cube samplers, built-in position and fragment color
      variables, and the vector/matrix helper functions that have direct HLSL
      equivalents.  Unsupported constructs raise NotSupportedException instead
      of producing incorrect shader code.

      Matrices are translated into row-major HLSL constant-buffer members.  The
      Direct3D 11 backend uploads the G3D matrix bytes in row-major order, so
      HLSL mul(matrix, vector) reproduces the GLSL matrix-times-vector result
      that the OpenGL backend computes with the original column-major upload.
    */
    public static class GlslToHlslTranslator
    {
        private sealed class SamplerMapping
        {
            public string Name;
            public int TextureRegister;
            public int SamplerRegister;
        }

        private static readonly Regex attributePattern = new Regex(@"^\s*(?:attribute|in)\s+(float|vec2|vec3|vec4|mat2|mat3|mat4|int|ivec2|ivec3|ivec4)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(\[[^\]]*\])?\s*;\s*$", RegexOptions.Compiled);
        private static readonly Regex vertexOutPattern = new Regex(@"^\s*(?:varying|out)\s+(float|vec2|vec3|vec4|int|ivec2|ivec3|ivec4)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(\[[^\]]*\])?\s*;\s*$", RegexOptions.Compiled);
        private static readonly Regex pixelInPattern = new Regex(@"^\s*(?:varying|in)\s+(float|vec2|vec3|vec4|int|ivec2|ivec3|ivec4)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(\[[^\]]*\])?\s*;\s*$", RegexOptions.Compiled);
        private static readonly Regex uniformPattern = new Regex(@"^\s*uniform\s+(float|vec2|vec3|vec4|mat2|mat3|mat4|int|ivec2|ivec3|ivec4|sampler1D|sampler2D|sampler3D|samplerCube)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(\[[^\]]*\])?\s*;\s*$", RegexOptions.Compiled);
        private static readonly Regex precisionPattern = new Regex(@"^\s*precision\s+\w+\s+\w+\s*;\s*$", RegexOptions.Compiled);
        private static readonly Regex matrixConstructorPattern = new Regex(@"\bmat([34])\s*\(([^()]*)\)", RegexOptions.Compiled);

        /**
          Translates a GLSL vertex shader into an HLSL vertex shader.

          @param source GLSL vertex shader source.
          @return HLSL shader model 4 vertex shader source.
        */
        public static string translateVertex(string source)
        {
            return translate(source, true, new Dictionary<string, int>(), ref _unusedRegister);
        }

        /**
          Translates a GLSL vertex shader into an HLSL vertex shader.

          @param source GLSL vertex shader source.
          @param samplerRegisterMap Shared map from sampler name to texture register
                  index, used so that the same sampler keeps the same register across
                  both stages of one program.
          @param nextTextureRegister Next free texture/sampler register index.
          @return HLSL shader model 4 vertex shader source.
        */
        public static string translateVertex(string source, Dictionary<string, int> samplerRegisterMap, ref int nextTextureRegister)
        {
            return translate(source, true, samplerRegisterMap, ref nextTextureRegister);
        }

        /**
          Translates a GLSL pixel shader into an HLSL pixel shader.

          @param source GLSL pixel shader source.
          @return HLSL shader model 4 pixel shader source.
        */
        public static string translatePixel(string source)
        {
            return translate(source, false, new Dictionary<string, int>(), ref _unusedRegister);
        }

        /**
          Translates a GLSL pixel shader into an HLSL pixel shader.

          @param source GLSL pixel shader source.
          @param samplerRegisterMap Shared map from sampler name to texture register
                  index, used so that the same sampler keeps the same register across
                  both stages of one program.
          @param nextTextureRegister Next free texture/sampler register index.
          @return HLSL shader model 4 pixel shader source.
        */
        public static string translatePixel(string source, Dictionary<string, int> samplerRegisterMap, ref int nextTextureRegister)
        {
            return translate(source, false, samplerRegisterMap, ref nextTextureRegister);
        }

        /**
          Translates a GLSL vertex/pixel shader pair into HLSL, assigning a single
          shared set of texture/sampler registers so that a sampler used in both
          stages keeps the same register index on each stage.

          @param vertexSource GLSL vertex shader source.
          @param pixelSource GLSL pixel shader source.
          @param vertexHlsl Receives the translated vertex shader source.
          @param pixelHlsl Receives the translated pixel shader source.
        */
        public static void translatePair(string vertexSource, string pixelSource, out string vertexHlsl, out string pixelHlsl)
        {
            Dictionary<string, int> samplerRegisterMap = new Dictionary<string, int>();
            int nextTextureRegister = 0;
            vertexHlsl = translate(vertexSource, true, samplerRegisterMap, ref nextTextureRegister);
            pixelHlsl = translate(pixelSource, false, samplerRegisterMap, ref nextTextureRegister);
        }

        private static int _unusedRegister = 0;


        /**
          Returns the names of the sampler uniforms declared by a GLSL shader.

          @param source GLSL shader source.
          @return Sampler uniform names in declaration order.
        */
        public static string[] getSamplerNames(string source)
        {
            List<string> names = new List<string>();
            foreach (string line in splitLines(source))
            {
                Match match = uniformPattern.Match(line);
                if (match.Success && match.Groups[3].Value == "" && isSamplerType(match.Groups[1].Value))
                {
                    names.Add(match.Groups[2].Value);
                }
            }
            return names.ToArray();
        }

        private static bool isSamplerType(string type)
        {
            return type == "sampler1D" || type == "sampler2D" || type == "sampler3D" || type == "samplerCube";
        }

        private static string[] splitLines(string source)
        {
            return (source ?? "").Replace("\r\n", "\n").Split('\n');
        }

        private static string translate(string source, bool vertexStage, Dictionary<string, int> samplerRegisterMap, ref int nextTextureRegister)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            List<string> body = new List<string>();
            List<string> cbufferMembers = new List<string>();
            List<string> textureDeclarations = new List<string>();
            List<string> samplerDeclarations = new List<string>();
            List<string> matrixNames = new List<string>();
            List<SamplerMapping> samplers = new List<SamplerMapping>();
            Dictionary<string, string> attributeTypes = new Dictionary<string, string>();

            // Ordered declarations used to build the HLSL entry-point signature.
            List<string> attributeParams = new List<string>();
            List<string> varyingParams = new List<string>();
            bool hasPixelOut = false;
            string pixelOutType = "";
            string pixelOutName = "";

            int attributeIndex = 0;

            foreach (string rawLine in splitLines(source))
            {
                string line = rawLine;
                string trimmed = line.Trim();

                if (trimmed.StartsWith("#version") || trimmed.StartsWith("#extension"))
                {
                    continue;
                }
                if (precisionPattern.IsMatch(trimmed))
                {
                    continue;
                }

                if (vertexStage)
                {
                    Match attributeMatch = attributePattern.Match(trimmed);
                    if (attributeMatch.Success)
                    {
                        if (attributeMatch.Groups[3].Value != "")
                        {
                            throw new NotSupportedException("G3D::GlslToHlslTranslator does not support array attributes ('" + attributeMatch.Groups[2].Value + "').");
                        }
                        string name = attributeMatch.Groups[2].Value;
                        attributeTypes[name] = attributeMatch.Groups[1].Value;
                        attributeParams.Add(translateType(attributeMatch.Groups[1].Value) + " " + name + " : " + attributeSemantic(name, attributeIndex));
                        attributeIndex++;
                        continue;
                    }

                    Match varyingMatch = vertexOutPattern.Match(trimmed);
                    if (varyingMatch.Success)
                    {
                        if (varyingMatch.Groups[3].Value != "")
                        {
                            throw new NotSupportedException("G3D::GlslToHlslTranslator does not support array varyings ('" + varyingMatch.Groups[2].Value + "').");
                        }
                        string name = varyingMatch.Groups[2].Value;
                        varyingParams.Add("out " + translateType(varyingMatch.Groups[1].Value) + " " + name + " : " + varyingSemantic(name));
                        continue;
                    }
                }
                else
                {
                    Match varyingMatch = pixelInPattern.Match(trimmed);
                    if (varyingMatch.Success)
                    {
                        if (varyingMatch.Groups[3].Value != "")
                        {
                            throw new NotSupportedException("G3D::GlslToHlslTranslator does not support array varyings ('" + varyingMatch.Groups[2].Value + "').");
                        }
                        string name = varyingMatch.Groups[2].Value;
                        varyingParams.Add("in " + translateType(varyingMatch.Groups[1].Value) + " " + name + " : " + varyingSemantic(name));
                        continue;
                    }

                    // GLSL 1.30 pixel shaders declare the color output with 'out'.
                    Regex pixelOutPattern = new Regex(@"^\s*out\s+(vec4|vec3)\s+([A-Za-z_][A-Za-z0-9_]*)\s*;\s*$");
                    Match pixelOutMatch = pixelOutPattern.Match(trimmed);
                    if (pixelOutMatch.Success)
                    {
                        hasPixelOut = true;
                        pixelOutType = translateType(pixelOutMatch.Groups[1].Value);
                        pixelOutName = pixelOutMatch.Groups[2].Value;
                        continue;
                    }
                }

                Match uniformMatch = uniformPattern.Match(trimmed);
                if (uniformMatch.Success)
                {
                    if (uniformMatch.Groups[3].Value != "")
                    {
                        throw new NotSupportedException("G3D::GlslToHlslTranslator does not support array uniforms ('" + uniformMatch.Groups[2].Value + "').");
                    }
                    string type = uniformMatch.Groups[1].Value;
                    string name = uniformMatch.Groups[2].Value;
                    if (isSamplerType(type))
                    {
                        int registerIndex;
                        if (!samplerRegisterMap.TryGetValue(name, out registerIndex))
                        {
                            registerIndex = nextTextureRegister++;
                            samplerRegisterMap[name] = registerIndex;
                        }
                        SamplerMapping mapping = new SamplerMapping();
                        mapping.Name = name;
                        mapping.TextureRegister = registerIndex;
                        mapping.SamplerRegister = registerIndex;
                        samplers.Add(mapping);
                        textureDeclarations.Add(translateSamplerType(type) + " " + name + " : register(t" + registerIndex + ");");
                        samplerDeclarations.Add("SamplerState " + name + "_sampler : register(s" + registerIndex + ");");
                    }
                    else if (isMatrixType(type))
                    {
                        cbufferMembers.Add("row_major " + translateType(type) + " " + name + ";");
                        matrixNames.Add(name);
                    }
                    else
                    {
                        cbufferMembers.Add(translateType(type) + " " + name + ";");
                    }
                    continue;
                }

                // Skip the GLSL entry-point wrapper; its statements are emitted directly
                // inside the translated HLSL entry-point body.
                if (Regex.IsMatch(trimmed, @"^void\s+main\s*\("))
                {
                    Match singleLine = Regex.Match(trimmed, @"^\s*void\s+main\s*\(\s*\)\s*\{(.*)\}\s*;?\s*$", RegexOptions.Singleline);
                    if (singleLine.Success && singleLine.Groups[1].Value.Trim().Length > 0)
                    {
                        body.Add(singleLine.Groups[1].Value);
                    }
                    continue;
                }
                if (trimmed == "}" || trimmed == "{")
                {
                    continue;
                }

                // Local matrix variables participate in matrix-vector multiply
                // rewriting, so their names must be known to translateExpressions.
                Match localMatrixMatch = Regex.Match(trimmed, @"^(?:mat2|mat3|mat4)\s+([A-Za-z_][A-Za-z0-9_]*)");
                if (localMatrixMatch.Success)
                {
                    matrixNames.Add(localMatrixMatch.Groups[1].Value);
                }

                body.Add(line);
            }

            StringBuilder result = new StringBuilder();
            if (cbufferMembers.Count > 0)
            {
                result.Append("cbuffer G3DUniforms : register(b0)\n{\n");
                foreach (string member in cbufferMembers)
                {
                    result.Append("    ").Append(member).Append('\n');
                }
                result.Append("};\n");
            }
            foreach (string declaration in textureDeclarations)
            {
                result.Append(declaration).Append('\n');
            }
            foreach (string declaration in samplerDeclarations)
            {
                result.Append(declaration).Append('\n');
            }

            // Build the entry-point signature.  HLSL shader model 4 does not allow
            // mutable global variables, so attributes, varyings, and the fragment
            // color are declared as entry-point parameters with semantics.
            List<string> parameters = new List<string>();
            parameters.AddRange(attributeParams);
            parameters.AddRange(varyingParams);

            if (vertexStage)
            {
                result.Append("float4 main(");
                for (int i = 0; i < parameters.Count; ++i)
                {
                    if (i > 0) { result.Append(",\n    "); } else { result.Append('\n').Append("    "); }
                    result.Append(parameters[i]);
                }
                result.Append("\n) : SV_Position\n{\n    float4 gl_Position;\n");
            }
            else
            {
                if (hasPixelOut)
                {
                    parameters.Add("out " + pixelOutType + " " + pixelOutName + " : SV_Target");
                }
                result.Append("void main(");
                for (int i = 0; i < parameters.Count; ++i)
                {
                    if (i > 0) { result.Append(",\n    "); } else { result.Append('\n').Append("    "); }
                    result.Append(parameters[i]);
                }
                result.Append("\n)\n{\n");
                if (!hasPixelOut)
                {
                    result.Append("    float4 gl_FragColor;\n");
                }
            }

            foreach (string line in body)
            {
                result.Append("    ").Append(translateExpressions(line, matrixNames, samplers, vertexStage, attributeTypes)).Append('\n');
            }

            if (vertexStage)
            {
                result.Append("    return gl_Position;\n}\n");
            }
            else
            {
                result.Append("    return;\n}\n");
            }

            return result.ToString();
        }

        private static bool isMatrixType(string type)
        {
            return type == "mat2" || type == "mat3" || type == "mat4";
        }

        private static string translateType(string type)
        {
            switch (type)
            {
                case "float": return "float";
                case "int": return "int";
                case "vec2": return "float2";
                case "vec3": return "float3";
                case "vec4": return "float4";
                case "ivec2": return "int2";
                case "ivec3": return "int3";
                case "ivec4": return "int4";
                case "mat2": return "float2x2";
                case "mat3": return "float3x3";
                case "mat4": return "float4x4";
                default: throw new NotSupportedException("G3D::GlslToHlslTranslator does not support the type '" + type + "'.");
            }
        }

        private static string translateSamplerType(string type)
        {
            switch (type)
            {
                case "sampler1D": return "Texture1D";
                case "sampler2D": return "Texture2D";
                case "sampler3D": return "Texture3D";
                case "samplerCube": return "TextureCube";
                default: throw new NotSupportedException("G3D::GlslToHlslTranslator does not support the sampler type '" + type + "'.");
            }
        }

        private static string attributeSemantic(string name, int fallbackIndex)
        {
            switch (name)
            {
                case "position": return "POSITION";
                case "normal": return "NORMAL";
                case "color": return "COLOR";
                case "tangent": return "TANGENT";
                case "binormal": return "BINORMAL";
                case "texCoord0": return "TEXCOORD0";
                case "texCoord1": return "TEXCOORD1";
                case "texCoord2": return "TEXCOORD2";
                case "texCoord3": return "TEXCOORD3";
                default: return "TEXCOORD" + fallbackIndex;
            }
        }

        private static string varyingSemantic(string name)
        {
            // HLSL requires the pixel-stage input semantics to match the
            // vertex-stage output semantics.  GLSL already requires the varying
            // names to match between the two stages, so the name-based semantic
            // below is both correct and immune to declaration-order differences.
            return "VARY_" + name;
        }

        private static string translateExpressions(string line, List<string> matrixNames, List<SamplerMapping> samplers, bool vertexStage, Dictionary<string, string> attributeTypes)
        {
            string result = line;

            // Matrix constructors must be rewritten before the bare \bmat[34]\b
            // tokens are renamed to float[34]x[34], so that the constructor
            // pattern still matches the GLSL spelling.
            result = translateMatrixConstructors(result);

            result = Regex.Replace(result, @"\bvec2\b", "float2");
            result = Regex.Replace(result, @"\bvec3\b", "float3");
            result = Regex.Replace(result, @"\bvec4\b", "float4");
            result = Regex.Replace(result, @"\bivec2\b", "int2");
            result = Regex.Replace(result, @"\bivec3\b", "int3");
            result = Regex.Replace(result, @"\bivec4\b", "int4");
            result = Regex.Replace(result, @"\bmat2\b", "float2x2");
            result = Regex.Replace(result, @"\bmat3\b", "float3x3");
            result = Regex.Replace(result, @"\bmat4\b", "float4x4");

            if (!vertexStage && result.Contains("gl_FragCoord"))
            {
                throw new NotSupportedException("G3D::GlslToHlslTranslator does not support gl_FragCoord.");
            }

            if (result.Contains("ftransform"))
            {
                throw new NotSupportedException("G3D::GlslToHlslTranslator does not support ftransform().");
            }

            foreach (SamplerMapping sampler in samplers)
            {
                result = Regex.Replace(result, @"\btexture(?:2D|Cube|1D|3D)\s*\(\s*" + Regex.Escape(sampler.Name) + @"\s*,\s*([^()]*?)\s*\)", sampler.Name + ".Sample(" + sampler.Name + "_sampler, $1)");
            }

            result = Regex.Replace(result, @"\bmod\s*\(", "fmod(");
            result = replaceAtan(result);

            foreach (string matrixName in matrixNames)
            {
                string escaped = Regex.Escape(matrixName);
                result = Regex.Replace(result, @"\b" + escaped + @"\s*\*\s*(float[234]|vec[234])\s*\(([^()]*)\)", "mul(" + matrixName + ", $1($2))");
                result = Regex.Replace(result, @"\b" + escaped + @"\s*\*\s*([A-Za-z_][A-Za-z0-9_]*)", "mul(" + matrixName + ", $1)");
                result = Regex.Replace(result, @"\b([A-Za-z_][A-Za-z0-9_]*)\s*\*\s*" + escaped + @"\b", "mul($1, " + matrixName + ")");
            }

            result = translateBuiltInVariables(result, vertexStage, attributeTypes);

            return result;
        }

        private static string translateBuiltInVariables(string source, bool vertexStage, Dictionary<string, string> attributeTypes)
        {
            if (!vertexStage)
            {
                return source;
            }

            string result = source;
            result = Regex.Replace(result, @"\bgl_Vertex\b", vertexAttribute("position", "float4(position, 1.0)", "position", attributeTypes));
            result = Regex.Replace(result, @"\bgl_Normal\b", "normal");
            result = Regex.Replace(result, @"\bgl_Color\b", vertexAttribute("color", "float4(color, 1.0)", "color", attributeTypes));
            result = Regex.Replace(result, @"\bgl_MultiTexCoord0\b", texCoordAttribute("texCoord0", attributeTypes));
            result = Regex.Replace(result, @"\bgl_MultiTexCoord1\b", texCoordAttribute("texCoord1", attributeTypes));
            result = Regex.Replace(result, @"\bgl_MultiTexCoord2\b", texCoordAttribute("texCoord2", attributeTypes));
            result = Regex.Replace(result, @"\bgl_MultiTexCoord3\b", texCoordAttribute("texCoord3", attributeTypes));

            if (result.Contains("gl_") && !result.Contains("gl_Position"))
            {
                throw new NotSupportedException("G3D::GlslToHlslTranslator does not support the GLSL built-in variable used by this shader.");
            }
            return result;
        }

        private static string vertexAttribute(string name, string vec3Expression, string vec4Expression, Dictionary<string, string> attributeTypes)
        {
            string type;
            if (attributeTypes.TryGetValue(name, out type))
            {
                if (type == "vec4")
                {
                    return vec4Expression;
                }
            }
            return vec3Expression;
        }

        private static string texCoordAttribute(string name, Dictionary<string, string> attributeTypes)
        {
            string type;
            if (attributeTypes.TryGetValue(name, out type))
            {
                if (type == "vec4")
                {
                    return name;
                }
                if (type == "vec3")
                {
                    return "float4(" + name + ", 1.0)";
                }
            }
            return "float4(" + name + ", 0.0, 1.0)";
        }

        private static string translateMatrixConstructors(string source)
        {
            return matrixConstructorPattern.Replace(source, delegate(Match match)
            {
                string size = match.Groups[1].Value;
                string[] values = match.Groups[2].Value.Split(',');
                int count = int.Parse(size) * int.Parse(size);
                if (values.Length != count)
                {
                    throw new NotSupportedException("G3D::GlslToHlslTranslator only supports square matrix constructors.");
                }

                List<string> numbers = new List<string>();
                foreach (string value in values)
                {
                    numbers.Add(value.Trim());
                }

                if (size == "3")
                {
                    return "float3x3(float3(" + numbers[0] + ", " + numbers[3] + ", " + numbers[6] + "), " +
                           "float3(" + numbers[1] + ", " + numbers[4] + ", " + numbers[7] + "), " +
                           "float3(" + numbers[2] + ", " + numbers[5] + ", " + numbers[8] + "))";
                }

                return "float4x4(" + string.Join(", ", numbers.ToArray()) + ")";
            });
        }

        private static string replaceAtan(string source)
        {
            int index = source.IndexOf("atan(", StringComparison.Ordinal);
            while (index >= 0)
            {
                int depth = 0;
                int commas = 0;
                int end = -1;
                for (int i = index + 4; i < source.Length; ++i)
                {
                    char c = source[i];
                    if (c == '(')
                    {
                        depth++;
                    }
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            end = i;
                            break;
                        }
                    }
                    else if (c == ',' && depth == 1)
                    {
                        commas++;
                    }
                }
                if (end < 0)
                {
                    break;
                }
                if (commas == 1)
                {
                    source = source.Substring(0, index) + "atan2" + source.Substring(index + 4);
                    index = source.IndexOf("atan2(", index + 6, StringComparison.Ordinal);
                    continue;
                }
                index = source.IndexOf("atan(", end, StringComparison.Ordinal);
            }
            return source;
        }
    }
}
