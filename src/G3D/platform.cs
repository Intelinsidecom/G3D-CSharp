/**
  @file platform.h

  #defines for platform specific issues.

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2003-06-09
  @edited  2006-01-16
  */

namespace G3D
{
    public static class Platform
    {
        public const int G3D_VER = 60900;

        public const bool G3D_WIN32 = true;
        public const bool G3D_DEBUG = false;
    }

    // Time types
    public struct RealTime 
    { 
        public double Value; 
        public RealTime(double v) { Value = v; } 
        public static implicit operator double(RealTime r) { return r.Value; } 
        public static implicit operator RealTime(double v) { return new RealTime(v); } 
    }
    
    public struct SimTime 
    { 
        public double Value; 
        public SimTime(double v) { Value = v; } 
        public static implicit operator double(SimTime r) { return r.Value; } 
        public static implicit operator SimTime(double v) { return new SimTime(v); } 
    }
    
    public struct GameTime 
    { 
        public double Value; 
        public GameTime(double v) { Value = v; } 
        public static implicit operator double(GameTime r) { return r.Value; } 
        public static implicit operator GameTime(double v) { return new GameTime(v); } 
    }

    // Reference-counted pointer types - use ReferenceCountedPointer<T> directly
    // typedef ReferenceCountedPointer<Texture> TextureRef;
    // typedef ReferenceCountedPointer<VARArea> VARAreaRef;
    // typedef ReferenceCountedPointer<VAR> VARRef;
    // typedef ReferenceCountedPointer<GFont> GFontRef;
    // typedef ReferenceCountedPointer<Sky> SkyRef;
    // typedef ReferenceCountedPointer<IFSModel> IFSModelRef;
    // typedef ReferenceCountedPointer<PosedModel> PosedModelRef;
    // typedef ReferenceCountedPointer<PosedModel2D> PosedModel2DRef;
    // typedef ReferenceCountedPointer<Lighting> LightingRef;
    // typedef ReferenceCountedPointer<GModule> GModuleRef;
    // typedef ReferenceCountedPointer<GModuleManager> GModuleManagerRef;
}
