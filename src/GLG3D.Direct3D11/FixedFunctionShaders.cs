using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace G3D.Direct3D11
{
    internal static class FixedFunctionShaders
    {
        public const int MaxLights = 8;

        public const string VertexSource = @"
struct VSIn { float3 position : POSITION; float3 normal : NORMAL; float4 color : COLOR; float4 texcoord : TEXCOORD0; };
struct VSOut { float4 position : SV_POSITION; float3 normal : NORMAL; float4 color : COLOR; float4 texcoord : TEXCOORD0; float4 shadowPosition : TEXCOORD1; float4 eyePosition : TEXCOORD2; float3 cubeDir : TEXCOORD3; };
cbuffer Constants : register(b0) { row_major float4x4 MVP; row_major float4x4 LightMVP; row_major float4x4 TextureMatrix; row_major float4x4 ModelView; row_major float4x4 ObjectToWorld; float4x3 NormalMatrix; float4 Ambient; float4 Specular; float4 Shininess; float4 EyePosition; float4 ShadowParams; float4 CombineParams; float4 LightPosition[8]; float4 LightDirection[8]; float4 LightColor[8]; float4 LightAttenuation[8]; };
VSOut main(VSIn input) {
 VSOut output;
 output.position = mul(MVP, float4(input.position, 1.0f));
 output.normal = mul(float4(input.normal, 0.0f), NormalMatrix);
 output.color = input.color;
 output.texcoord = mul(TextureMatrix, float4(input.texcoord.xy, 0.0f, 1.0f));
 output.shadowPosition = mul(LightMVP, mul(ObjectToWorld, float4(input.position, 1.0f)));
 output.eyePosition = mul(ModelView, float4(input.position, 1.0f));
 // GL_NORMAL_MAP texgen generates from the EYE-space normal; the texture matrix
 // (the camera rotation) then maps it to world space for the cube lookup.
 float3 eyeNormal = mul(float4(input.normal, 0.0f), NormalMatrix);
 float3 cubeDir = mul(TextureMatrix, float4(eyeNormal, 0.0f)).xyz;
 // Direct3D cube maps are indexed with a left-handed direction.  Negating X turns
 // the OpenGL lookup vector into the Direct3D one, which swaps the +X and -X faces
 // back to the order G3D uploads them in.
 output.cubeDir = float3(-cubeDir.x, cubeDir.y, cubeDir.z);
 return output;
}";

        public const string FragmentSource = @"
struct PSIn { float4 position : SV_POSITION; float3 normal : NORMAL; float4 color : COLOR; float4 texcoord : TEXCOORD0; float4 shadowPosition : TEXCOORD1; float4 eyePosition : TEXCOORD2; float3 cubeDir : TEXCOORD3; bool frontFacing : SV_IsFrontFace; };
Texture2D<float4> DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);
Texture2D<float> ShadowTexture : register(t1);
SamplerComparisonState ShadowSampler : register(s1);
TextureCube<float4> CubeTexture : register(t2);
SamplerState CubeSampler : register(s2);
cbuffer Constants : register(b0) { row_major float4x4 MVP; row_major float4x4 LightMVP; row_major float4x4 TextureMatrix; row_major float4x4 ModelView; row_major float4x4 ObjectToWorld; float4x3 NormalMatrix; float4 Ambient; float4 Specular; float4 Shininess; float4 EyePosition; float4 ShadowParams; float4 CombineParams; float4 LightPosition[8]; float4 LightDirection[8]; float4 LightColor[8]; float4 LightAttenuation[8]; };
float4 combineTexture(float4 fragment, float4 tex, int mode) {
 if (mode == 0) { return tex; }
 if (mode == 1) { return fragment * (1.0f - tex.a) + tex * tex.a; }
 if (mode == 2) { return fragment * tex + fragment * (1.0f - tex.a) + tex * (1.0f - fragment.a); }
 if (mode == 3) { return fragment + tex; }
 if (mode == 4) { return fragment * tex; }
 if (mode == 5) { return (fragment + tex) - 0.5f; }
 if (mode == 6) { return fragment - tex; }
 if (mode == 7) { float d = dot(fragment.rgb, tex.rgb); return float4(d, d, d, fragment.a * tex.a); }
 if (mode == 8) { float d = dot(fragment, tex); return float4(d, d, d, d); }
 return fragment * tex;
}
float4 main(PSIn input) : SV_TARGET {
  float3 n = normalize(input.normal);
  if (Shininess.y > 0.5f && !input.frontFacing) { n = -n; }
  float4 fragment = input.color;
  if (Shininess.z > 0.5f) {
   float3 lit = Ambient.rgb * input.color.rgb;
   // GL_LIGHT_MODEL_LOCAL_VIEWER defaults to FALSE, so the fixed-function
   // viewer sits at infinity and the eye-space view vector is the constant
   // +Z axis.  Using the per-fragment eye position would add specular
   // highlights that OpenGL does not produce.
   float3 view = float3(0.0f, 0.0f, 1.0f);
   int lightMask = (int)EyePosition.w;
   for (int i = 0; i < 8; ++i) {
    if ((lightMask & (1 << i)) == 0) { continue; }
    float4 lightPosition = LightPosition[i];
    float4 lightColor = LightColor[i];
    float4 attenuation = LightAttenuation[i];
    float flags = lightColor.a;
    bool diffuseOn = flags >= 1.0f;
    bool specularOn = flags >= 2.0f;
    float3 toLight;
    float atten = 1.0f;
    if (lightPosition.w == 0.0f) {
     toLight = normalize(lightPosition.xyz);
    } else {
     float3 delta = lightPosition.xyz - input.eyePosition.xyz;
     float distance = length(delta);
     toLight = delta / max(distance, 1e-6f);
     atten = 1.0f / max(attenuation.x + attenuation.y * distance + attenuation.z * distance * distance, 1e-6f);
     float cutoff = LightDirection[i].w;
     if (cutoff < 180.0f) {
      float cosAngle = dot(-toLight, normalize(LightDirection[i].xyz));
      if (cosAngle < cos(cutoff * 0.0174532925f)) { atten = 0.0f; }
     }
    }
    float diffuse = max(dot(n, toLight), 0.0f) * atten;
    if (diffuseOn) { lit += lightColor.rgb * diffuse * input.color.rgb; }
    if (specularOn) {
     float3 halfVector = normalize(toLight + view);
     // GL fixed-function lighting evaluates (N dot H)^0 as 1, but HLSL pow(0, 0)
     // is NaN; the multiply below would turn the whole lit color NaN and black
     // out every fragment whose normal faces away from the half-vector.
     float nh = max(dot(n, halfVector), 0.0f);
     float specular = (Shininess.x == 0.0f ? 1.0f : pow(nh, Shininess.x)) * atten;
     lit += lightColor.rgb * Specular.rgb * specular;
    }
   }
   fragment = float4(lit, input.color.a);
  }
  if (Shininess.w > 0.5f) {
   float4 tex = CombineParams.w > 0.5f
    ? CubeTexture.Sample(CubeSampler, input.cubeDir)
    : DiffuseTexture.Sample(DiffuseSampler, input.texcoord.xy);
   fragment = combineTexture(fragment, tex, (int)CombineParams.x);
  }
  int alphaFunction = (int)CombineParams.y;
  float alphaReference = CombineParams.z;
  if (alphaFunction == 1) { if (!(fragment.a > alphaReference)) discard; }
  else if (alphaFunction == 2) { if (!(fragment.a < alphaReference)) discard; }
  else if (alphaFunction == 3) { if (!(fragment.a >= alphaReference)) discard; }
  else if (alphaFunction == 4) { if (!(fragment.a <= alphaReference)) discard; }
  else if (alphaFunction == 5) { if (fragment.a == alphaReference) discard; }
  else if (alphaFunction == 6) { if (fragment.a != alphaReference) discard; }
  else if (alphaFunction == 8) { discard; }
  float shadow = 1.0f;
  float3 sp = input.shadowPosition.xyz / input.shadowPosition.w;
  float2 uv = sp.xy * float2(0.5, -0.5) + float2(0.5, 0.5);
  if (ShadowParams.x > 0.5f && uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1 && sp.z >= 0 && sp.z <= 1) shadow = ShadowTexture.SampleCmpLevelZero(ShadowSampler, uv, sp.z - 1e-6f);
  fragment.rgb *= shadow;
  return fragment;
}";
    }
}