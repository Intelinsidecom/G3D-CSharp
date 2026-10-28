/**
  @file Shader.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2004-04-25
  @edited  2006-05-04
*/

using System;
using System.Collections.Generic;
using G3D;

namespace G3D
{

    /** Argument to G3D::VertexAndPixelShader and G3D::Shader create methods */
    public enum UseG3DUniforms
    {
        DEFINE_G3D_UNIFORMS,
        DO_NOT_DEFINE_G3D_UNIFORMS
    }

    /**
      Reference-counted smart pointer to Shader.
    */
    // typedef ReferenceCountedPointer<class Shader> ShaderRef;

    /**
      Base class for shader arguments.
    */
    public abstract class ShaderArgument
    {
        public string name;
        public ShaderArgument(string name)
        {
            this.name = name;
        }
    }

    /**
      Abstraction of the programmable hardware pipeline.
      Use with G3D::RenderDevice::setShader.
      G3D::Shader allows you to specify C++ code (by overriding
      the methods) that executes for each group of primitives and
      OpenGL Shading Language (<A HREF="http://developer.3dlabs.com/openGL2/specs/GLSLangSpec.Full.1.10.59.pdf">GLSL</A>)
      code that executes for each vertex and each pixel.

      <P>
      Uses G3D::VertexAndPixelShader internally.  What we call pixel shaders
      are really "fragment shaders" in OpenGL terminology.

      <P>

      Unless DO_NOT_DEFINE_G3D_UNIFORMS is specified to the static constructor, the
      following additional variables will be available inside the shaders:

      <PRE>
        uniform mat4 g3d_WorldToObjectMatrix;
        uniform mat4 g3d_ObjectToWorldMatrix;
        uniform mat4 g3d_WorldToCameraMatrix;
        uniform mat4 g3d_CameraToWorldMatrix;
        uniform int  g3d_NumLights;        // 1 + highest index of the enabled lights
        uniform int  g3d_NumTextures;      // 1 + highest index of the enabled textures
        uniform vec4 g3d_ObjectLight0;     // g3d_WorldToObject * gl_LightState[0].position

        vec2 g3d_sampler2DSize(sampler2D t);        // Returns the x and y dimensions of t
        vec2 g3d_sampler2DInvSize(sampler2D t);     // Returns vec2(1.0, 1.0) / g3d_size(t) at no additional cost
      </PRE>

      <code>g3d_sampler2DSize</code> and <code>g3d_sampler2DInvSize</code> require that there be no additional space between
      the function name and parens and no space between the parens and sampler name.  There is no cost for
      definining and then not using any of these; unused variables do not increase the runtime cost of the
      shader.

      If your shader begins with <CODE>#include</CODE> or <CODE>#define</CODE> the
      line numbers will be off by one in error messages because the G3D uniforms are
      inserted on the first line.

      <P>

      <P>
      ATI's RenderMonkey is an excellent tool for writing GLSL shaders under an IDE.
      http://www.ati.com/developer/rendermonkey/index.html
      You can then load those shaders into G3D::Shader and use them with your application.

      <B>BETA API</B>
      This API is subject to change.
     */
    public class Shader
    {
        public sealed class ArgList
        {
            private Dictionary<string, object> args = new Dictionary<string, object>();

            public void set(string name, Matrix4 value) { args[name] = value; }
            public void set(string name, Matrix3 value) { args[name] = value; }
            public void set(string name, Vector4 value) { args[name] = value; }
            public void set(string name, Vector3 value) { args[name] = value; }
            public void set(string name, Vector2 value) { args[name] = value; }
            public void set(string name, Color4 value) { args[name] = value; }
            public void set(string name, Color3 value) { args[name] = value; }
            public void set(string name, float value) { args[name] = value; }
            public void set(string name, int value) { args[name] = value; }
            public void set(string name, Texture value) { args[name] = value; }
            public void set(string name, CoordinateFrame value) { args[name] = value; }

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

            internal IEnumerable<KeyValuePair<string, object>> getArguments()
            {
                return args;
            }
        }

        public class Preprocess
        {
            public string defines;
            public Preprocess() { defines = ""; }
        }

        /**
          Arguments to the vertex and pixel shader.  You may change these either
            before or after the shader is set on G3D::RenderDevice-- either way
            they will take effect immediately.*/
        public VertexAndPixelShader.ArgList args = new VertexAndPixelShader.ArgList();

        protected VertexAndPixelShader _vertexAndPixelShader;
        protected UseG3DUniforms _useUniforms;
        protected bool _preserveState;

        private string name;
        private string vertexShaderSource;
        private string pixelShaderSource;
        private object backendHandle;
        private VertexAndPixelShader program;

        protected Shader(VertexAndPixelShader v, UseG3DUniforms u)
        {
            _vertexAndPixelShader = v;
            _useUniforms = u;
            _preserveState = true;
        }

        /** For subclasses to invoke */
        protected Shader()
        {
            _preserveState = true;
        }

        public Shader(string name, string vertexShaderSource, string pixelShaderSource)
        {
            this.name = name;
            this.vertexShaderSource = vertexShaderSource;
            this.pixelShaderSource = pixelShaderSource;
            this._useUniforms = UseG3DUniforms.DEFINE_G3D_UNIFORMS;
        }

        public static Shader fromFiles(string vertexFile, string pixelFile)
        {
            return fromFiles(vertexFile, pixelFile, UseG3DUniforms.DEFINE_G3D_UNIFORMS);
        }

        public static Shader fromFiles(string vertexFile, string pixelFile, UseG3DUniforms u)
        {
            return new Shader(VertexAndPixelShader.fromFiles(vertexFile, pixelFile, u, false), u);
        }

        public static Shader fromFiles(string name, string vertexShaderFile, string pixelShaderFile)
        {
            return fromFiles(name, vertexShaderFile, pixelShaderFile, null);
        }

        public static Shader fromFiles(string name, string vertexShaderFile, string pixelShaderFile, Preprocess preprocess)
        {
            string defines = preprocess == null ? "" : preprocess.defines;
            Shader shader = new Shader(name, defines, "");
            shader.vertexShaderSource = defines + readShaderFile(vertexShaderFile);
            shader.pixelShaderSource = defines + readShaderFile(pixelShaderFile);
            return shader;
        }

        public static Shader fromStrings(string vertexCode, string pixelCode)
        {
            return fromStrings(vertexCode, pixelCode, UseG3DUniforms.DEFINE_G3D_UNIFORMS);
        }

        public static Shader fromStrings(string vertexCode, string pixelCode, UseG3DUniforms u)
        {
            return new Shader(VertexAndPixelShader.fromStrings(vertexCode, pixelCode, u, false), u);
        }

        /** Names are purely for debugging purposes */
        public static Shader fromStrings(string vertexName, string vertexCode, string pixelName, string pixelCode)
        {
            return fromStrings(vertexName, vertexCode, pixelName, pixelCode, UseG3DUniforms.DEFINE_G3D_UNIFORMS);
        }

        /** Names are purely for debugging purposes */
        public static Shader fromStrings(string vertexName, string vertexCode, string pixelName, string pixelCode, UseG3DUniforms u)
        {
            return new Shader(VertexAndPixelShader.fromStrings(vertexName, vertexCode, pixelName, pixelCode, u, false), u);
        }

        private static string readShaderFile(string filename)
        {
            if (filename == null || filename == "")
            {
                return "";
            }
            return global::System.IO.File.ReadAllText(filename);
        }

        /** Returns true if this shader is declared to accept the specified argument. */
        public bool hasArgument(string argname)
        {
            VertexAndPixelShader shader = _vertexAndPixelShader != null ? _vertexAndPixelShader : program;
            if (shader == null)
            {
                return false;
            }
            return shader.hasUniform(argname);
        }

        /**
         When true, any RenderDevice state that the shader configured before a primitive it restores at
            the end of the primitive.  When false, the shader is allowed to corrupt state.  Corruption is fast and is
            useful
            when you know that the next primitive will also be rendered with a shader, since shaders tend
            to set all of the state that they need.

            Defaults to true */
        public virtual void setPreserveState(bool s)
        {
            _preserveState = s;
        }

        public virtual bool preserveState()
        {
            return _preserveState;
        }

        public virtual bool ok()
        {
            if (_vertexAndPixelShader != null)
            {
                return _vertexAndPixelShader.ok();
            }
            return program != null && program.ok();
        }

        /** Returns true if this card supports vertex shaders */
        public static bool supportsVertexShaders()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                return false;
            }
            return backend.SupportsShaderLanguage(GraphicsShaderLanguage.GLSL) ||
                   backend.SupportsShaderLanguage(GraphicsShaderLanguage.HLSL);
        }

        /** Returns true if this card supports pixel shaders.  If vertex but not pixel
            shaders are supported you can pass an empty string as the pixel shader. */
        public static bool supportsPixelShaders()
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
         Invoked by RenderDevice immediately before a primitive group.
            Override to set state on the RenderDevice (including the underlying
            vertex and pixel shader).

            Default implementation pushes state, sets the g3d_ uniforms,
            and loads the vertex and pixel shader.
         */
        public virtual void beforePrimitive(RenderDevice renderDevice)
        {
            if (renderDevice == null)
            {
                throw new ArgumentNullException("renderDevice");
            }

            if (_preserveState) {
                renderDevice.pushState();
            }

            if (_useUniforms == UseG3DUniforms.DEFINE_G3D_UNIFORMS) {
                CoordinateFrame o2w = renderDevice.getObjectToWorldMatrix();
                CoordinateFrame c2w = renderDevice.getCameraToWorldMatrix();

                // Bind matrices
                if (hasArgument("g3d_ObjectToWorldMatrix")) {
                    args.set("g3d_ObjectToWorldMatrix", o2w);
                }

                if (hasArgument("g3d_CameraToWorldMatrix")) {
                    args.set("g3d_CameraToWorldMatrix", c2w);
                }

                if (hasArgument("g3d_WorldToObjectMatrix")) {
                    args.set("g3d_WorldToObjectMatrix", o2w.inverse());
                }

                if (hasArgument("g3d_WorldToCameraMatrix")) {
                    args.set("g3d_WorldToCameraMatrix", c2w.inverse());
                }

                if (hasArgument("g3d_NumLights")) {
                    args.set("g3d_NumLights", 0);
                }

                if (hasArgument("g3d_ObjectLight0")) {
                    args.set("g3d_ObjectLight0", objectLight);
                }

                if (hasArgument("g3d_NumTextures")) {
                    args.set("g3d_NumTextures", 0);
                }
            }

            if (_vertexAndPixelShader != null) {
                renderDevice.setVertexAndPixelShader(_vertexAndPixelShader, args);
                _vertexAndPixelShader.bindArgList(renderDevice, args);
            } else {
                VertexAndPixelShader shader = getProgram();
                renderDevice.setVertexAndPixelShader(shader, args);
                shader.bindArgList(renderDevice, args);
            }
        }

        /**
         Default implementation pops state.
         */
        public virtual void afterPrimitive(RenderDevice renderDevice)
        {
            if (renderDevice == null)
            {
                throw new ArgumentNullException("renderDevice");
            }
            if (_preserveState) {
                renderDevice.popState();
            }
        }

        public virtual string messages()
        {
            if (_vertexAndPixelShader != null)
            {
                return _vertexAndPixelShader.messages();
            }
            if (program != null)
            {
                return program.messages();
            }
            return "";
        }

        public Vector4 objectLight = new Vector4(0, 0, 0, 0);

        public string getName()
        {
            return name;
        }

        public virtual void bind(RenderDevice renderDevice, ArgList args)
        {
            VertexAndPixelShader shader = getProgram();
            VertexAndPixelShader.ArgList list = new VertexAndPixelShader.ArgList();
            foreach (KeyValuePair<string, object> argument in getArguments(args))
            {
                setArgument(list, argument.Key, argument.Value);
            }
            addG3DUniforms(renderDevice, shader, list);
            shader.bindArgList(renderDevice, list);
        }

        private VertexAndPixelShader getProgram()
        {
            if (_vertexAndPixelShader != null)
            {
                return _vertexAndPixelShader;
            }
            if (program == null)
            {
                program = VertexAndPixelShader.fromStrings(vertexShaderSource, pixelShaderSource, UseG3DUniforms.DEFINE_G3D_UNIFORMS);
                backendHandle = program.getBackendHandle();
            }
            return program;
        }

        public virtual void unbind(RenderDevice renderDevice)
        {
        }

        private static IEnumerable<KeyValuePair<string, object>> getArguments(ArgList args)
        {
            if (args == null)
            {
                return new List<KeyValuePair<string, object>>();
            }
            return args.getArguments();
        }

        private static void setArgument(VertexAndPixelShader.ArgList list, string argumentName, object value)
        {
            if (value is Matrix4) { list.set(argumentName, (Matrix4)value); }
            else if (value is Matrix3) { list.set(argumentName, (Matrix3)value); }
            else if (value is Vector4) { list.set(argumentName, (Vector4)value); }
            else if (value is Vector3) { list.set(argumentName, (Vector3)value); }
            else if (value is Vector2) { list.set(argumentName, (Vector2)value); }
            else if (value is Color4) { list.set(argumentName, (Color4)value); }
            else if (value is Color3) { list.set(argumentName, (Color3)value); }
            else if (value is float) { list.set(argumentName, (float)value); }
            else if (value is int) { list.set(argumentName, (int)value); }
            else if (value is Texture) { list.set(argumentName, (Texture)value); }
            else if (value is CoordinateFrame) { list.set(argumentName, (CoordinateFrame)value); }
            else
            {
                throw new ArgumentException("Unsupported shader argument type for '" + argumentName + "'.");
            }
        }

        private void addG3DUniforms(RenderDevice renderDevice, VertexAndPixelShader shader, VertexAndPixelShader.ArgList list)
        {
            CoordinateFrame objectToWorld = renderDevice.getObjectToWorldMatrix();
            CoordinateFrame cameraToWorld = renderDevice.getCameraToWorldMatrix();
            if (shader.hasUniform("g3d_ObjectToWorldMatrix"))
            {
                list.set("g3d_ObjectToWorldMatrix", objectToWorld.toMatrix4());
            }
            if (shader.hasUniform("g3d_CameraToWorldMatrix"))
            {
                list.set("g3d_CameraToWorldMatrix", cameraToWorld.toMatrix4());
            }
            if (shader.hasUniform("g3d_WorldToObjectMatrix"))
            {
                list.set("g3d_WorldToObjectMatrix", objectToWorld.inverse().toMatrix4());
            }
            if (shader.hasUniform("g3d_WorldToCameraMatrix"))
            {
                list.set("g3d_WorldToCameraMatrix", cameraToWorld.inverse().toMatrix4());
            }
            if (shader.hasUniform("g3d_NumLights"))
            {
                list.set("g3d_NumLights", 0);
            }
            if (shader.hasUniform("g3d_ObjectLight0"))
            {
                list.set("g3d_ObjectLight0", objectLight);
            }
            if (shader.hasUniform("g3d_NumTextures"))
            {
                list.set("g3d_NumTextures", 0);
            }
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
