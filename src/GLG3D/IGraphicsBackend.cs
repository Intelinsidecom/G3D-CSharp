using System;
using System.Collections.Generic;

namespace G3D
{

    public interface IGraphicsBackend : IDisposable
    {
        GraphicsBackendKind Kind { get; }
        string Description { get; }
        BackendCaps Caps { get; }

        void Initialize(IGraphicsWindow window);
        void BeginFrame();
        void EndFrame();
        void Clear(bool color, bool depth, bool stencil);
        void Resize(int width, int height);

        // Texture management
        int CreateTexture();
        int CreateTexture(TextureFormat format, int width, int height, Texture.DepthReadMode depthReadMode);
        void DeleteTexture(int textureID);
        void BindTexture(int textureUnit, int textureID, Texture.Dimension dimension);
        int GetTextureTarget(Texture.Dimension dimension);
        void TexImage2D(int target, int level, TextureFormat format, int width, int height, int border, byte[] data);
        void CompressedTexImage2D(int target, TextureFormat format, int width, int height, List<List<byte[]>> mipMaps);
        void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, TextureFormat format, byte[] data);
        void GenerateMipmap(int target);
        void TexParameter(int target, Texture.WrapMode wrapS, Texture.WrapMode wrapT, Texture.InterpolateMode minFilter, Texture.InterpolateMode magFilter, float maxAnisotropy);
        void SetTextureDepthCompare(int target, Texture.DepthReadMode mode);
        void ConfigureShadowMap(int textureUnit, Matrix4 lightMVP, Matrix4 cameraToWorldMatrixInverse, int textureID, Texture.DepthReadMode mode);
        void DisableShadowMap(int textureUnit);
        void CopyTexImage2D(int target, int level, int x, int y, int width, int height, int border);
        void CopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height);
        void GetTexImage(int target, int level, TextureFormat format, byte[] data);

        // Shader management
        bool SupportsShaderLanguage(GraphicsShaderLanguage language);
        int CreateShaderProgram(string vertexSource, string fragmentSource);
        int CreateShaderProgram(GraphicsShaderLanguage language, string vertexSource, string fragmentSource);
        ShaderProgramInfo CompileShaderProgram(GraphicsShaderLanguage language, string vertexSource, string fragmentSource);
        void DeleteShaderProgram(int programID);
        void UseShaderProgram(int programID);
        int GetUniformLocation(int programID, string name);
        int GetAttribLocation(int programID, string name);
        void UniformMatrix4fv(int location, Matrix4 value);
        void UniformMatrix3fv(int location, Matrix3 value);
        void Uniform4fv(int location, Vector4 value);
        void Uniform3fv(int location, Vector3 value);
        void Uniform2fv(int location, Vector2 value);
        void Uniform1f(int location, float value);
        void Uniform1i(int location, int value);
        void UniformTexture(int location, int textureUnit);
        void SetShaderTexture(int programID, int textureUnit, int textureID, Texture.Dimension dimension);

        // Vertex Array / VBO management
        int CreateVertexArray();
        void DeleteVertexArray(int vaoID);
        void BindVertexArray(int vaoID);
        int CreateBuffer();
        void DeleteBuffer(int bufferID);
        void BindBuffer(int target, int bufferID);
        void BufferData(int target, int size, byte[] data, VAR.Usage usage);
        void BufferSubData(int target, int offset, int size, byte[] data);
        void VertexAttribPointer(int index, int size, int type, bool normalized, int stride, int offset);
        void EnableVertexAttribArray(int index);
        void DisableVertexAttribArray(int index);
        void SetVertexAttribArray(int attribNum, VAR v, bool normalize);

        // Framebuffer management
        int CreateFramebuffer();
        void DeleteFramebuffer(int framebufferID);
        void BindFramebuffer(int framebufferID);
        void FramebufferTexture2D(int target, int attachment, int textarget, int textureID, int level);
        void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, int renderbufferID);
        void AttachFramebufferTexture(int framebufferID, Framebuffer.AttachmentPoint attachment, int textureID);
        void AttachFramebufferTexture(int framebufferID, Framebuffer.AttachmentPoint attachment, int textureID, int mipLevel, int cubeFace);
        void AttachFramebufferRenderbuffer(int framebufferID, Framebuffer.AttachmentPoint attachment, int renderbufferID);
        bool CheckFramebufferStatus();

        // Renderbuffer management
        int CreateRenderbuffer();
        void DeleteRenderbuffer(int renderbufferID);
        void BindRenderbuffer(int renderbufferID);
        void RenderbufferStorage(int renderbufferID, TextureFormat format, int width, int height);

        /**
          Returns the identifier of the currently bound framebuffer, or 0 when
          the default framebuffer is bound.
         */
        int GetBoundFramebuffer();

        // Draw calls
        void DrawArrays(RenderDevice.Primitive primitive, int first, int count);
        void DrawElements(RenderDevice.Primitive primitive, int count, int type, int offset);
        void DrawElementsInstanced(RenderDevice.Primitive primitive, int count, int type, int offset, int instanceCount);

        /**
          Draws geometry from vertex arrays.  When a shader program is bound, the
          vertex, normal, color, and texture-coordinate arrays are bound to the
          program's attributes by name ("position", "normal", "color", "texCoord0").
          The index array is optional; when it is null the vertices are drawn in
          sequential order.
         */
        void DrawVAR(RenderDevice.Primitive primitive, int numIndices, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray);

        // Immediate mode drawing
        void BeginPrimitive(RenderDevice.Primitive primitive);
        void SendVertex(float x, float y, float z);
        void SendVertex(float x, float y, float z, float w);
        void SendNormal(float x, float y, float z);
        void SendColor(float r, float g, float b, float a);
        void SendTexCoord(float s, float t, int textureUnit);
        void EndPrimitive();

        // State management
        void SetDepthTest(RenderDevice.DepthTest test);
        void SetStencilTest(RenderDevice.StencilTest test, int reference);
        void SetAlphaTest(RenderDevice.AlphaTest test, double reference);
        void SetRenderMode(RenderDevice.RenderMode mode);
        void SetCullFace(RenderDevice.CullFace face);
        void SetBlendFunc(RenderDevice.BlendFunc src, RenderDevice.BlendFunc dst, RenderDevice.BlendEq eq);
        void SetColorWrite(bool enable);
        void SetAlphaWrite(bool enable);
        void SetDepthWrite(bool enable);
        void SetShadeMode(RenderDevice.ShadeMode mode);
        void SetStencilOp(RenderDevice.StencilOp fail, RenderDevice.StencilOp zfail, RenderDevice.StencilOp zpass,
                         RenderDevice.StencilOp backFail, RenderDevice.StencilOp backZfail, RenderDevice.StencilOp backZpass);
        void SetPolygonOffset(double offset);
        void SetLineWidth(double width);
        void SetPointSize(double size);
        void SetScissor(Rect2D rect, bool enable);
        void SetViewport(Rect2D viewport);
        void SetDepthRange(double near, double far);
        void SetColorClearValue(Color4 color);
        void SetDepthClearValue(double depth);
        void SetStencilClearValue(int stencil);

        // Texture matrix
        void SetTextureMatrix(int textureUnit, Matrix4 matrix);
        void SetTextureCombineMode(int textureUnit, RenderDevice.CombineMode mode);
        void SetTextureLODBias(int textureUnit, float bias);
        void SetDrawBuffer(RenderDevice.Buffer drawBuffer);

        // Matrices
        void SetProjectionMatrix(Matrix4 matrix);
        void SetModelViewMatrix(Matrix4 matrix);

        // Lighting
        void SetLight(int index, GLight light, bool enabled);
        void SetAmbientLight(Color4 color);
        void SetLightingEnabled(bool enabled);
        void SetTwoSidedLighting(bool enabled);
        void SetSpecular(Color3 color);
        void SetShininess(float shininess);

        // Milestones / queries
        int CreateQuery();
        void DeleteQuery(int queryID);
        void BeginQuery(int queryID);
        void EndQuery(int queryID);
        bool GetQueryResult(int queryID, out uint result);

        int CreateMilestone();
        void DeleteMilestone(int milestoneID);
        void SetMilestone(int milestoneID);
        void WaitForMilestone(int milestoneID);

        // Capabilities
        bool SupportsExtension(string extension);
        bool SupportsTextureFormat(TextureFormat format);
        bool SupportsTwoSidedStencil();
        bool SupportsTextureRectangle();
        bool SupportsVertexProgram();
        bool SupportsVertexProgramNV2();
        bool SupportsPixelProgram();
        bool SupportsVertexBufferObject();
        bool HasBug_glMultiTexCoord3fvARB();
        bool HasBug_normalMapTexGen();
        bool HasBug_redBlueMipmapSwap();
        bool HasBug_mipmapGeneration();
        bool HasBug_slowVBO();

        // DescribeSystem support
        string GetRenderer();
        string GetVendor();
        string GetDriverVersion();
        string GetGLVersion();
        int GetMaxTextures();
        int GetMaxTextureCoords();
        int GetMaxTextureUnits();
        int GetMaxTextureSize();
        int GetMaxColorAttachments();

        // Screenshot
        void ReadPixels(int x, int y, int width, int height, byte[] data);

        /**
          Reads the depth buffer of the currently bound render target.  The
          region is specified in the native orientation of the render target,
          which is bottom-up for OpenGL and top-down for Direct3D 11.  The
          values are normalized to the range [0, 1].
         */
        void ReadDepth(int x, int y, int width, int height, float[] data);
    }
}