/**
  @file Framebuffer.cpp

  @maintainer Daniel Hilferty, djhilferty@users.sourceforge.net

  Notes:
  <UL>
  <LI>http://oss.sgi.com/projects/ogl-sample/registry/EXT/framebuffer_object.txt
  </UL>

  @created 2006-01-07
  @edited 2006-04-07
*/

using System;
using System.Collections.Generic;

namespace G3D
{

    /*
      Abstraction of OpenGL's Framebuffer Object extension.  This is a fast and
      efficient way of rendering to textures.  This class can be used with raw OpenGL,
      without RenderDevice or SDL.

      Basic Framebuffer Theory:
      Every OpenGL program has at least one Framebuffer.  This framebuffer is
      setup by the windowing system and its image format is that specified by the
      OS.  With the Framebuffer Object extension, OpenGL gives the developer
      the ability to create offscreen framebuffers that can be used to render
      to textures of any specified format.
         The Framebuffer class is used in conjunction with the RenderDevice to
      set a render target.  The RenderDevice method setFramebuffer performs this
      action.  If a NULL argument is passed to setFramebuffer, the render target
      defaults to the window display framebuffer.
         Framebuffer works in conjunction with the push/pop RenderDevice state, but
      in a limited form.   The state will save the current Framebuffer, but will not
      save the state of the Framebuffer itself.  If the attachment points are changed
      in the push/pop block, these will not be restored by a pop.

      Note:  Not any combination of images may be attached to a Framebuffer.
      OpenGL lays out some restrictions that must be considered:

      <ol>
        <li> In order to render to a Framebuffer, there must be at least
        one image (Renderbuffer or Texture) attached to an attachment point.
        <li> All images must have the same height and width.
        <li> All images attached to a COLOR_ATTACHMENT[n] point must have
        the same internal format (RGBA8, RGBA16...etc)
        <li> If RenderDevice->setDrawBuffer is used then the specified
        attachment point must have a bound image.
        <li> The combination of internal formats of attached images does not
        violate some implementation-dependent set of restrictions (i.e., Your
        graphics card must completely implement all combinations that you
        plan to use!)
      </ol>

      <B>BETA API</B> -- Subject to change
    */
    public sealed class Framebuffer : ReferenceCountedObject
    {
        public enum Settings
        {
            DEFAULT = 0
        }

        /**
         Specifies the attachment to which a framebuffer-attachable-image
         (Renderbuffer or a Texture image) will be attached.  These mirror
         the OpenGL definition as do their values.
         */
        public enum AttachmentPoint
        {
            COLOR_ATTACHMENT0 = 0x8CE0,
            COLOR_ATTACHMENT1 = 0x8CE1,
            COLOR_ATTACHMENT2 = 0x8CE2,
            COLOR_ATTACHMENT3 = 0x8CE3,
            COLOR_ATTACHMENT4 = 0x8CE4,
            COLOR_ATTACHMENT5 = 0x8CE5,
            COLOR_ATTACHMENT6 = 0x8CE6,
            COLOR_ATTACHMENT7 = 0x8CE7,
            COLOR_ATTACHMENT8 = 0x8CE8,
            COLOR_ATTACHMENT9 = 0x8CE9,
            COLOR_ATTACHMENT10 = 0x8CEA,
            COLOR_ATTACHMENT11 = 0x8CEB,
            COLOR_ATTACHMENT12 = 0x8CEC,
            COLOR_ATTACHMENT13 = 0x8CED,
            COLOR_ATTACHMENT14 = 0x8CEE,
            COLOR_ATTACHMENT15 = 0x8CEF,
            DEPTH_ATTACHMENT = 0x8D00,
            STENCIL_ATTACHMENT = 0x8D20,

            Color0 = COLOR_ATTACHMENT0,
            Color1 = COLOR_ATTACHMENT1,
            Color2 = COLOR_ATTACHMENT2,
            Color3 = COLOR_ATTACHMENT3,
            Color4 = COLOR_ATTACHMENT4,
            Color5 = COLOR_ATTACHMENT5,
            Color6 = COLOR_ATTACHMENT6,
            Color7 = COLOR_ATTACHMENT7,
            Color8 = COLOR_ATTACHMENT8,
            Color9 = COLOR_ATTACHMENT9,
            Color10 = COLOR_ATTACHMENT10,
            Color11 = COLOR_ATTACHMENT11,
            Color12 = COLOR_ATTACHMENT12,
            Color13 = COLOR_ATTACHMENT13,
            Color14 = COLOR_ATTACHMENT14,
            Color15 = COLOR_ATTACHMENT15,
            Depth = DEPTH_ATTACHMENT,
            Stencil = STENCIL_ATTACHMENT
        }

        private sealed class Attachment
        {
            public enum Type { TEXTURE, RENDERBUFFER }

            public Type type;

            public Renderbuffer renderbuffer;
            public Texture texture;

            /** True if the texture had autoMipMap on when it was set. */
            public bool hadAutoMipMap;

            public int mipLevel;
            public int cubeFace;
        }

        public sealed class TextureAttachment
        {
            public AttachmentPoint attachmentPoint;
            public Texture texture;
            public int mipLevel;
            public int cubeFace;
        }

        public sealed class RenderbufferAttachment
        {
            public AttachmentPoint attachmentPoint;
            public Renderbuffer renderbuffer;
        }

        /**
         Slots are not specified if they correspond to NULL elements.
         */
        private readonly Dictionary<AttachmentPoint, Attachment> attachmentTable =
            new Dictionary<AttachmentPoint, Attachment>();

        /** OpenGL Object ID */
        private int framebufferID;

        /** Framebuffer name */
        private string m_name;

        /**
         Not yet implemented yet -- for non-gl error checking to pre-check
         for Framebuffer completeness.  Width & Height should also be
         implemented for this check.
        */
        private TextureFormat m_format;

        /**
         Framebuffer Height
         */
        private int m_height;

        private int m_width;

        /**
         Number of currently bound attachments.  When this hits zero we can
         add attachments with new sizes.
         */
        private int numAttachments;

        private bool sizeIsExplicit;

        private IGraphicsBackend backend;

        private object backendHandle;

        /** Default Constructor. */
        private Framebuffer(string name, int framebufferID)
        {
            m_name = name ?? "";
            this.framebufferID = framebufferID;
            m_height = 0;
            m_width = 0;
            numAttachments = 0;
            sizeIsExplicit = false;
            m_format = null;
        }

        /**
         Creates a framebuffer object from scratch.

         @param name			Name of framebuffer
         */
        public static Framebuffer create(string name)
        {
            return new Framebuffer(name, 0);
        }

        public static Framebuffer create(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "Framebuffer dimensions must be positive.");
            }
            Framebuffer framebuffer = new Framebuffer("", 0);
            framebuffer.m_width = width;
            framebuffer.m_height = height;
            framebuffer.sizeIsExplicit = true;
            return framebuffer;
        }

        /**
         Creates a framebuffer object from an OpenGL context.

         @param name			Name of framebuffer
         @param framebufferID	OpenGL id of ramebuffer
         */
        public static Framebuffer fromGLFramebuffer(string name, int framebufferID)
        {
            if (framebufferID < 0)
            {
                throw new ArgumentOutOfRangeException("framebufferID");
            }
            Framebuffer framebuffer = new Framebuffer(name, framebufferID);
            framebuffer.backend = GLCaps.getBackendInstance();
            return framebuffer;
        }

        /**
         Set one of the attachment points to reference a texture.  Set to NULL to unset.
         Auto-mipmap will automatically be disabled on set.  It will be re-enabled when
         the texture is unbound.

         Do not use a texture that is bound to the *current* framebuffer for rendering,
         however, you can render using a texture that is bound on a different frame buffer.

         @param texture		Texture to bind to the framebuffer.
         @param ap	Attachment point to bind texture to.
         */
        public void set(AttachmentPoint ap, Texture texture)
        {
            if (texture == null)
            {
                detach(ap);
                return;
            }
            attachTexture(ap, texture, 0, -1);
        }

        /**
         Set one of the attachment points to reference a renderbuffer.  Set to NULL to unset.

         @param renderbuffer	Renderbuffer to bind to the framebuffer
         @param ap	Attachment point to bind renderbuffer to.
         */
        public void set(AttachmentPoint ap, Renderbuffer renderbuffer)
        {
            if (renderbuffer == null)
            {
                detach(ap);
                return;
            }
            attachRenderbuffer(ap, renderbuffer);
        }

        public void setTexture(AttachmentPoint attachmentPoint, Texture texture)
        {
            setTexture(attachmentPoint, texture, 0, -1);
        }

        public void setTexture(AttachmentPoint attachmentPoint, Texture texture, int mipLevel, int cubeFace)
        {
            attachTexture(attachmentPoint, texture, mipLevel, cubeFace);
        }

        public void setRenderbuffer(AttachmentPoint attachmentPoint, Renderbuffer renderbuffer)
        {
            attachRenderbuffer(attachmentPoint, renderbuffer);
        }

        public void setRenderbuffer(int attachmentIndex, Renderbuffer renderbuffer)
        {
            setRenderbuffer(colorPoint(attachmentIndex), renderbuffer);
        }

        public void attachTexture(AttachmentPoint attachmentPoint, Texture texture)
        {
            attachTexture(attachmentPoint, texture, 0, -1);
        }

        public void attachTexture(AttachmentPoint attachmentPoint, Texture texture, int mipLevel, int cubeFace)
        {
            if (texture == null)
            {
                throw new ArgumentNullException("texture");
            }
            if (mipLevel < 0)
            {
                throw new ArgumentOutOfRangeException("mipLevel");
            }
            if (cubeFace < -1 || cubeFace > 5)
            {
                throw new ArgumentOutOfRangeException("cubeFace");
            }

            int attachmentWidth = texture.texelWidth();
            int attachmentHeight = texture.texelHeight();
            validateDimensions(attachmentWidth, attachmentHeight, remainingAttachmentCount(attachmentPoint));
            checkColorAttachmentCount(backend != null ? backend : GLCaps.getBackendInstance(),
                                      attachmentPoint, isColorAttachment(attachmentPoint));

            removeAttachment(attachmentPoint);

            Attachment attachment = new Attachment();
            attachment.type = Attachment.Type.TEXTURE;
            attachment.texture = texture;
            attachment.mipLevel = mipLevel;
            attachment.cubeFace = cubeFace;
            attachment.hadAutoMipMap = texture.parameters().autoMipMap;
            if (attachment.hadAutoMipMap)
            {
                texture.setAutoMipMap(false);
            }
            attachmentTable[attachmentPoint] = attachment;
            ++numAttachments;

            if (numAttachments == 1 && !sizeIsExplicit)
            {
                m_width = attachmentWidth;
                m_height = attachmentHeight;
            }

            applyAttachment(backend != null ? backend : GLCaps.getBackendInstance(), attachmentPoint, attachment);
            updateDrawBuffer(backend != null ? backend : GLCaps.getBackendInstance());
        }

        public void attachRenderbuffer(AttachmentPoint attachmentPoint, Renderbuffer renderbuffer)
        {
            if (renderbuffer == null)
            {
                throw new ArgumentNullException("renderbuffer");
            }

            int attachmentWidth = renderbuffer.getWidth();
            int attachmentHeight = renderbuffer.getHeight();
            validateDimensions(attachmentWidth, attachmentHeight, remainingAttachmentCount(attachmentPoint));
            checkColorAttachmentCount(backend != null ? backend : GLCaps.getBackendInstance(),
                                      attachmentPoint, isColorAttachment(attachmentPoint));

            removeAttachment(attachmentPoint);

            Attachment attachment = new Attachment();
            attachment.type = Attachment.Type.RENDERBUFFER;
            attachment.renderbuffer = renderbuffer;
            attachmentTable[attachmentPoint] = attachment;
            ++numAttachments;

            if (numAttachments == 1 && !sizeIsExplicit)
            {
                m_width = attachmentWidth;
                m_height = attachmentHeight;
            }

            applyAttachment(backend != null ? backend : GLCaps.getBackendInstance(), attachmentPoint, attachment);
            updateDrawBuffer(backend != null ? backend : GLCaps.getBackendInstance());
        }

        public void detach(AttachmentPoint attachmentPoint)
        {
            removeAttachment(attachmentPoint);
        }

        public void detachTexture(AttachmentPoint attachmentPoint)
        {
            Attachment attachment;
            if (attachmentTable.TryGetValue(attachmentPoint, out attachment) &&
                attachment.type == Attachment.Type.TEXTURE)
            {
                removeAttachment(attachmentPoint);
            }
        }

        public void detachRenderbuffer(AttachmentPoint attachmentPoint)
        {
            Attachment attachment;
            if (attachmentTable.TryGetValue(attachmentPoint, out attachment) &&
                attachment.type == Attachment.Type.RENDERBUFFER)
            {
                removeAttachment(attachmentPoint);
            }
        }

        public void detachAll()
        {
            List<AttachmentPoint> points = new List<AttachmentPoint>();
            foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
            {
                points.Add(pair.Key);
            }
            for (int i = 0; i < points.Count; ++i)
            {
                removeAttachment(points[i]);
            }
        }

        private void removeAttachment(AttachmentPoint attachmentPoint)
        {
            Attachment attachment;
            if (!attachmentTable.TryGetValue(attachmentPoint, out attachment))
            {
                return;
            }

            if (attachment.type == Attachment.Type.TEXTURE && attachment.hadAutoMipMap && attachment.texture != null)
            {
                attachment.texture.setAutoMipMap(true);
            }

            attachmentTable.Remove(attachmentPoint);
            --numAttachments;

            detachFromBackend(attachmentPoint, attachment);
            updateDrawBuffer(backend);

            if (numAttachments == 0 && !sizeIsExplicit)
            {
                m_width = 0;
                m_height = 0;
            }
        }

        private int remainingAttachmentCount(AttachmentPoint attachmentPoint)
        {
            if (attachmentTable.ContainsKey(attachmentPoint))
            {
                return numAttachments - 1;
            }
            return numAttachments;
        }

        private void validateDimensions(int attachmentWidth, int attachmentHeight, int otherAttachmentCount)
        {
            if (otherAttachmentCount <= 0 && !sizeIsExplicit)
            {
                return;
            }
            if (attachmentWidth != m_width || attachmentHeight != m_height)
            {
                throw new InvalidOperationException(
                    "All attachments bound to a Framebuffer must have identical dimensions!");
            }
        }

        /**
         Gets the OpenGL ID of the framebuffer object.
         */
        public int openGLID()
        {
            return framebufferID;
        }

        /**
         Gets the OpenGL ID of the framebuffer object.
         */
        public int width()
        {
            return m_width;
        }

        /**
         Gets the OpenGL ID of the framebuffer object.
         */
        public int height()
        {
            return m_height;
        }

        public int getWidth()
        {
            return m_width;
        }

        public int getHeight()
        {
            return m_height;
        }

        public Rect2D rect2DBounds()
        {
            return Rect2D.xywh(0.0f, 0.0f, (float)m_width, (float)m_height);
        }

        public string name()
        {
            return m_name;
        }

        public string getName()
        {
            return m_name;
        }

        public TextureFormat format()
        {
            return m_format;
        }

        public int getNumAttachments()
        {
            return numAttachments;
        }

        public bool isComplete()
        {
            string whyNot;
            return isComplete(out whyNot);
        }

        public bool isComplete(out string whyNot)
        {
            whyNot = "";

            if (numAttachments == 0)
            {
                whyNot = "Framebuffer Incomplete: Missing attachment.";
                return false;
            }

            if (m_width <= 0 || m_height <= 0)
            {
                whyNot = "Framebuffer Incomplete: The framebuffer has no size.";
                return false;
            }

            TextureFormat colorFormat = null;
            foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
            {
                Attachment attachment = pair.Value;
                int attachmentWidth;
                int attachmentHeight;
                TextureFormat attachmentFormat;

                if (attachment.type == Attachment.Type.TEXTURE)
                {
                    if (attachment.texture == null)
                    {
                        whyNot = "Framebuffer Incomplete: Incomplete Attachment.";
                        return false;
                    }
                    attachmentWidth = attachment.texture.texelWidth();
                    attachmentHeight = attachment.texture.texelHeight();
                    attachmentFormat = attachment.texture.getFormat();
                }
                else
                {
                    if (attachment.renderbuffer == null)
                    {
                        whyNot = "Framebuffer Incomplete: Incomplete Attachment.";
                        return false;
                    }
                    attachmentWidth = attachment.renderbuffer.getWidth();
                    attachmentHeight = attachment.renderbuffer.getHeight();
                    attachmentFormat = attachment.renderbuffer.getFormat();
                }

                if (attachmentWidth != m_width || attachmentHeight != m_height)
                {
                    whyNot = "Framebuffer Incomplete: Attached images must have same dimensions.";
                    return false;
                }

                if (isColorAttachment(pair.Key))
                {
                    if (colorFormat == null)
                    {
                        colorFormat = attachmentFormat;
                    }
                    else if (colorFormat != attachmentFormat)
                    {
                        whyNot = "Framebuffer Incomplete: Attached images must have same format.";
                        return false;
                    }
                }
            }

            return true;
        }

        /**
         Checks to see if the framebuffer is complete.

         NOTE: This function is meant to be called by RenderDevice through currentFramebufferComplete.
         The results are unpredictable if this function is directly called by the user.

         @param whyNot Defined when the test fails.  Will contain the reason for failure.

         @return true   If complete framebuffer.
         @return false  If incomplete or error.
         */
        internal bool debugIsComplete(out string whyNot)
        {
            if (!isComplete(out whyNot))
            {
                return false;
            }

            if (backend != null && framebufferID != 0)
            {
                if (!backend.CheckFramebufferStatus())
                {
                    whyNot = "Framebuffer Incomplete: Unknown error.";
                    return false;
                }
            }

            return true;
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

            if (framebufferID == 0)
            {
                framebufferID = targetBackend.CreateFramebuffer();
                backend = targetBackend;
            }
            else if (backend == null)
            {
                backend = targetBackend;
            }
            else if (backend != targetBackend)
            {
                throw new InvalidOperationException("The framebuffer was allocated by a different graphics backend.");
            }

            checkColorAttachmentCount(targetBackend);
            targetBackend.BindFramebuffer(framebufferID);
            applyAllAttachments(targetBackend);
            updateDrawBuffer(targetBackend);
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
            targetBackend.BindFramebuffer(0);
        }

        public void readPixels(int x, int y, int width, int height, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "The readback region must have a positive size.");
            }

            IGraphicsBackend targetBackend = GLCaps.getBackendInstance();
            if (targetBackend == null)
            {
                throw new InvalidOperationException(
                    "No graphics backend is available. Set the backend with GLCaps.setBackend before reading pixels.");
            }

            int previousFramebuffer = targetBackend.GetBoundFramebuffer();
            bind(targetBackend);
            try
            {
                targetBackend.ReadPixels(x, y, width, height, data);
            }
            finally
            {
                targetBackend.BindFramebuffer(previousFramebuffer);
            }
        }

        public void readDepth(int x, int y, int width, int height, float[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "The readback region must have a positive size.");
            }

            IGraphicsBackend targetBackend = GLCaps.getBackendInstance();
            if (targetBackend == null)
            {
                throw new InvalidOperationException(
                    "No graphics backend is available. Set the backend with GLCaps.setBackend before reading the depth buffer.");
            }

            int previousFramebuffer = targetBackend.GetBoundFramebuffer();
            bind(targetBackend);
            try
            {
                targetBackend.ReadDepth(x, y, width, height, data);
            }
            finally
            {
                targetBackend.BindFramebuffer(previousFramebuffer);
            }
        }

        internal List<TextureAttachment> getTextureAttachments()
        {
            List<TextureAttachment> attachments = new List<TextureAttachment>();
            foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
            {
                if (pair.Value.type == Attachment.Type.TEXTURE)
                {
                    TextureAttachment attachment = new TextureAttachment();
                    attachment.attachmentPoint = pair.Key;
                    attachment.texture = pair.Value.texture;
                    attachment.mipLevel = pair.Value.mipLevel;
                    attachment.cubeFace = pair.Value.cubeFace;
                    attachments.Add(attachment);
                }
            }
            return attachments;
        }

        internal List<RenderbufferAttachment> getRenderbufferAttachments()
        {
            List<RenderbufferAttachment> attachments = new List<RenderbufferAttachment>();
            foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
            {
                if (pair.Value.type == Attachment.Type.RENDERBUFFER)
                {
                    RenderbufferAttachment attachment = new RenderbufferAttachment();
                    attachment.attachmentPoint = pair.Key;
                    attachment.renderbuffer = pair.Value.renderbuffer;
                    attachments.Add(attachment);
                }
            }
            return attachments;
        }

        public int getFramebufferID()
        {
            return framebufferID;
        }

        internal void setFramebufferID(int id)
        {
            framebufferID = id;
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

        private static AttachmentPoint colorPoint(int index)
        {
            if (index < 0 || index > 15)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return (AttachmentPoint)(0x8CE0 + index);
        }

        private static bool isColorAttachment(AttachmentPoint attachmentPoint)
        {
            int value = (int)attachmentPoint;
            return value >= (int)AttachmentPoint.COLOR_ATTACHMENT0 &&
                   value <= (int)AttachmentPoint.COLOR_ATTACHMENT15;
        }

        private void checkColorAttachmentCount(IGraphicsBackend targetBackend)
        {
            checkColorAttachmentCount(targetBackend, null, false);
        }

        private void checkColorAttachmentCount(IGraphicsBackend targetBackend, AttachmentPoint? replacedPoint, bool addsColorAttachment)
        {
            if (targetBackend == null)
            {
                return;
            }

            int colorAttachmentCount = 0;
            foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
            {
                if (replacedPoint.HasValue && pair.Key == replacedPoint.Value)
                {
                    continue;
                }
                if (isColorAttachment(pair.Key))
                {
                    ++colorAttachmentCount;
                }
            }

            if (addsColorAttachment)
            {
                ++colorAttachmentCount;
            }

            if (colorAttachmentCount > 1 && targetBackend.Kind == GraphicsBackendKind.Direct3D11)
            {
                throw new NotSupportedException(
                    "The Direct3D 11 backend supports only one color render target per framebuffer; " +
                    "multiple color attachments are not supported.");
            }
        }

        private void updateDrawBuffer(IGraphicsBackend targetBackend)
        {
            if (framebufferID == 0 || targetBackend == null || targetBackend.Kind != GraphicsBackendKind.OpenGL)
            {
                return;
            }

            int firstColorAttachment = -1;
            foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
            {
                if (isColorAttachment(pair.Key))
                {
                    int index = (int)pair.Key - (int)AttachmentPoint.COLOR_ATTACHMENT0;
                    if (firstColorAttachment < 0 || index < firstColorAttachment)
                    {
                        firstColorAttachment = index;
                    }
                }
            }

            if (firstColorAttachment < 0)
            {
                return;
            }

            targetBackend.SetDrawBuffer(
                (RenderDevice.Buffer)((int)RenderDevice.Buffer.BUFFER_COLOR0 + firstColorAttachment));
        }

        private void applyAllAttachments(IGraphicsBackend targetBackend)
        {
            foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
            {
                applyAttachment(targetBackend, pair.Key, pair.Value);
            }
        }

        private void applyAttachment(IGraphicsBackend targetBackend, AttachmentPoint attachmentPoint, Attachment attachment)
        {
            if (framebufferID == 0 || targetBackend == null)
            {
                return;
            }

            if (attachment.type == Attachment.Type.TEXTURE)
            {
                int textureID = attachment.texture.getBackendTextureID(targetBackend);
                if (textureID == 0)
                {
                    throw new InvalidOperationException(
                        "The texture attached to " + attachmentPoint + " has not been uploaded to the graphics backend. " +
                        "Upload it with RenderDevice.allocateTexture(texture) before attaching it to an allocated framebuffer, " +
                        "or use RenderDevice.setFramebuffer(framebuffer), which uploads the attachments and binds the framebuffer.");
                }
                targetBackend.AttachFramebufferTexture(framebufferID, attachmentPoint, textureID,
                                                      attachment.mipLevel, attachment.cubeFace);
            }
            else
            {
                int renderbufferID = attachment.renderbuffer.ensureBackendResource(targetBackend);
                targetBackend.AttachFramebufferRenderbuffer(framebufferID, attachmentPoint, renderbufferID);
            }
        }

        private void detachFromBackend(AttachmentPoint attachmentPoint, Attachment attachment)
        {
            if (framebufferID == 0 || backend == null)
            {
                return;
            }

            if (attachment.type == Attachment.Type.TEXTURE)
            {
                backend.AttachFramebufferTexture(framebufferID, attachmentPoint, 0, 0, -1);
            }
            else
            {
                backend.AttachFramebufferRenderbuffer(framebufferID, attachmentPoint, 0);
            }
        }

        public static uint hashCode(AttachmentPoint attachmentPoint)
        {
            return (uint)(int)attachmentPoint;
        }

        /** Reclaims OpenGL ID.  All buffers/textures are automatically detacted on destruction. */
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (backend != null && framebufferID != 0)
                {
                    backend.DeleteFramebuffer(framebufferID);
                }
                framebufferID = 0;

                foreach (KeyValuePair<AttachmentPoint, Attachment> pair in attachmentTable)
                {
                    Attachment attachment = pair.Value;
                    if (attachment.type == Attachment.Type.TEXTURE && attachment.hadAutoMipMap &&
                        attachment.texture != null)
                    {
                        attachment.texture.setAutoMipMap(true);
                    }
                }
                attachmentTable.Clear();
                numAttachments = 0;
            }
            base.Dispose(disposing);
        }
    }
}
