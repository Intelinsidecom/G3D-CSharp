using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace G3D.Direct3D11
{
    internal static class FixedFunctionShaders
    {
        public const string VertexSource = @"
struct VSIn { float3 position : POSITION; float3 normal : NORMAL; float4 color : COLOR; float4 texcoord : TEXCOORD0; };
struct VSOut { float4 position : SV_POSITION; float3 normal : NORMAL; float4 color : COLOR; float4 texcoord : TEXCOORD0; float4 shadowPosition : TEXCOORD1; };
cbuffer Constants : register(b0) { row_major float4x4 MVP; row_major float4x4 LightMVP; float4 LightDirection; float4 LightColor; float4 Ambient; float4 Material; float4 ShadowParams; float4x4 TextureMatrix; };
VSOut main(VSIn input) {
 VSOut output;
 output.position = mul(float4(input.position, 1.0f), MVP);
 output.normal = input.normal;
 output.color = input.color;
 output.texcoord = mul(float4(input.texcoord.xy, 0.0f, 1.0f), TextureMatrix);
 output.shadowPosition = mul(float4(input.position, 1.0f), LightMVP);
 return output;
}";

        public const string FragmentSource = @"
struct PSIn { float4 position : SV_POSITION; float3 normal : NORMAL; float4 color : COLOR; float4 texcoord : TEXCOORD0; float4 shadowPosition : TEXCOORD1; };
Texture2D<float4> DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);
Texture2D<float> ShadowTexture : register(t1);
SamplerComparisonState ShadowSampler : register(s1);
cbuffer Constants : register(b0) { row_major float4x4 MVP; row_major float4x4 LightMVP; float4 LightDirection; float4 LightColor; float4 Ambient; float4 Material; float4 ShadowParams; float4x4 TextureMatrix; };
float4 main(PSIn input) : SV_TARGET {
 float3 n = normalize(input.normal);
 float3 l = normalize(LightDirection.xyz);
 float diffuse = max(dot(n, l), 0.0);
 float3 lit = Ambient.rgb + LightColor.rgb * diffuse * Material.rgb;
 float shadow = 1.0;
 float3 sp = input.shadowPosition.xyz / input.shadowPosition.w;
 float2 uv = sp.xy * float2(0.5, -0.5) + float2(0.5, 0.5);
 if (ShadowParams.x > 0.5 && uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1 && sp.z >= 0 && sp.z <= 1) shadow = ShadowTexture.SampleCmpLevelZero(ShadowSampler, uv, sp.z);
 lit *= shadow;
 return float4(lit * input.color.rgb, input.color.a);
}";
    }
}
