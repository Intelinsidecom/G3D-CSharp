/**
  @file Shader.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2004-04-25
  @edited  2006-05-04
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace G3D
{

    /**
      A compatible vertex and pixel shader.  Used internally by G3D::Shader; see that
      class for more information.

      Only newer graphics cards with recent drivers (e.g. GeForceFX cards with driver version 57 or greater)
      support this API.  Use the VertexAndPixelShader::fullySupported method to determine at run-time
      if your graphics card is compatible.

      For purposes of shading, a "pixel" is technically a "fragment" in OpenGL terminology.

      Pixel and vertex shaders are loaded as text strings written in
      <A HREF="http://developer.3dlabs.com/openGL2/specs/GLSLangSpec.Full.1.10.59.pdf">GLSL</A>, the high-level
      OpenGL shading language.  Object shaders are written in C++ by subclassing ObjectShader.

      Typically, the G3D::Shader sets up constants like the object-space position
      of the light source and the object-to-world matrix.  The vertex shader transforms
      input vertices to homogeneous clip space and computes values that are interpolated
      across the surface of a triangle (e.g. reflection vector).  The pixel shader
      computes the final color of a pixel (it does not perform alpha-blending, however).

      Multiple VertexAndPixelShaders may share object, vertex, and pixel shaders.

      Uniform variables that begin with 'gl_' are ignored because they are assumed to
      be GL built-ins.

      @cite http://oss.sgi.com/projects/ogl-sample/registry/ARB/shader_objects.txt
      @cite http://oss.sgi.com/projects/ogl-sample/registry/ARB/vertex_shader.txt

      <B>BETA API</B>
      This API is subject to change.
     */
    public class VertexAndPixelShader : ReferenceCountedObject
    {
        /** Thrown by validateArgList */
        public sealed class ArgumentError : Exception
        {
            public string message;

            public ArgumentError(string message) : base(message)
            {
                this.message = message;
            }
        }

        public sealed class UniformDeclaration
        {
            /** If true, this variable is declared but unused */
            public bool dummy;

            /** Register location */
            public int location;

            /** Name of the variable.  May include [] and . (e.g.
                "foo[1].normal")*/
            public string name;

            /** OpenGL type of the variable (e.g. GL_INT) */
            public ShaderProgramType type;

            /** Unknown... appears to always be 1 */
            public int size;

            /**
             Index of the texture unit in which this value
             is stored.  -1 for uniforms that are not textures. */
            public int textureUnit;
        }

        /**
         Bindings of values to uniform variables for a VertexAndPixelShader.
         Be aware that
         the uniform namespace is global across the pixel and vertex shader.

         GL_BOOL_ARB and GL_INT_ARB-based values are coerced to floats
         automatically by the arg list.
         */
        public sealed class ArgList
        {
            private readonly Dictionary<string, object> arguments = new Dictionary<string, object>();

            public void set(string var, Texture value) { arguments[var] = value; }
            public void set(string var, CoordinateFrame value) { arguments[var] = value; }
            public void set(string var, Matrix4 value) { arguments[var] = value; }
            public void set(string var, Matrix3 value) { arguments[var] = value; }
            public void set(string var, Color4 value) { arguments[var] = value; }
            public void set(string var, Color3 value) { arguments[var] = value; }
            public void set(string var, Vector4 value) { arguments[var] = value; }
            public void set(string var, Vector3 value) { arguments[var] = value; }
            public void set(string var, Vector2 value) { arguments[var] = value; }
            public void set(string var, float value) { arguments[var] = value; }
            public void set(string var, int value) { arguments[var] = value; }

            private static string arrayElementName(string arrayName, int index)
            {
                return Format.format("%s[%d]", arrayName, index);
            }

            /**
             GLSL does not natively support arrays and structs in the uniform binding API.  Instead, each
             element of an array is treated as a separate element.  This method expands out to setting
             each element of an array.  You can instead set them using <CODE>args.set("arry[3]", myVal)</CODE>.
             Likewise for structs, <CODE>args.set("str.foo.bar", myVal)</CODE>.
             */
            public void set(string arrayName, float[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, int[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Vector2[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Vector3[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Vector4[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Color3[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Color4[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Matrix4[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, CoordinateFrame[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Texture[] arrayVal)
            {
                for (int i = 0; i < arrayVal.Length; ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<float> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<int> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<Vector2> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<Vector3> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<Vector4> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<Color3> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<Color4> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<Matrix4> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<CoordinateFrame> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void set(string arrayName, Array<Texture> arrayVal)
            {
                for (int i = 0; i < arrayVal.size(); ++i) { set(arrayElementName(arrayName, i), arrayVal[i]); }
            }

            public void clear() { arguments.Clear(); }

            internal bool contains(string name) { return arguments.ContainsKey(name); }
            internal int size() { return arguments.Count; }
            internal IEnumerable<KeyValuePair<string, object>> getArguments() { return arguments; }
            internal object get(string name)
            {
                object value;
                return arguments.TryGetValue(name, out value) ? value : null;
            }
        }

        private readonly string name;
        private readonly ShaderProgramInfo programInfo;
        internal readonly string vertexSource;
        internal readonly string pixelSource;
        private readonly List<UniformDeclaration> uniformArray = new List<UniformDeclaration>();
        private readonly HashSet<string> uniformNames = new HashSet<string>();
        private object backendHandle;

        /** argument for output on subclasses */
        protected static string ignore = "";

        protected VertexAndPixelShader(string shaderName, string vertexCode, string pixelCode, UseG3DUniforms useUniforms)
            : this(vertexCode, shaderName, false, pixelCode, "", false, false, useUniforms)
        {
        }

        protected VertexAndPixelShader(string vsCode, string vsFilename, bool vsFromFile,
                                       string psCode, string psFilename, bool psFromFile,
                                       bool debug, UseG3DUniforms useUniforms)
        {
            string vs = vsCode == null ? "" : vsCode;
            string ps = psCode == null ? "" : psCode;

            if (vsFromFile && vsFilename != null && vsFilename != "")
            {
                vs = File.ReadAllText(vsFilename);
            }

            if (psFromFile && psFilename != null && psFilename != "")
            {
                ps = File.ReadAllText(psFilename);
            }

            if (vsFilename != null && vsFilename != "" && psFilename != null && psFilename != "")
            {
                name = vsFilename + "/" + psFilename;
            }
            else if (vsFilename != null && vsFilename != "")
            {
                name = vsFilename;
            }
            else
            {
                name = "shader";
            }

            vertexSource = vs;
            pixelSource = ps;

            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            if (!backend.SupportsShaderLanguage(GraphicsShaderLanguage.GLSL) && !backend.SupportsShaderLanguage(GraphicsShaderLanguage.HLSL))
            {
                throw new NotSupportedException("The active graphics backend does not support GLSL or HLSL shader programs.");
            }

            string transformedVertex = transformSource(vs, useUniforms);
            string transformedPixel = transformSource(ps, useUniforms);

            programInfo = backend.CompileShaderProgram(GraphicsShaderLanguage.GLSL, transformedVertex, transformedPixel);
            backendHandle = programInfo.ProgramID;

            if (programInfo.Success)
            {
                computeUniformArray();
                // note that the extra uniforms are computed from the original code,
                // not from the code that has the g3d uniforms prepended.
                addUniformsFromCode(vs);
                addUniformsFromCode(ps);

                // Add all uniforms to the name list
                for (int i = uniformArray.Count - 1; i >= 0; --i)
                {
                    uniformNames.Add(uniformArray[i].name);
                }
            }

            if (debug && !ok())
            {
                throw new InvalidOperationException(messages());
            }
        }

        /**
         To use the fixed function pipeline for part of the
         shader, pass an empty string.
         */
        public static VertexAndPixelShader fromStrings(string vertexShader, string pixelShader)
        {
            return fromStrings(vertexShader, pixelShader, UseG3DUniforms.DO_NOT_DEFINE_G3D_UNIFORMS, false);
        }

        public static VertexAndPixelShader fromStrings(string vertexShader, string pixelShader, UseG3DUniforms useUniforms)
        {
            return fromStrings(vertexShader, pixelShader, useUniforms, false);
        }

        public static VertexAndPixelShader fromStrings(string vertexShader, string pixelShader, UseG3DUniforms useUniforms, bool debugErrors)
        {
            return new VertexAndPixelShader(vertexShader, "", false, pixelShader, "", false, debugErrors, useUniforms);
        }

        public static VertexAndPixelShader fromStrings(string vertexShaderName, string vertexShader, string pixelShaderName, string pixelShader)
        {
            return fromStrings(vertexShaderName, vertexShader, pixelShaderName, pixelShader, UseG3DUniforms.DO_NOT_DEFINE_G3D_UNIFORMS, false);
        }

        public static VertexAndPixelShader fromStrings(string vertexShaderName, string vertexShader, string pixelShaderName, string pixelShader, UseG3DUniforms useUniforms)
        {
            return fromStrings(vertexShaderName, vertexShader, pixelShaderName, pixelShader, useUniforms, false);
        }

        public static VertexAndPixelShader fromStrings(string vertexShaderName, string vertexShader, string pixelShaderName, string pixelShader, UseG3DUniforms useUniforms, bool debugErrors)
        {
            return new VertexAndPixelShader(vertexShader, vertexShaderName, false, pixelShader, pixelShaderName, false, debugErrors, useUniforms);
        }

        /**
         To use the fixed function pipeline for part of the
         shader, pass an empty string.

         @param debugErrors If true, a debugging dialog will
            appear when there are syntax errors in the shaders.
            If false, failures will occur silently; check
            VertexAndPixelShader::ok() to see if the files
            compiled correctly.
         */
        public static VertexAndPixelShader fromFiles(string vertexShader, string pixelShader)
        {
            return fromFiles(vertexShader, pixelShader, UseG3DUniforms.DO_NOT_DEFINE_G3D_UNIFORMS, false);
        }

        public static VertexAndPixelShader fromFiles(string vertexShader, string pixelShader, UseG3DUniforms useUniforms)
        {
            return fromFiles(vertexShader, pixelShader, useUniforms, false);
        }

        public static VertexAndPixelShader fromFiles(string vertexShader, string pixelShader, UseG3DUniforms useUniforms, bool debugErrors)
        {
            return new VertexAndPixelShader("", vertexShader, true, "", pixelShader, true, debugErrors, useUniforms);
        }

        /**
         Replaces all instances of
           <code>"g3d_sampler2DSize(name)"</code> with
           <code>"     (g3d_sz2D_name.xy)"</code> and

           <code>"g3d_sampler2DInvSize(name)"</code> with
           <code>"        (g3d_sz2D_name.zw)"</code>

          Note that both replacements will leave column numbers the same in error messages.  The
          <code>()</code> wrapper ensures that <code>.xy</code> fields are accessible using
          normal syntax off the result; it is the same as the standard practice of wrapping macros
          in parentheses.

          and adds "uniform vec4 g3d_sz2D_name;" to the uniform string.

          Called from init.
         */
        private static string replaceG3DSize(string code, List<string> newUniforms)
        {
            // Ensure that uniforms are only declared once
            string funcName = "g3d_sampler2DSize(";
            string prefix = "     (";
            string postfix = ".xy)";

            for (int count = 0; count < 2; ++count)
            {
                int last = 0;
                while (true)
                {
                    int next = code.IndexOf(funcName, last, StringComparison.Ordinal);
                    if (next < 0)
                    {
                        break;
                    }

                    int nameStart = next + funcName.Length;
                    int nameEnd = code.IndexOf(')', nameStart);

                    if (nameEnd < 0)
                    {
                        throw new InvalidOperationException("Could not find the end of " + funcName + ") expression");
                    }

                    string samplerName = code.Substring(nameStart, nameEnd - nameStart);
                    string decoratedName = "g3d_sz2D_" + samplerName;
                    string replacement = prefix + decoratedName + postfix;

                    code = code.Substring(0, next) + replacement + code.Substring(nameEnd + 1);

                    if (!newUniforms.Contains(decoratedName))
                    {
                        newUniforms.Add(decoratedName);
                    }

                    last = next + replacement.Length;

                } // Iterating through all instances

                // On the second iteration, replace g3d_invSize.
                funcName = "g3d_sampler2DInvSize(";
                prefix = "        (";
                postfix = ".zw)";
            }

            return code;
        }

        private static string transformSource(string source, UseG3DUniforms useUniforms)
        {
            if (source == null)
            {
                return "";
            }
            if (useUniforms == UseG3DUniforms.DO_NOT_DEFINE_G3D_UNIFORMS)
            {
                return source;
            }

            List<string> sizeUniforms = new List<string>();
            string transformed = replaceG3DSize(source, sizeUniforms);

            StringBuilder declarations = new StringBuilder();
            declarations.Append("uniform mat4 g3d_WorldToObjectMatrix;\n");
            declarations.Append("uniform mat4 g3d_ObjectToWorldMatrix;\n");
            declarations.Append("uniform mat4 g3d_WorldToCameraMatrix;\n");
            declarations.Append("uniform mat4 g3d_CameraToWorldMatrix;\n");
            declarations.Append("uniform int g3d_NumLights;\n");
            declarations.Append("uniform int g3d_NumTextures;\n");
            declarations.Append("uniform vec4 g3d_ObjectLight0;\n");
            for (int i = 0; i < sizeUniforms.Count; ++i)
            {
                declarations.Append("uniform vec4 " + sizeUniforms[i] + ";\n");
            }

            string trimmed = transformed.TrimStart();
            if (trimmed.StartsWith("#version"))
            {
                int endOfLine = trimmed.IndexOf('\n');
                if (endOfLine < 0)
                {
                    return trimmed + "\n" + declarations.ToString();
                }
                return trimmed.Substring(0, endOfLine + 1) + declarations.ToString() + trimmed.Substring(endOfLine + 1);
            }
            return declarations.ToString() + transformed;
        }

        /** Computes the uniformArray from the current program object. */
        private void computeUniformArray()
        {
            uniformArray.Clear();

            for (int i = 0; i < programInfo.Uniforms.Count; ++i)
            {
                ShaderUniformInfo source = programInfo.Uniforms[i];
                UniformDeclaration decl = new UniformDeclaration();
                decl.dummy = source.Location < 0;
                decl.location = source.Location;
                decl.name = source.Name;
                decl.size = source.Size;
                decl.type = source.Type;
                decl.textureUnit = source.TextureUnit;
                uniformArray.Add(decl);
            }
        }

        /** Converts a type name to a GL enum */
        private static ShaderProgramType toGLType(string s)
        {
            if (s == "float") {
                return ShaderProgramType.Float;
            } else if (s == "vec2") {
                return ShaderProgramType.Float2;
            } else if (s == "vec3") {
                return ShaderProgramType.Float3;
            } else if (s == "vec4") {
                return ShaderProgramType.Float4;

            } else if (s == "int") {
                return ShaderProgramType.Int;

            } else if (s == "bool") {
                return ShaderProgramType.Bool;

            } else if (s == "mat2") {
                return ShaderProgramType.Matrix2;
            } else if (s == "mat3") {
                return ShaderProgramType.Matrix3;
            } else if (s == "mat4") {
                return ShaderProgramType.Matrix4;

            } else if (s == "sampler1D") {
                return ShaderProgramType.Sampler1D;
            } else if (s == "sampler2D") {
                return ShaderProgramType.Sampler2D;
            } else if (s == "sampler3D") {
                return ShaderProgramType.Sampler3D;
            } else if (s == "samplerCube") {
                return ShaderProgramType.SamplerCube;
            } else if (s == "sampler2DRect") {
                return ShaderProgramType.SamplerRectangle;
            } else {
                throw new ArgumentException("Unknown type in shader: " + s);
            }
        }

        /**
         Finds any uniform variables in the code that are not already in
         the uniform array that OpenGL returned and adds them to that array.
         This causes VertexAndPixelShader to surpress warnings about
         setting variables that have been compiled away--those warnings
         are annoying when temporarily commenting out code.
         */
        private void addUniformsFromCode(string code)
        {
            if (code == null || code.Length == 0)
            {
                return;
            }

            TextInput ti = new TextInput(TextInput.FS.FROM_STRING, code);

            while (ti.hasMore()) {
                if ((ti.peek().type == Token.Type.SYMBOL) && (ti.peek().str == "uniform")) {
                    // Read the definition
                    ti.readSymbol("uniform");

                    // Maybe read "const"
                    if ((ti.peek().type == Token.Type.SYMBOL) && (ti.peek().str == "const")) {
                        ti.readSymbol("const");
                    }

                    // Read the type
                    ShaderProgramType type = toGLType(ti.readSymbol());

                    // Read the name
                    string uniformName = ti.readSymbol();

                    if ((ti.peek().type == Token.Type.SYMBOL) && (ti.peek().str == "[")) {
                        ti.readSymbol("[");
                        ti.readNumber();
                        ti.readSymbol("]");
                    }

                    // Read the semi-colon
                    ti.readSymbol(";");

                    // See if this variable is already declared.
                    bool found = false;

                    for (int i = 0; i < uniformArray.Count; ++i) {
                        if (uniformArray[i].name == uniformName) {
                            found = true;
                            break;
                        }
                    }

                    if (! found) {
                        // Add the definition
                        UniformDeclaration decl = new UniformDeclaration();

                        decl.dummy = true;
                        decl.location = -1;
                        decl.name = uniformName;
                        decl.size = 1;
                        decl.type = type;

                        // Don't allocate texture units for unused variables
                        decl.textureUnit = -1;

                        uniformArray.Add(decl);
                    }

                } else {
                    // Consume the token
                    ti.read();
                }
            }
        }

        /** Returns true for types that are textures (e.g., GL_TEXTURE_2D) */
        protected static bool isSamplerType(ShaderProgramType e)
        {
            return ShaderProgram.isSamplerType(e);
        }

        /** Converts from int and bool types to float types (e.g. GL_INT_VEC2_ARB -> GL_FLOAT_VEC2_ARB).
            Other types are left unmodified.*/
        protected static ShaderProgramType canonicalType(ShaderProgramType e)
        {
            return ShaderProgram.canonicalType(e);
        }

        private static ShaderProgramType typeOfValue(object value)
        {
            if (value is Texture) {
                Texture texture = (Texture)value;
                if (texture.getDimension() == Texture.Dimension.DIM_2D_RECT) {
                    return ShaderProgramType.SamplerRectangle;
                } else if (texture.getDimension() == Texture.Dimension.DIM_CUBE_MAP ||
                           texture.getDimension() == Texture.Dimension.DIM_CUBE_MAP_NPOT) {
                    return ShaderProgramType.SamplerCube;
                }
                return ShaderProgramType.Sampler2D;
            } else if (value is CoordinateFrame) {
                return ShaderProgramType.Matrix4;
            } else if (value is Matrix4) {
                return ShaderProgramType.Matrix4;
            } else if (value is Matrix3) {
                return ShaderProgramType.Matrix3;
            } else if (value is Vector4) {
                return ShaderProgramType.Float4;
            } else if (value is Color4) {
                return ShaderProgramType.Float4;
            } else if (value is Vector3) {
                return ShaderProgramType.Float3;
            } else if (value is Color3) {
                return ShaderProgramType.Float3;
            } else if (value is Vector2) {
                return ShaderProgramType.Float2;
            } else if (value is int) {
                return ShaderProgramType.Int;
            }
            return ShaderProgramType.Float;
        }

        /**
         Returns GLCaps::supports_GL_ARB_shader_objects() &&
            GLCaps::supports_GL_ARB_shading_language_100() &&
            GLCaps::supports_GL_ARB_fragment_shader() &&
            GLCaps::supports_GL_ARB_vertex_shader()
         */
        public static bool fullySupported()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                return false;
            }
            return backend.SupportsShaderLanguage(GraphicsShaderLanguage.GLSL) ||
                   backend.SupportsShaderLanguage(GraphicsShaderLanguage.HLSL);
        }

        /**
         Returns true if compilation and loading succeeded.  If they failed,
         check the message string.*/
        public bool ok()
        {
            return programInfo.Success;
        }

        /**
         All compilation and linking messages, with additional formatting.
         For details about a specific part of the process, see
         vertexErrors, pixelErrors, and linkErrors.
         */
        public string messages()
        {
            StringBuilder result = new StringBuilder();
            result.Append("Compiling Vertex Shader " + name + "\n");
            result.Append(programInfo.VertexErrors);
            result.Append("\n\n");
            result.Append("Compiling Pixel Shader " + name + "\n");
            result.Append(programInfo.PixelErrors);
            result.Append("\n\n");
            result.Append("Linking\n");
            result.Append(programInfo.LinkErrors);
            result.Append("\n");
            return result.ToString();
        }

        public string vertexErrors() { return programInfo.VertexErrors; }
        public string pixelErrors() { return programInfo.PixelErrors; }
        public string linkErrors() { return programInfo.LinkErrors; }

        /** The underlying OpenGL object for the vertex/pixel shader pair. */
        public int glProgramObject() { return programInfo.ProgramID; }

        public int numArgs() { return uniformArray.Count; }

        public string getName() { return name; }

        /**
         Returns information about one of the arguments expected
            by this VertexAndPixelShader.  There are VertexAndPixelShader::numArgs()
            total.*/
        public UniformDeclaration arg(int i)
        {
            if (i < 0 || i >= uniformArray.Count)
            {
                throw new ArgumentOutOfRangeException("i");
            }
            return uniformArray[i];
        }

        internal bool hasUniform(string uniformName)
        {
            return uniformNames.Contains(uniformName);
        }

        /**
         Checks the actual values of uniform variables against those
            expected by the program.
            If one of the arguments does not match, an ArgumentError
            exception is thrown.
        */
        public void validateArgList(ArgList args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            int numVariables = 0;

            // Iterate through formal bindings
            for (int u = 0; u < uniformArray.Count; ++u) {
                UniformDeclaration decl = uniformArray[u];

                // Only count variables that need to be set in code
                if (! decl.dummy) {
                    ++numVariables;
                }

                bool declared = false;
                bool performTypeCheck = true;

                if (decl.name.StartsWith("g3d_sz2D_")) {
                    // This is the autogenerated dimensions of a texture.
                    string textureName  = decl.name.Substring(9, decl.name.Length - 9);
                    declared = args.contains(textureName);
                    performTypeCheck = false;

                    if (! declared && ! decl.dummy) {
                        throw new ArgumentError(
                            Format.format("No value provided for VertexAndPixelShader uniform variable %s.",
                                textureName));
                    }

                } else {

                    declared = args.contains(decl.name);

                    if (! declared && ! decl.dummy) {
                        throw new ArgumentError(
                            Format.format("No value provided for VertexAndPixelShader uniform variable %s of type %s.",
                                decl.name, ShaderProgram.typeToString(decl.type)));
                    }
                }

                if (declared && performTypeCheck) {
                    object value = args.get(decl.name);

                    // check the type
                    if (value != null && canonicalType(decl.type) != canonicalType(typeOfValue(value))) {
                        string v1 = ShaderProgram.typeToString(decl.type);
                        string v2 = ShaderProgram.typeToString(typeOfValue(value));
                        string v1c = ShaderProgram.typeToString(canonicalType(decl.type));
                        string v2c = ShaderProgram.typeToString(canonicalType(typeOfValue(value)));
                        throw new ArgumentError(
                        Format.format("Variable %s was declared as type %s (%s) and the value provided at runtime had type %s (%s).",
                                decl.name, v1, v1c, v2, v2c));
                    }
                }

            }

            if (numVariables < args.size()) {
                // Some variables were unused.  Figure out which they were.
                foreach (KeyValuePair<string, object> arg in args.getArguments()) {
                    // See if this arg was in the formal binding list

                    bool foundArgument = false;

                    for (int u = 0; u < uniformArray.Count; ++u) {
                        if (uniformArray[u].name == arg.Key) {
                            foundArgument = true;
                            break;
                        }
                    }

                    if (! foundArgument) {
                        throw new ArgumentError(
                        "Extra VertexAndPixelShader uniform variable provided at runtime: " + arg.Key + ".");
                    }
                }
            }
        }

        /**
           Makes renderDevice calls to bind this argument list.
           Calls validateArgList.
         */
        public void bindArgList(RenderDevice renderDevice, ArgList args)
        {
            if (renderDevice == null)
            {
                throw new ArgumentNullException("renderDevice");
            }

            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }

            backend.UseShaderProgram(programInfo.ProgramID);

            validateArgList(args);

            // Iterate through the formal parameter list
            for (int u = 0; u < uniformArray.Count; ++u) {
                UniformDeclaration decl  = uniformArray[u];

                if (decl.dummy) {
                    // Don't set this variable; it is unused.
                    continue;
                }

                if (decl.name.StartsWith("g3d_sz2D_")) {
                    // This is the autogenerated dimensions of a texture.

                    string textureName  = decl.name.Substring(9, decl.name.Length - 9);

                    Texture texture = (Texture)args.get(textureName);

                    // Compute the vector of size and inverse size
                    float w = texture.texelWidth();
                    float h = texture.texelHeight();
                    Vector4 v = new Vector4(w, h, 1.0f / w, 1.0f / h);

                    backend.Uniform4fv(decl.location, v);

                } else {

                    // Normal user defined variable

                    object value = args.get(decl.name);

                    bindValue(renderDevice, backend, programInfo.ProgramID, decl, value);
                }
            }
        }

        private static void bindValue(RenderDevice renderDevice, IGraphicsBackend backend, int programID, UniformDeclaration decl, object value)
        {
            if (value is Texture)
            {
                Texture texture = (Texture)value;
                int textureID = texture.getBackendTextureID(backend);
                if (textureID == 0)
                {
                    textureID = renderDevice.allocateTexture(texture);
                }
                backend.SetShaderTexture(programID, decl.textureUnit, textureID, texture.getDimension());
                backend.UniformTexture(decl.location, decl.textureUnit);
                if (decl.textureUnit >= 0)
                {
                    renderDevice.setTexture((uint)decl.textureUnit, texture);
                }
            }
            else if (value is CoordinateFrame)
            {
                backend.UniformMatrix4fv(decl.location, ((CoordinateFrame)value).toMatrix4());
            }
            else if (value is Matrix4)
            {
                backend.UniformMatrix4fv(decl.location, (Matrix4)value);
            }
            else if (value is Matrix3)
            {
                backend.UniformMatrix3fv(decl.location, (Matrix3)value);
            }
            else if (value is Vector2)
            {
                backend.Uniform2fv(decl.location, (Vector2)value);
            }
            else if (value is Vector3)
            {
                backend.Uniform3fv(decl.location, (Vector3)value);
            }
            else if (value is Vector4)
            {
                backend.Uniform4fv(decl.location, (Vector4)value);
            }
            else if (value is Color3)
            {
                Color3 color = (Color3)value;
                backend.Uniform3fv(decl.location, new Vector3(color.r, color.g, color.b));
            }
            else if (value is Color4)
            {
                Color4 color = (Color4)value;
                backend.Uniform4fv(decl.location, new Vector4(color.r, color.g, color.b, color.a));
            }
            else if (value is int)
            {
                backend.Uniform1i(decl.location, (int)value);
            }
            else
            {
                backend.Uniform1f(decl.location, (float)value);
            }
        }

        protected override void dispose()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend != null && programInfo != null && programInfo.ProgramID != 0)
            {
                backend.DeleteShaderProgram(programInfo.ProgramID);
                programInfo.ProgramID = 0;
                backendHandle = null;
            }
            base.dispose();
        }

        internal object getBackendHandle()
        {
            return backendHandle;
        }

        internal void setBackendHandle(object handle)
        {
            backendHandle = handle;
        }
    }
}
