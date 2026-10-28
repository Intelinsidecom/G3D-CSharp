using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using D3DBuffer = SharpDX.Direct3D11.Buffer;

namespace G3D.Direct3D11
{
    internal sealed class Direct3D11Texture
    {
        public int Id;
        public Texture2D Resource;
        public ShaderResourceView ShaderView;
        public ShaderResourceView ComparisonView;
        public DepthStencilView DepthView;
        public RenderTargetView ColorView;
        public SamplerState Sampler;
        public SamplerState ComparisonSampler;
        public TextureFormat Format;
        public int Width;
        public int Height;
        public Texture.DepthReadMode DepthMode;
        public bool IsDepth;
        public bool IsCube;
        public byte[] InitialData;

        public void Dispose()
        {
            if (ComparisonSampler != null) { ComparisonSampler.Dispose(); ComparisonSampler = null; }
            if (Sampler != null) { Sampler.Dispose(); Sampler = null; }
            if (ComparisonView != null) { ComparisonView.Dispose(); ComparisonView = null; }
            if (ShaderView != null) { ShaderView.Dispose(); ShaderView = null; }
            if (DepthView != null) { DepthView.Dispose(); DepthView = null; }
            if (ColorView != null) { ColorView.Dispose(); ColorView = null; }
            if (Resource != null) { Resource.Dispose(); Resource = null; }
        }
    }

    internal sealed class Direct3D11Framebuffer
    {
        public int Id;
        public RenderTargetView Color;
        public RenderTargetView[] Colors = new RenderTargetView[8];
        public DepthStencilView Depth;
        public Texture2D DepthResource;
        public int Width;
        public int Height;

        public void Dispose()
        {
            if (Color != null) { Color.Dispose(); Color = null; }
            for (int i = 0; i < Colors.Length; ++i)
            {
                if (Colors[i] != null) { Colors[i].Dispose(); Colors[i] = null; }
            }
            if (Depth != null) { Depth.Dispose(); Depth = null; }
        }
    }

    internal sealed class Direct3D11Renderbuffer
    {
        public int Id;
        public Texture2D Resource;
        public DepthStencilView DepthView;
        public RenderTargetView ColorView;
        public int Width;
        public int Height;
        public TextureFormat Format;

        public void Dispose()
        {
            if (DepthView != null) { DepthView.Dispose(); DepthView = null; }
            if (ColorView != null) { ColorView.Dispose(); ColorView = null; }
            if (Resource != null) { Resource.Dispose(); Resource = null; }
        }
    }

    internal sealed class Direct3D11Buffer
    {
        public int Id;
        public D3DBuffer Resource;
        public bool IsVertex;

        public void Dispose()
        {
            if (Resource != null) { Resource.Dispose(); Resource = null; }
        }
    }

    internal sealed class Direct3D11Milestone
    {
        public int Id;
        public D3DBuffer Source;
        public D3DBuffer Readback;

        public void Dispose()
        {
            if (Source != null) { Source.Dispose(); Source = null; }
            if (Readback != null) { Readback.Dispose(); Readback = null; }
        }
    }

    internal sealed class Direct3D11ConstantBuffer
    {
        public string Name;
        public int Slot;
        public int Size;
        public D3DBuffer Buffer;
        public byte[] Data;
        public bool IsVertexStage;

        public void Dispose()
        {
            if (Buffer != null) { Buffer.Dispose(); Buffer = null; }
        }
    }

    internal sealed class Direct3D11UniformSlot
    {
        public Direct3D11ConstantBuffer Buffer;
        public int ByteOffset;
    }

    internal sealed class Direct3D11UniformBinding
    {
        public string Name;
        public int Size;
        public List<Direct3D11UniformSlot> Slots = new List<Direct3D11UniformSlot>();
    }

    internal sealed class Direct3D11VertexAttribute
    {
        public string SemanticName;
        public int SemanticIndex;
        public int Slot;
        public SharpDX.DXGI.Format Format;
    }

    internal sealed class Direct3D11Program
    {
        public int Id;
        public VertexShader Vertex;
        public PixelShader Pixel;
        public InputLayout Layout;
        public ShaderResourceView[] Textures = new ShaderResourceView[16];
        public List<Direct3D11ConstantBuffer> ConstantBuffers = new List<Direct3D11ConstantBuffer>();
        public List<Direct3D11UniformBinding> Uniforms = new List<Direct3D11UniformBinding>();
        public List<Direct3D11VertexAttribute> Attributes = new List<Direct3D11VertexAttribute>();

        public void Dispose()
        {
            if (Layout != null) { Layout.Dispose(); Layout = null; }
            if (Vertex != null) { Vertex.Dispose(); Vertex = null; }
            if (Pixel != null) { Pixel.Dispose(); Pixel = null; }
            foreach (Direct3D11ConstantBuffer buffer in ConstantBuffers) { buffer.Dispose(); }
            ConstantBuffers.Clear();
        }
    }
}
