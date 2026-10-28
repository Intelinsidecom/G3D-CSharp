using System;
using System.Collections.Generic;

namespace G3D
{
    public sealed class BackendCaps
    {
        public int MaxTextureSize;
        public int MaxTextureUnits;
        public int MaxTextures;
        public int MaxTextureCoords;
        public int MaxLights;
        public int MaxColorAttachments;
        public bool SupportsShadows;
        public bool SupportsCubeMaps;
        public bool SupportsStencil;
        public bool SupportsTwoSidedStencil;
        public bool SupportsVertexProgram;
        public bool SupportsVertexProgramNV2;
        public bool SupportsPixelProgram;
        public bool SupportsVertexBufferObject;
        public bool SupportsTextureRectangle;
        public bool SupportsAlphaTest;
        public bool SupportsTextureCombineModes;
        public bool SupportsTextureLODBias;
        public bool SupportsGenericVertexAttributes;
        public bool SupportsStereoDrawBuffers;
        public bool SupportsReflectionMaps;
        public bool HasBug_glMultiTexCoord3fvARB;
        public bool HasBug_normalMapTexGen;
        public bool HasBug_redBlueMipmapSwap;
        public bool HasBug_mipmapGeneration;
        public bool HasBug_slowVBO;
        public string Renderer;
        public string Vendor;
        public string Version;
        public string DriverVersion;
        public ulong VideoMemorySize;
        public IList<string> Extensions;
        public IList<GraphicsShaderLanguage> ShaderLanguages;

        public BackendCaps()
        {
            MaxTextureSize = 0;
            MaxTextureUnits = 0;
            MaxTextures = 0;
            MaxTextureCoords = 0;
            MaxLights = 0;
            MaxColorAttachments = 0;
            SupportsShadows = false;
            SupportsCubeMaps = false;
            SupportsStencil = false;
            SupportsTwoSidedStencil = false;
            SupportsVertexProgram = false;
            SupportsVertexProgramNV2 = false;
            SupportsPixelProgram = false;
            SupportsVertexBufferObject = false;
            SupportsTextureRectangle = false;
            SupportsAlphaTest = false;
            SupportsTextureCombineModes = false;
            SupportsTextureLODBias = false;
            SupportsGenericVertexAttributes = false;
            SupportsStereoDrawBuffers = false;
            SupportsReflectionMaps = false;
            HasBug_glMultiTexCoord3fvARB = false;
            HasBug_normalMapTexGen = false;
            HasBug_redBlueMipmapSwap = false;
            HasBug_mipmapGeneration = false;
            HasBug_slowVBO = false;
            Renderer = string.Empty;
            Vendor = string.Empty;
            Version = string.Empty;
            DriverVersion = string.Empty;
            VideoMemorySize = 0;
            Extensions = new List<string>();
            ShaderLanguages = new List<GraphicsShaderLanguage>();
        }
    }
}
