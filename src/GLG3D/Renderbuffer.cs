/**
  @file Renderbuffer.h

  @maintainer Daniel Hilferty, djhilferty@users.sourceforge.net

  @created 2006-01-06
  @edited 2006-02-14
*/

using System;

namespace G3D
{

    /*
      Abstraction of the OpenGL renderbuffer object.  A Framebuffer has many
      buffers inside it; typically one for colors, one for stencil, one for depth.
      Those are Renderbuffers.

      A Renderbuffer object is essentially an image that cannot be used as a
      texture.  It may be rendered to, but not used for rendering.  A perfect
      example of a pure Renderbuffer would be a Stencil buffer.  All other
      Renderbuffers could just as easily be implemented as textures.

      See G3D::Framebuffer for a detailed example.

      <B>BETA API</B> -- Subject to change

        @cite http://oss.sgi.com/projects/ogl-sample/registry/EXT/framebuffer_object.txt
    */
    public sealed class Renderbuffer : ReferenceCountedObject
    {
        public sealed class Settings
        {
            public TextureFormat format;
            public int width;
            public int height;
            public string name;

            public Settings()
            {
                format = TextureFormat.DEPTH24;
                width = 0;
                height = 0;
                name = "";
            }
        }

        /** Renderbuffer name */
        private string mName;

        /** OpenGL Object ID */
        private int mImageID;

        /** Texel format */
        private TextureFormat mFormat;

        /** Buffer width */
        private int mWidth;

        /** Buffer height */
        private int mHeight;

        private IGraphicsBackend backend;
        private object backendHandle;

        /** Constructor */
        private Renderbuffer(
            string name,
            int renderbufferID,
            TextureFormat format,
            int width,
            int height)
        {
            mName = name ?? "";
            mImageID = renderbufferID;
            mFormat = format;
            mWidth = width;
            mHeight = height;
        }

        /**
         Creates an empty renderbuffer object.

         @param name	Name of renderbuffer
         @param format	Texel format
         @param width  Width
         @param height Height
         @param depth  Image depth
         */
        public static Renderbuffer createEmpty(
            string name,
            TextureFormat format,
            int width,
            int height)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "Renderbuffer dimensions must be positive.");
            }

            Renderbuffer renderbuffer = new Renderbuffer(name, 0, format, width, height);

            IGraphicsBackend activeBackend = GLCaps.getBackendInstance();
            if (activeBackend != null)
            {
                renderbuffer.ensureBackendResource(activeBackend);
                activeBackend.BindRenderbuffer(0);
            }

            return renderbuffer;
        }

        public static Renderbuffer create(Settings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            return createEmpty(settings.name, settings.format, settings.width, settings.height);
        }

        /**
         Create a renderbuffer object from a previously initialized OpenGL
         renderbuffer context.

         @param name			Name of renderbuffer
         @param imageID OpenGL context of prior Renderbuffer
         @param format			Texel format
         @param width			Width
         @param height			Height
         @param depth			Image depth
         */
        public static Renderbuffer fromGLRenderbuffer(
            string name,
            int imageID,
            TextureFormat format)
        {
            throw new NotSupportedException(
                "The backend contract cannot query the dimensions of an existing renderbuffer. " +
                "Use fromGLRenderbuffer(string name, int imageID, TextureFormat format, int width, int height) " +
                "or add the backend operations IGraphicsBackend.GetRenderbufferWidth(int renderbufferID) and " +
                "IGraphicsBackend.GetRenderbufferHeight(int renderbufferID).");
        }

        public static Renderbuffer fromGLRenderbuffer(
            string name,
            int imageID,
            TextureFormat format,
            int width,
            int height)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "Renderbuffer dimensions must be positive.");
            }
            Renderbuffer renderbuffer = new Renderbuffer(name, imageID, format, width, height);
            renderbuffer.backend = GLCaps.getBackendInstance();
            return renderbuffer;
        }

        /**
         Get OpenGL renderbuffer ID, useful for accessing
         functionality not directly exposed by G3D.
        */
        public int openGLID()
        {
            return mImageID;
        }

        public int getRenderbufferID()
        {
            return mImageID;
        }

        public TextureFormat format()
        {
            return mFormat;
        }

        public TextureFormat getFormat()
        {
            return mFormat;
        }

        public string name()
        {
            return mName;
        }

        public string getName()
        {
            return mName;
        }

        public int width()
        {
            return mWidth;
        }

        public int getWidth()
        {
            return mWidth;
        }

        public int height()
        {
            return mHeight;
        }

        public int getHeight()
        {
            return mHeight;
        }

        public Vector2 vector2Bounds()
        {
            return new Vector2((float)mWidth, (float)mHeight);
        }

        public Rect2D rect2DBounds()
        {
            return Rect2D.xywh(0.0f, 0.0f, (float)mWidth, (float)mHeight);
        }

        public void storage(TextureFormat format, int width, int height)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "Renderbuffer dimensions must be positive.");
            }

            mFormat = format;
            mWidth = width;
            mHeight = height;

            if (mImageID != 0 && backend != null)
            {
                backend.RenderbufferStorage(mImageID, format, width, height);
                backend.BindRenderbuffer(0);
            }
        }

        public void setFormat(TextureFormat format)
        {
            storage(format, mWidth, mHeight);
        }

        public void setWidth(int width)
        {
            storage(mFormat, width, mHeight);
        }

        public void setHeight(int height)
        {
            storage(mFormat, mWidth, height);
        }

        public void bind()
        {
            bind(GLCaps.getBackendInstance());
        }

        public void bind(IGraphicsBackend targetBackend)
        {
            if (targetBackend == null)
            {
                throw new InvalidOperationException(
                    "No graphics backend is available. Set the backend with GLCaps.setBackend or pass one to bind(IGraphicsBackend).");
            }

            ensureBackendResource(targetBackend);
            targetBackend.BindRenderbuffer(mImageID);
        }

        public void unbind()
        {
            unbind(GLCaps.getBackendInstance());
        }

        public void unbind(IGraphicsBackend targetBackend)
        {
            if (targetBackend == null)
            {
                throw new InvalidOperationException(
                    "No graphics backend is available. Set the backend with GLCaps.setBackend or pass one to unbind(IGraphicsBackend).");
            }
            targetBackend.BindRenderbuffer(0);
        }

        internal int ensureBackendResource(IGraphicsBackend targetBackend)
        {
            if (targetBackend == null)
            {
                throw new InvalidOperationException("No graphics backend is available.");
            }
            if (mFormat == null)
            {
                throw new InvalidOperationException("The renderbuffer has no texture format.");
            }
            if (mWidth <= 0 || mHeight <= 0)
            {
                throw new InvalidOperationException("The renderbuffer has no storage.");
            }
            if (mImageID != 0)
            {
                if (backend != null && backend != targetBackend)
                {
                    throw new InvalidOperationException("The renderbuffer was allocated by a different graphics backend.");
                }
                return mImageID;
            }

            mImageID = targetBackend.CreateRenderbuffer();
            backend = targetBackend;
            targetBackend.RenderbufferStorage(mImageID, mFormat, mWidth, mHeight);
            targetBackend.BindRenderbuffer(0);
            return mImageID;
        }

        internal void setRenderbufferID(int id)
        {
            mImageID = id;
            if (backend == null)
            {
                backend = GLCaps.getBackendInstance();
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

        /** Destroys the underlying OpenGL id */
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (backend != null && mImageID != 0)
                {
                    backend.DeleteRenderbuffer(mImageID);
                }
                mImageID = 0;
            }
            base.Dispose(disposing);
        }
    }
}
