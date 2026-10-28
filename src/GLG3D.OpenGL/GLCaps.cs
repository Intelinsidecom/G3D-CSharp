using System;
using System.Collections.Generic;

namespace G3D.OpenGL
{
    internal static class OpenGLCapabilities
    {
        public static readonly HashSet<string> Extensions = new HashSet<string>(StringComparer.Ordinal);
        public static string Version = string.Empty;
        public static string Vendor = string.Empty;
        public static string Renderer = string.Empty;
        public static string DriverVersion = string.Empty;
        public static int MaxTextureUnits;
        public static int MaxTextures;
        public static int MaxTextureCoords;
        public static bool BugMultiTexCoord3fvARB = false;
        public static bool BugNormalMapTexGen = false;
        public static bool BugRedBlueMipmapSwap = false;
        public static bool BugMipmapGeneration = false;
        public static bool BugSlowVBO = false;

        public static void initialize(string version, string vendor, string renderer, string driverVersion, int maxTextureUnits, int maxTextures, int maxTextureCoords)
        {
            Version = version ?? string.Empty;
            Vendor = vendor ?? string.Empty;
            Renderer = renderer ?? string.Empty;
            DriverVersion = driverVersion ?? string.Empty;
            MaxTextureUnits = maxTextureUnits;
            MaxTextures = maxTextures;
            MaxTextureCoords = maxTextureCoords;
        }
    }
}
