/**
  @file GLCaps.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2004-03-28
  @edited  2006-02-28

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
*/

using System;

namespace G3D
{
    /**
      Low-level wrapper for OpenGL extension management.
      Can be used without G3D::RenderDevice to load and
      manage extensions.

      OpenGL has a base API and an extension API.  All OpenGL drivers
      must support the base API.  The latest features may not
      be supported by some drivers, so they are in the extension API
      and are dynamically loaded at runtime using GLCaps::loadExtensions.
      Before using a specific extension you must test for its presence
      using the GLCaps::supports method.

      For convenience, frequently used extensions have fast tests, e.g.,
      GLCaps::supports_GL_EXT_texture_rectangle.

      Note that GL_NV_texture_rectangle and GL_EXT_texture_rectangle
      have exactly the same constants, so supports_GL_EXT_texture_rectangle
      returns true if either is supported.

      GLCaps assumes all OpenGL contexts have the same capabilities.
    */
    public static class GLCaps
    {
        public const int G3D_MAX_TEXTURE_UNITS = 8;

        private static IGraphicsBackend backend;

        public static void setBackend(IGraphicsBackend value)
        {
            backend = value;
        }

        internal static IGraphicsBackend getBackendInstance()
        {
            return backend;
        }

        private static IGraphicsBackend requireBackend()
        {
            if (backend == null)
            {
                throw new InvalidOperationException("GLCaps has not been initialized.");
            }
            return backend;
        }

        public static void init()
        {
            requireBackend();
        }

        public static void loadExtensions()
        {
            init();
        }

        public static void loadExtensions(Log debugLog)
        {
            init();
        }

        public static bool supports(string extName)
        {
            if (extName == null)
            {
                return false;
            }
            return requireBackend().SupportsExtension(extName);
        }

        public static bool supports(TextureFormat fmt)
        {
            if (fmt == null)
            {
                return false;
            }
            return requireBackend().SupportsTextureFormat(fmt);
        }

        public static string glVersion()
        {
            return requireBackend().GetGLVersion();
        }

        public static string driverVersion()
        {
            return requireBackend().GetDriverVersion();
        }

        public static string vendor()
        {
            return requireBackend().GetVendor();
        }

        public static string renderer()
        {
            return requireBackend().GetRenderer();
        }

        public static bool supports_two_sided_stencil()
        {
            return requireBackend().SupportsTwoSidedStencil();
        }

        public static int numTextureCoords()
        {
            return requireBackend().GetMaxTextureCoords();
        }

        public static int numTextures()
        {
            return requireBackend().GetMaxTextures();
        }

        public static int numTextureUnits()
        {
            return requireBackend().GetMaxTextureUnits();
        }

        public static bool supports_GL_ARB_texture_non_power_of_two() { return supports("GL_ARB_texture_non_power_of_two"); }
        public static bool supports_GL_EXT_texture_rectangle() { return supports("GL_EXT_texture_rectangle") || supports("GL_NV_texture_rectangle"); }
        public static bool supports_GL_ARB_vertex_program() { return supports("GL_ARB_vertex_program"); }
        public static bool supports_GL_NV_vertex_program2() { return supports("GL_NV_vertex_program2"); }
        public static bool supports_GL_ARB_vertex_buffer_object() { return supports("GL_ARB_vertex_buffer_object"); }
        public static bool supports_GL_ARB_fragment_program() { return supports("GL_ARB_fragment_program"); }
        public static bool supports_GL_ARB_multitexture() { return supports("GL_ARB_multitexture"); }
        public static bool supports_GL_EXT_texture_edge_clamp() { return supports("GL_EXT_texture_edge_clamp"); }
        public static bool supports_GL_ARB_texture_border_clamp() { return supports("GL_ARB_texture_border_clamp"); }
        public static bool supports_GL_EXT_texture_3D() { return supports("GL_EXT_texture_3D"); }
        public static bool supports_GL_EXT_stencil_wrap() { return supports("GL_EXT_stencil_wrap"); }
        public static bool supports_GL_EXT_stencil_two_side() { return supports("GL_EXT_stencil_two_side"); }
        public static bool supports_GL_ATI_separate_stencil() { return supports("GL_ATI_separate_stencil"); }
        public static bool supports_GL_EXT_texture_compression_s3tc() { return supports("GL_EXT_texture_compression_s3tc"); }
        public static bool supports_GL_EXT_texture_cube_map() { return supports("GL_EXT_texture_cube_map"); }
        public static bool supports_GL_ARB_texture_cube_map() { return supports_GL_EXT_texture_cube_map(); }
        public static bool supports_GL_ARB_shadow() { return supports("GL_ARB_shadow"); }
        public static bool supports_GL_ARB_shader_objects() { return supports("GL_ARB_shader_objects"); }
        public static bool supports_GL_ARB_shading_language_100() { return supports("GL_ARB_shading_language_100"); }
        public static bool supports_GL_ARB_fragment_shader() { return supports("GL_ARB_fragment_shader"); }
        public static bool supports_GL_ARB_vertex_shader() { return supports("GL_ARB_vertex_shader"); }
        public static bool supports_GL_EXT_framebuffer_object() { return supports("GL_EXT_framebuffer_object"); }

        public static bool hasBug_glMultiTexCoord3fvARB()
        {
            return requireBackend().HasBug_glMultiTexCoord3fvARB();
        }

        public static bool hasBug_normalMapTexGen()
        {
            return requireBackend().HasBug_normalMapTexGen();
        }

        public static bool hasBug_redBlueMipmapSwap()
        {
            return requireBackend().HasBug_redBlueMipmapSwap();
        }

        public static bool hasBug_mipmapGeneration()
        {
            return requireBackend().HasBug_mipmapGeneration();
        }

        public static bool hasBug_slowVBO()
        {
            return requireBackend().HasBug_slowVBO();
        }

        public static void describeSystem(RenderDevice rd, NetworkDevice nd, TextOutput t)
        {
            System.describeSystem(t);
            if (rd != null)
            {
                rd.describeSystem(t);
            }
            if (nd != null)
            {
                nd.describeSystem(t);
            }
        }

        public static void describeSystem(RenderDevice rd, NetworkDevice nd, ref string s)
        {
            TextOutput t = new TextOutput(new TextOutput.Options());
            describeSystem(rd, nd, t);
            t.commitString(ref s);
        }
    }
}
