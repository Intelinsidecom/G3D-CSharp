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
            throw new NotSupportedException("Legacy ARB/NVIDIA assembly GPU programs are not supported by the backend-neutral GLG3D layer.");
        }

        protected void bindProgram(int unit, uint glProgram)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            backend.UseShaderProgram((int)glProgram);
        }

        protected void loadProgram(string code)
        {
            throw new NotSupportedException("Legacy ARB/NVIDIA assembly GPU programs are not supported by the backend-neutral GLG3D layer.");
        }

        protected void loadConstant(int slot, Vector4 value)
        {
            throw new NotSupportedException("Legacy ARB/NVIDIA assembly GPU program constant registers are not supported by the backend-neutral GLG3D layer.");
        }

        protected void getProgramError(out int pos, out string msg)
        {
            throw new NotSupportedException("Legacy ARB/NVIDIA assembly GPU program errors are not supported by the backend-neutral GLG3D layer.");
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
                backend.DeleteShaderProgram((int)id);
            }
        }

        /**
            Called by RenderDevice::setVertexProgram() and RenderDevice::setPixelProgram()
        */
        protected virtual void setArgs(RenderDevice renderDevice, ArgList args)
        {
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

        public string getName()
        {
            return name;
        }

        public bool ok() { return programInfo != null && programInfo.Success; }
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
            Extension legacyExtension;
            int legacyUnit = getUnitFromCode(code == null ? "" : code, out legacyExtension);
            unit = legacyUnit;
            extension = legacyExtension;
            if (legacyUnit != GL_NONE)
            {
                throw new NotSupportedException("Legacy ARB/NVIDIA assembly GPU programs are not supported by the backend-neutral GLG3D layer.");
            }

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
            if (programInfo != null && programInfo.ProgramID != 0)
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
