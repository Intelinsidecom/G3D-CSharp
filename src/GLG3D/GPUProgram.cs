/**
  @file GPUProgram.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2003-04-13
  @edited  2006-02-25
*/

using System;
using System.Collections.Generic;
using System.IO;
using G3D;

namespace G3D
{

    /**
      Reference-counted smart pointer to GPUProgram.
    */
    // typedef ReferenceCountedPointer<class GPUProgram> GPUProgramRef;

    /**
      Base class for VertexProgram and PixelProgram

      If you use GPUProgramRef instead of GPUProgram*, the texture memory will be
      garbage collected.

      If an error is encountered inside a shader in debug mode, that error is printed to the
      debug window (under MSVC) and the programmer can fix the error and reload the
      shader without reloading the program.

      Subclasses of GPUProgram (VertexProgram and PixelProgram) automatically recognize
      the output of NVIDIA's Cg compiler and will load constants from comments of the
      form:

      <CODE>    # const c[N] = V0 V1 V2 V3</CODE>

      Likewise, variable bindings are detected in Cg output. Create an ArgList mapping
      variable names to the actual parameters and provide it when you call
      RenderDevice::setVertexProgram(). Alternately, you can track matrices and load
      variables yourself using the NVIDIA or ARB APIs.

      For example:

      <PRE>
        glTrackMatrixNV(GL_VERTEX_PROGRAM_NV, 0, GL_MODELVIEW_PROJECTION_NV, GL_IDENTITY_NV);
        glTrackMatrixNV(GL_VERTEX_PROGRAM_NV, 4, GL_MODELVIEW, GL_INVERSE_NV);
        glProgramParameter4fvNV(GL_VERTEX_PROGRAM_NV, 8, Vector4(myVariable,0,0,0));
      </PRE>
      @deprecated
      Use the VertexShader and PixelShader classes
     */
    public abstract class GPUProgram : ReferenceCountedObject
    {
        /** Internal use only */
        public enum Type
        {
            FLOAT4X4, FLOAT3X3, FLOAT2X2,
            FLOAT4, FLOAT3, FLOAT2, FLOAT1,
            SAMPLER1D, SAMPLER2D, SAMPLER3D, SAMPLERCUBE, SAMPLERRECT
        }

        /** Internal use only */
        public enum Source { VARIABLE, CONSTANT }

        /**
          Which extension set to use.
         */
        protected enum Extension { ARB, NVIDIA }

        private const int GL_NONE = 0;
        private const int GL_VERTEX_PROGRAM_ARB = 0x8620;
        private const int GL_FRAGMENT_PROGRAM_ARB = 0x8804;
        private const int GL_VERTEX_PROGRAM_NV = 0x8621;
        private const int GL_FRAGMENT_PROGRAM_NV = 0x8808;

        /** Internal use only
          Converts a string name (e.g. "float4") to a Cg type.
          Returns false if it can't match the type.
        */
        public static bool CgType(string s, out Type t)
        {
            t = Type.FLOAT1;

            if (s == "float4x4") {
                t = Type.FLOAT4X4;
            } else if (s == "float3x3") {
                t = Type.FLOAT3X3;
            } else if (s == "float2x2") {
                t = Type.FLOAT2X2;
            } else if (s == "float4") {
                t = Type.FLOAT4;
            } else if (s == "float3") {
                t = Type.FLOAT3;
            } else if (s == "float2") {
                t = Type.FLOAT2;
            } else if (s == "float1") {
                t = Type.FLOAT1;
            } else if (s == "float") {
                t = Type.FLOAT1;
            } else if (s == "sampler1D") {
                t = Type.SAMPLER1D;
            } else if (s == "sampler2D") {
                t = Type.SAMPLER2D;
            } else if (s == "sampler3D") {
                t = Type.SAMPLER3D;
            } else if (s == "samplerCUBE") {
                t = Type.SAMPLERCUBE;
            } else if (s == "samplerRECT") {
                t = Type.SAMPLERRECT;
            } else {
                return false;
            }

            return true;
        }

        /** Internal use only.  For printing error messages. */
        public static string toString(Type t)
        {
            switch (t) {
            case Type.FLOAT4X4:
                return "float4x4";
            case Type.FLOAT3X3:
                return "float3x3";
            case Type.FLOAT2X2:
                return "float2x2";
            case Type.FLOAT4:
                return "float4";
            case Type.FLOAT3:
                return "float3";
            case Type.FLOAT2:
                return "float2";
            case Type.FLOAT1:
                return "float1";
            case Type.SAMPLER1D:
                return "sampler1D";
            case Type.SAMPLER2D:
                return "sampler2D";
            case Type.SAMPLER3D:
                return "sampler3D";
            case Type.SAMPLERCUBE:
                return "samplerCUBE";
            case Type.SAMPLERRECT:
                return "samplerRECT";
            }

            return "** unknown type**";
        }

        /**
          Manages the constants in a vertex or fragment program produced by Cg, where
          the header has comments of the form:

           # const c[N] = V0 V1 V2 V3

           where N, V0, V1, V2, and V3 are numbers.
         */
        private class BindingTable
        {
            /**
              A constant, variable, or matrix binding for a vertex program.
             */
            public class Binding
            {
                /**
                  When the slot is UNASSIGNED, the variable
                  was declared but never used; it has no slot.
                 */
                public const int UNASSIGNED = -1;

                /**
                  Variable/constant name
                 */
                public string         name;

                /**
                  Which const register receives this value.
                 */
                public int             slot;

                public Type            type;

                public Source          source;

                /** Used by constants only */
                public Vector4         vector;
            }

            private List<Binding> bindingArray = new List<Binding>();

            private static bool symbolMatch(Token t, string s)
            {
                return (t.type == Token.Type.SYMBOL) && (t.str == s);
            }

            /**
              Returns true if it is able to consume the next symbol, which must match s.
             */
            private static bool consumeSymbol(TextInput ti, string s)
            {
                Token t = ti.peek();
                if (symbolMatch(t, s))
                {
                    ti.readSymbol(s);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /** Called from parse() */
            private void parseVariable(TextInput ti)
            {
                string name;

                // #var float4 osLight :  : c[4] : 1 : 1
                // #var float3 vin.v0 : $vin.POSITION : ATTR0 : 2 : 1

                Token t = ti.peek();
                if (t.type != Token.Type.SYMBOL)
                {
                    goto abort;
                }
                // get the binding's type
                ti.readSymbol();
                Type type;

                if (!CgType(t.str, out type))
                {
                    Debug.alwaysAssertM(false, "Unsupported type: \"" + t.str + "\"");
                }

                t = ti.peek();
                if (t.type != Token.Type.SYMBOL)
                {
                    goto abort;
                }
                // read the binding name
                name = ti.readSymbol();

                if (!consumeSymbol(ti, ":"))
                {
                    goto abort;
                }

                // see if it is the vertex or a constant register
                t = ti.peek();

                if (t.type != Token.Type.SYMBOL)
                {
                    goto abort;
                }

                // Sometimes there is an extra token between the colons
                if (t.str != ":")
                {
                    ti.readSymbol();
                    t = ti.peek();
                }

                if (!consumeSymbol(ti, ":"))
                {
                    goto abort;
                }

                // read the register number
                t = ti.peek();

                if (t.type != Token.Type.SYMBOL)
                {
                    goto abort;
                }

                // Consume the symbol we just peeked
                ti.readSymbol();

                if (t.str == "texunit")
                {
                    // We're reading a texture unit

                }
                else if (t.str == "c")
                {
                    // We're reading a regular variable; parse the open bracket

                    if (!consumeSymbol(ti, "["))
                    {
                        goto abort;
                    }

                }
                else if ((t.type == Token.Type.SYMBOL) && (t.str == ":"))
                {
                    // Unused variable; must be present but is not bound

                    Binding binding = new Binding();
                    binding.source = Source.VARIABLE;
                    binding.type = type;
                    binding.name = name;
                    binding.slot = Binding.UNASSIGNED;
                    bindingArray.Add(binding);
                    return;

                }
                else
                {
                    // Something unexpected happened.
                    goto abort;
                }

                t = ti.peek();

                if (t.type == Token.Type.NUMBER)
                {
                    int slot = g3dmath.iRound(ti.readNumber());
                    Binding binding = new Binding();
                    binding.source = Source.VARIABLE;
                    binding.type = type;
                    binding.name = name;
                    binding.slot = slot;
                    bindingArray.Add(binding);
                }

            abort:;
                // Jump here if anything unexpected is encountered during parsing
            }

            /** Called from parse() */
            private void parseConstant(TextInput ti)
            {
                if (consumeSymbol(ti, "c") && consumeSymbol(ti, "["))
                {
                    // constant
                    Token t = ti.peek();

                    if (t.type == Token.Type.NUMBER)
                    {
                        Binding binding = new Binding();
                        binding.source = Source.CONSTANT;
                        binding.type   = Type.FLOAT4;
                        binding.slot   = g3dmath.iRound(ti.readNumber());

                        if (consumeSymbol(ti, "]") && consumeSymbol(ti, "="))
                        {
                            for (int i = 0; i < 4; ++i)
                            {
                                t = ti.peek();
                                if (t.type == Token.Type.NUMBER)
                                {
                                    binding.vector[i] = (float)ti.readNumber();
                                }
                            }
                            bindingArray.Add(binding);
                        }
                    }
                }
            }

            /**
              Initializes from a Cg generated assembly program.
             */
            public void parse(string code)
            {
                bindingArray.Clear();

                TextInput ti = new TextInput(TextInput.FS.FROM_STRING, code);

                while (ti.hasMore())
                {
                    Token t = ti.read();
                    // Scan for "#"
                    while (!symbolMatch(t, "#") && ti.hasMore())
                    {
                        t = ti.read();
                    }

                    if (ti.hasMore())
                    {
                        // Read the comment line
                        Token nextToken = ti.peek();
                        if (nextToken.type == Token.Type.SYMBOL)
                        {
                            ti.readSymbol();
                            if (nextToken.str == "var")
                            {

                                parseVariable(ti);

                            }
                            else if (nextToken.str == "const")
                            {

                                parseConstant(ti);

                            }
                        }
                    }
                }
            }

            /**
              Binds constants using the NVIDIA API.
             */
            public void nvBind(int target)
            {
                for (int b = 0; b < bindingArray.Count; ++b)
                {
                    Binding binding = bindingArray[b];

                    if ((binding.source == Source.CONSTANT) && (binding.type == Type.FLOAT4))
                    {
                        IGraphicsBackend backend = GLCaps.getBackendInstance();
                        if (backend == null)
                        {
                            throw new InvalidOperationException("GLCaps has not been initialized.");
                        }
                        backend.SetAssemblyProgramLocalParameter(target, binding.slot, binding.vector);
                    }
                }
            }

            /**
              Binds constants using the ARB API.
             */
            public void arbBind(int target)
            {
                for (int b = 0; b < bindingArray.Count; ++b)
                {
                    Binding binding = bindingArray[b];

                    if ((binding.source == Source.CONSTANT) && (binding.type == Type.FLOAT4))
                    {
                        IGraphicsBackend backend = GLCaps.getBackendInstance();
                        if (backend == null)
                        {
                            throw new InvalidOperationException("GLCaps has not been initialized.");
                        }
                        backend.SetAssemblyProgramLocalParameter(target, binding.slot, binding.vector);
                    }
                }
            }

            internal int count()
            {
                return bindingArray.Count;
            }

            internal Binding get(int i)
            {
                return bindingArray[i];
            }
        }

        /**
         Argument list for a vertex program.
         See RenderDevice::setVertexProgram
         and RenderDevice::setPixelProgram.
         */
        public sealed class ArgList
        {
            private Dictionary<string, object> args = new Dictionary<string, object>();

            private void setValue(string name, object value)
            {
                if (args.ContainsKey(name))
                {
                    throw new InvalidOperationException("Cannot set variable \"" + name + "\" more than once");
                }
                args[name] = value;
            }

            public void set(string name, Matrix4 value) { setValue(name, value); }
            public void set(string name, Matrix3 value) { setValue(name, value); }
            public void set(string name, Vector4 value) { setValue(name, value); }
            public void set(string name, Vector3 value) { setValue(name, value); }
            public void set(string name, Vector2 value) { setValue(name, value); }
            public void set(string name, Color4 value) { setValue(name, value); }
            public void set(string name, Color3 value) { setValue(name, value); }
            public void set(string name, float value) { setValue(name, value); }
            public void set(string name, int value) { setValue(name, value); }
            public void set(string name, Texture value) { setValue(name, value); }
            public void set(string name, CoordinateFrame value) { setValue(name, value); }

            public bool get(string name, out Matrix4 value) { object o; if (args.TryGetValue(name, out o) && o is Matrix4) { value = (Matrix4)o; return true; } value = default(Matrix4); return false; }
            public bool get(string name, out Matrix3 value) { object o; if (args.TryGetValue(name, out o) && o is Matrix3) { value = (Matrix3)o; return true; } value = default(Matrix3); return false; }
            public bool get(string name, out Vector4 value) { object o; if (args.TryGetValue(name, out o) && o is Vector4) { value = (Vector4)o; return true; } value = default(Vector4); return false; }
            public bool get(string name, out Vector3 value) { object o; if (args.TryGetValue(name, out o) && o is Vector3) { value = (Vector3)o; return true; } value = default(Vector3); return false; }
            public bool get(string name, out Vector2 value) { object o; if (args.TryGetValue(name, out o) && o is Vector2) { value = (Vector2)o; return true; } value = default(Vector2); return false; }
            public bool get(string name, out Color4 value) { object o; if (args.TryGetValue(name, out o) && o is Color4) { value = (Color4)o; return true; } value = default(Color4); return false; }
            public bool get(string name, out Color3 value) { object o; if (args.TryGetValue(name, out o) && o is Color3) { value = (Color3)o; return true; } value = default(Color3); return false; }
            public bool get(string name, out float value) { object o; if (args.TryGetValue(name, out o) && o is float) { value = (float)o; return true; } value = 0; return false; }
            public bool get(string name, out int value) { object o; if (args.TryGetValue(name, out o) && o is int) { value = (int)o; return true; } value = 0; return false; }
            public bool get(string name, out Texture value) { object o; if (args.TryGetValue(name, out o) && o is Texture) { value = (Texture)o; return true; } value = null; return false; }
            public bool get(string name, out CoordinateFrame value) { object o; if (args.TryGetValue(name, out o) && o is CoordinateFrame) { value = (CoordinateFrame)o; return true; } value = default(CoordinateFrame); return false; }

            public bool has(string name)
            {
                return args.ContainsKey(name);
            }

            public void clear()
            {
                args.Clear();
            }

            internal IEnumerable<KeyValuePair<string, object>> getArguments()
            {
                return args;
            }

            internal object get(string name)
            {
                return args[name];
            }

            internal int count()
            {
                return args.Count;
            }

            internal IEnumerable<string> getKeys()
            {
                return args.Keys;
            }
        }

        public class Settings
        {
            public string name;
            public string vertexShader;
            public string pixelShader;
            public string defines;

            public Settings()
            {
                name = "";
                vertexShader = "";
                pixelShader = "";
                defines = "";
            }
        }

        /** Formal parameters */
        private BindingTable bindingTable = new BindingTable();

        protected string name;
        protected object backendHandle;
        protected ShaderProgramInfo programInfo;

        /** e.g. GL_VERTEX_PROGRAM_ARB, GL_FRAGMENT_PROGRAM_ARB */
        protected int unit = 0;

        protected uint glProgram;

        protected string filename;

        protected Extension extension = Extension.ARB;

        protected string vertexSource = "";
        protected string pixelSource = "";
        protected GraphicsShaderLanguage language = GraphicsShaderLanguage.GLSL;
        protected bool vertexStage = false;

        protected GPUProgram(string name)
        {
            this.name = name;
        }

        protected GPUProgram(string name, string filename)
        {
            this.name = name;
            this.filename = filename;
        }

        protected void compileProgram(GraphicsShaderLanguage language, string vertexSource, string pixelSource)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            programInfo = backend.CompileShaderProgram(language, vertexSource, pixelSource);
            backendHandle = programInfo.ProgramID;
            glProgram = (uint)programInfo.ProgramID;
            this.vertexSource = vertexSource;
            this.pixelSource = pixelSource;
            this.language = language;
        }

        /**
          Loads an ARB/NVIDIA assembly language program.  The original saved and
          restored all attributes with glPushAttrib/glPopAttrib around the enable;
          the port enables and then disables the program unit, which has the same
          result whenever no other program of the same kind was active.
         */
        protected void loadAssemblyProgram(string code)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }

            Extension assemblyExtension;
            unit = getUnitFromCode(code, out assemblyExtension);
            extension = assemblyExtension;

            backend.EnableAssemblyProgram(unit, true);

            uint id;
            genPrograms(1, out id);
            glProgram = id;
            bindProgram(unit, glProgram);
            backend.LoadAssemblyProgram(unit, (int)glProgram, code);

            int pos;
            string msg;
            getProgramError(out pos, out msg);
            if (msg.Length > 0)
            {
                deletePrograms(1, glProgram);
                glProgram = 0;
                backend.EnableAssemblyProgram(unit, false);
                throw new InvalidOperationException(
                    string.Format("GPU Program Error in \"{0}\" ({1}): {2}", filename, pos, msg));
            }

            bindingTable.parse(code);

            backend.EnableAssemblyProgram(unit, false);
        }

        /**
          Determines which unit and extension API a shader uses from the first line.
         */
        protected static int getUnitFromCode(string code, out Extension extension)
        {
            extension = Extension.ARB;

            if (code == null)
            {
                return GL_NONE;
            }

            if (code.StartsWith("!!ARBvp1.0")) {
                extension = Extension.ARB;
                return GL_VERTEX_PROGRAM_ARB;
            } else if (code.StartsWith("!!ARBfp1.0")) {
                extension = Extension.ARB;
                return GL_FRAGMENT_PROGRAM_ARB;
            } else if (code.StartsWith("!!VP2.0")) {
                extension = Extension.NVIDIA;
                return GL_VERTEX_PROGRAM_NV;
            } else if (code.StartsWith("!!VP1.0")) {
                extension = Extension.NVIDIA;
                return GL_VERTEX_PROGRAM_NV;
            } else if (code.StartsWith("!!FP1.0")) {
                extension = Extension.NVIDIA;
                return GL_FRAGMENT_PROGRAM_NV;
            } else if (code.StartsWith("!!FP1.1")) {
                extension = Extension.NVIDIA;
                return GL_FRAGMENT_PROGRAM_NV;
            } else if (code.StartsWith("!!FP2.0")) {
                extension = Extension.NVIDIA;
                return GL_FRAGMENT_PROGRAM_NV;
            }
            return GL_NONE;
        }

        /**
          Abstraction over NVIDIA/ARB extensions
         */
        protected void genPrograms(int num, out uint id)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            id = (uint)backend.GenAssemblyProgram(unit);
        }

        protected void bindProgram(int unit, uint glProgram)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            switch (extension)
            {
                case Extension.NVIDIA:
                    bindingTable.nvBind(unit);
                    break;

                case Extension.ARB:
                    bindingTable.arbBind(unit);
                    break;
            }
            backend.BindAssemblyProgram(unit, (int)glProgram);
        }

        protected void loadProgram(string code)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            backend.LoadAssemblyProgram(unit, (int)glProgram, code);
        }

        protected void loadConstant(int slot, Vector4 value)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            backend.SetAssemblyProgramLocalParameter(unit, slot, value);
        }

        protected void getProgramError(out int pos, out string msg)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            backend.GetAssemblyProgramError(unit, out pos, out msg);
        }

        protected void deletePrograms(int num, uint id)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            if (id != 0)
            {
                backend.DeleteAssemblyProgram((int)id);
            }
        }

        /**
            Called by RenderDevice::setVertexProgram() and RenderDevice::setPixelProgram()
        */
        protected internal virtual void setArgs(RenderDevice renderDevice, ArgList args)
        {
            if (isAssembly())
            {
                setAssemblyArgs(renderDevice, args);
                return;
            }
            if (programInfo == null)
            {
                throw new InvalidOperationException("The program was not compiled.");
            }
            if (!programInfo.Success)
            {
                throw new InvalidOperationException("The program failed to compile: " + programInfo.Messages);
            }

            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }

            backend.UseShaderProgram(programInfo.ProgramID);

            if (args == null)
            {
                return;
            }

            for (int i = 0; i < programInfo.Uniforms.Count; ++i)
            {
                ShaderUniformInfo uniform = programInfo.Uniforms[i];
                if (uniform.Location < 0)
                {
                    continue;
                }
                if (!args.has(uniform.Name))
                {
                    throw new InvalidOperationException("No value provided for GPUProgram variable \"" + uniform.Name + "\"");
                }
            }

            foreach (KeyValuePair<string, object> argument in args.getArguments())
            {
                ShaderUniformInfo uniform = null;
                foreach (ShaderUniformInfo candidate in programInfo.Uniforms)
                {
                    if (candidate.Name == argument.Key)
                    {
                        uniform = candidate;
                        break;
                    }
                }
                if (uniform == null)
                {
                    throw new ArgumentException("Unknown program argument '" + argument.Key + "'.");
                }
                bindUniform(renderDevice, backend, programInfo.ProgramID, uniform, argument.Value);
            }
        }

        /**
          Converts an ArgList value to the Cg representation used by assembly
          programs: a row-major vector array plus the type the original ArgList
          would have derived from it.
         */
        private static bool tryGetAssemblyArg(object value, out Type type, out Vector4[] vector, out Texture texture)
        {
            type = Type.FLOAT1;
            vector = new Vector4[4];
            texture = null;

            if (value is Texture)
            {
                texture = (Texture)value;
                switch (texture.getDimension())
                {
                    case Texture.Dimension.DIM_2D_NPOT:
                    case Texture.Dimension.DIM_2D:
                        type = Type.SAMPLER2D;
                        break;

                    case Texture.Dimension.DIM_2D_RECT:
                        type = Type.SAMPLERRECT;
                        break;

                    case Texture.Dimension.DIM_CUBE_MAP_NPOT:
                    case Texture.Dimension.DIM_CUBE_MAP:
                        type = Type.SAMPLERCUBE;
                        break;
                }
                return true;
            }
            else if (value is CoordinateFrame)
            {
                type = Type.FLOAT4X4;
                Matrix4 m = ((CoordinateFrame)value).toMatrix4();
                for (int r = 0; r < 4; ++r)
                {
                    vector[r] = m.getRow(r);
                }
                return true;
            }
            else if (value is Matrix4)
            {
                type = Type.FLOAT4X4;
                Matrix4 m = (Matrix4)value;
                for (int r = 0; r < 4; ++r)
                {
                    vector[r] = m.getRow(r);
                }
                return true;
            }
            else if (value is Matrix3)
            {
                type = Type.FLOAT3X3;
                Matrix3 m = (Matrix3)value;
                for (int r = 0; r < 3; ++r)
                {
                    vector[r] = new Vector4(m.getRowVector(r), 0);
                }
                return true;
            }
            else if (value is Vector4)
            {
                type = Type.FLOAT4;
                vector[0] = (Vector4)value;
                return true;
            }
            else if (value is Vector3)
            {
                type = Type.FLOAT3;
                vector[0] = new Vector4((Vector3)value, 0);
                return true;
            }
            else if (value is Vector2)
            {
                type = Type.FLOAT2;
                vector[0] = new Vector4((Vector2)value, 0, 0);
                return true;
            }
            else if (value is float)
            {
                type = Type.FLOAT1;
                vector[0] = new Vector4((float)value, 0, 0, 0);
                return true;
            }

            return false;
        }

        /**
          Binds arguments to an ARB/NVIDIA assembly program using the bindings
          parsed from the Cg header comments.
         */
        private void setAssemblyArgs(RenderDevice renderDevice, ArgList args)
        {
            int numVariables = 0;

            // Iterate through formal bindings
            for (int b = 0; b < bindingTable.count(); ++b)
            {
                BindingTable.Binding binding = bindingTable.get(b);

                if (binding.source == GPUProgram.Source.VARIABLE)
                {
                    ++numVariables;
                    Type argType;
                    Vector4[] argVector;
                    Texture argTexture;

                    if (!args.has(binding.name) ||
                        !tryGetAssemblyArg(args.get(binding.name), out argType, out argVector, out argTexture))
                    {
                        throw new InvalidOperationException("No value provided for GPUProgram variable \"" + binding.name + "\"");
                    }

                    // check the type
                    if (argType != binding.type)
                    {
                        throw new InvalidOperationException(
                            "Variable \"" + binding.name + "\" was declared as type " +
                            GPUProgram.toString(binding.type) + " and the values supplied" +
                            " at runtime had type " + GPUProgram.toString(argType));
                    }

                    if (binding.slot != BindingTable.Binding.UNASSIGNED)
                    {
                        switch (binding.type)
                        {
                        case GPUProgram.Type.SAMPLER1D:
                        case GPUProgram.Type.SAMPLER2D:
                        case GPUProgram.Type.SAMPLER3D:
                        case GPUProgram.Type.SAMPLERCUBE:
                        case GPUProgram.Type.SAMPLERRECT:
                            renderDevice.setTexture((uint)binding.slot, argTexture);
                            break;

                        case GPUProgram.Type.FLOAT4:
                        case GPUProgram.Type.FLOAT3:
                        case GPUProgram.Type.FLOAT2:
                        case GPUProgram.Type.FLOAT1:
                            loadConstant(binding.slot, argVector[0]);
                            break;

                        case GPUProgram.Type.FLOAT2X2:
                            throw new NotSupportedException("FLOAT2X2 not implemented.");

                        case GPUProgram.Type.FLOAT3X3:
                            for (int s = 0; s < 3; ++s)
                            {
                                loadConstant(binding.slot + s, argVector[s]);
                            }
                            break;

                        case GPUProgram.Type.FLOAT4X4:
                            for (int s = 0; s < 4; ++s)
                            {
                                loadConstant(binding.slot + s, argVector[s]);
                            }
                            break;
                        }
                    }
                }
            }

            if (numVariables < args.count())
            {
                // Some variables were unused.  Figure out which they were.
                foreach (string argName in args.getKeys())
                {
                    // See if this arg was in the formal binding list

                    bool foundArgument = false;

                    for (int b = 0; b < bindingTable.count(); ++b)
                    {
                        if (bindingTable.get(b).name == argName)
                        {
                            foundArgument = true;
                            break;
                        }
                    }

                    Debug.debugAssertM(foundArgument,
                        "Unused GPU program argument specified: \"" + argName + "\".");
                }
            }
        }

        public string getName()
        {
            return name;
        }

        /**
          Internal use only.  Reports whether a source is an ARB/NVIDIA assembly
          program and, when it is, the unit and extension set it uses.
         */
        protected static bool isAssemblyCode(string code, out int unit, out Extension extension)
        {
            int u = getUnitFromCode(code, out extension);
            unit = u;
            return u != GL_NONE;
        }

        /**
          True when this program was loaded from an ARB/NVIDIA assembly source.
         */
        protected bool isAssembly()
        {
            return unit != GL_NONE;
        }

        public bool ok() { return isAssembly() ? glProgram != 0 : (programInfo != null && programInfo.Success); }
        public string messages() { return programInfo == null ? "" : programInfo.Messages; }

        /**
         Reload from supplied code or from the original file that
         was specified (handy when debugging shaders) */
        public virtual void reload()
        {
            reload("");
        }

        public virtual void reload(string code)
        {
            string newCode = code;
            if (newCode == null || newCode == "")
            {
                if (filename != null && filename != "" && File.Exists(filename))
                {
                    newCode = File.ReadAllText(filename);
                }
                else
                {
                    newCode = vertexStage ? vertexSource : pixelSource;
                }
            }

            Extension reloadExtension;
            int reloadUnit = getUnitFromCode(newCode, out reloadExtension);
            if (reloadUnit != GL_NONE)
            {
                deletePrograms(1, glProgram);
                glProgram = 0;
                loadAssemblyProgram(newCode);
                return;
            }

            if (vertexStage)
            {
                compileProgram(language, newCode, pixelSource);
            }
            else
            {
                compileProgram(language, vertexSource, newCode);
            }
        }

        public uint getOpenGLID()
        {
            return glProgram;
        }

        /**
         Binds this program.
         */
        public void bind()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            if (isAssembly())
            {
                backend.EnableAssemblyProgram(unit, true);
                bindProgram(unit, glProgram);
                return;
            }
            if (programInfo == null)
            {
                throw new InvalidOperationException("The program was not compiled.");
            }
            backend.UseShaderProgram(programInfo.ProgramID);
        }

        /**
         Unbinds and disables.
         */
        public void disable()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            if (isAssembly())
            {
                backend.EnableAssemblyProgram(unit, false);
                return;
            }
            if (backend.Kind != GraphicsBackendKind.OpenGL)
            {
                throw new NotSupportedException("The active graphics backend does not support unbinding a GPU program.");
            }
            backend.UseShaderProgram(0);
        }

        public virtual void bind(RenderDevice renderDevice, ArgList args)
        {
            setArgs(renderDevice, args);
        }

        private static void bindUniform(RenderDevice renderDevice, IGraphicsBackend backend, int programID, ShaderUniformInfo uniform, object value)
        {
            if (value is Texture)
            {
                Texture texture = (Texture)value;
                int textureID = texture.getBackendTextureID(backend);
                if (textureID == 0)
                {
                    textureID = renderDevice.allocateTexture(texture);
                }
                backend.SetShaderTexture(programID, uniform.TextureUnit, textureID, texture.getDimension());
                backend.UniformTexture(uniform.Location, uniform.TextureUnit);
            }
            else if (value is CoordinateFrame)
            {
                backend.UniformMatrix4fv(uniform.Location, ((CoordinateFrame)value).toMatrix4());
            }
            else if (value is Matrix4)
            {
                backend.UniformMatrix4fv(uniform.Location, (Matrix4)value);
            }
            else if (value is Matrix3)
            {
                backend.UniformMatrix3fv(uniform.Location, (Matrix3)value);
            }
            else if (value is Vector2)
            {
                backend.Uniform2fv(uniform.Location, (Vector2)value);
            }
            else if (value is Vector3)
            {
                backend.Uniform3fv(uniform.Location, (Vector3)value);
            }
            else if (value is Vector4)
            {
                backend.Uniform4fv(uniform.Location, (Vector4)value);
            }
            else if (value is Color3)
            {
                Color3 color = (Color3)value;
                backend.Uniform3fv(uniform.Location, new Vector3(color.r, color.g, color.b));
            }
            else if (value is Color4)
            {
                Color4 color = (Color4)value;
                backend.Uniform4fv(uniform.Location, new Vector4(color.r, color.g, color.b, color.a));
            }
            else if (value is int)
            {
                backend.Uniform1i(uniform.Location, (int)value);
            }
            else if (value is float)
            {
                backend.Uniform1f(uniform.Location, (float)value);
            }
            else
            {
                throw new ArgumentException("Unsupported program argument type.");
            }
        }

        public virtual void unbind(RenderDevice renderDevice)
        {
        }

        protected override void dispose()
        {
            if (isAssembly())
            {
                deletePrograms(1, glProgram);
                glProgram = 0;
                unit = 0;
            }
            else if (programInfo != null && programInfo.ProgramID != 0)
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                if (backend != null)
                {
                    backend.DeleteShaderProgram(programInfo.ProgramID);
                }
                programInfo.ProgramID = 0;
                glProgram = 0;
                backendHandle = null;
            }
            base.dispose();
        }

        /**
          Sets a program.local parameter on an ARB/NVIDIA assembly program.
          This replaces the original raw glProgramLocalParameter4fARB call used
          by the demos, which cannot be expressed through the backend-neutral API.
         */
        public void setLocalParameter(int slot, Vector4 value)
        {
            if (!isAssembly())
            {
                throw new InvalidOperationException("The program is not an ARB/NVIDIA assembly program.");
            }
            loadConstant(slot, value);
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
