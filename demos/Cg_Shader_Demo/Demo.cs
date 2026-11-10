/**
  @file Cg_Shader_Demo/Demo.cs

  Port of the Demo class in Cg_Shader_Demo/main.cpp.

  This demo shows how to use NVIDIA's Cg shading language
  with VertexProgram and PixelProgram.  The Visual Studio
  workspace has been configured to compile the *.cg files
  to OpenGL assembly.

  The effect shown is an implementation of parallax
  bump mapping with per-pixel shading and normal maps.
 */

using System;
using G3D;

namespace Cg_Shader_Demo
{
    public class Demo : GApplet
    {
        private App app;

        public Mesh model = new Mesh();

        public ReferenceCountedPointer<Texture> texture;

        /**
          Tangent space normal map with the bump map in
          the alpha channel.
         */
        public ReferenceCountedPointer<Texture> normalMap;

        public PixelProgram parallaxPP;
        public VertexProgram parallaxVP;

        public Demo(App _app)
            : base(_app)
        {
            app = _app;

            texture = new ReferenceCountedPointer<Texture>(Texture.fromFile("texture.jpg"));

            GImage bump = new GImage("bump.jpg");
            GImage normal = new GImage();

            GImage.computeNormalMap(bump, normal);

            normalMap = new ReferenceCountedPointer<Texture>(
                Texture.fromGImage("Normal Map", normal, null, Texture.WrapMode.TILE,
                    Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D,
                    Texture.DepthReadMode.DEPTH_NORMAL, 2.0f));

            parallaxPP = PixelProgram.fromFile("parallaxPP.pp");
            parallaxVP = VertexProgram.fromFile("parallaxVP.vp");
        }

        public override void onInit()
        {
            // Called before Demo::run() beings
            app.debugCamera.setPosition(new Vector3(0, 4, 3));
            app.debugCamera.lookAt(new Vector3(0, 1, 0));
        }

        public override void onUserInput(UserInput ui)
        {
            if (ui.keyPressed(UserInput.KeyCode.ESCAPE))
            {
                // Even when we aren't in debug mode, quit on escape.
                endApplet = true;
                app.endProgram = true;
            }

            // Add other key handling here
        }

        public override void onGraphics(RenderDevice rd)
        {
            LightingParameters lighting = new LightingParameters(G3DGameUnits.toSeconds(2, 00, 00, AMPM.AM), false);
            rd.setProjectionAndCameraMatrix(app.debugCamera);

            // Cyan background
            rd.setColorClearValue(new Color4(new Color3(0.1f, 0.5f, 1.0f), 1));

            rd.clear(app.sky == null, true, true);
            if (app.sky != null)
            {
                app.sky.pointer().render(lighting);
            }

            // Create a light
            Vector4 wsLight = new Vector4(1.0f, 2.5f, 2.0f, 1.0f);

            // Setup lighting
            rd.enableLighting();
            rd.setLight(0, GLight.directional(lighting.lightDirection, lighting.lightColor));
            rd.setAmbientLightColor(lighting.ambient);

            CoordinateFrame cframe = CoordinateFrame.identity();
            // Rotate the quad
            cframe.rotation = Matrix3.fromAxisAngle(Vector3.unitY(), (float)(G3D.System.getTick() * .1));

            rd.pushState();
            GPUProgram.ArgList vertexArgs = new GPUProgram.ArgList();

            rd.setObjectToWorldMatrix(cframe);

            // Take the light to object space
            Vector4 osLight = cframe.toObjectSpace(wsLight);

            // Take the viewer to object space
            Vector3 osEye = cframe.pointToObjectSpace(app.debugCamera.getCoordinateFrame().translation);

            vertexArgs.set("MVP", rd.getModelViewProjectionMatrix());
            vertexArgs.set("osLight", osLight);
            vertexArgs.set("osEye", osEye);
            rd.setVertexProgram(parallaxVP, vertexArgs);

            GPUProgram.ArgList pixelArgs = new GPUProgram.ArgList();
            pixelArgs.set("texture", texture.pointer());
            pixelArgs.set("normalMap", normalMap.pointer());
            rd.setPixelProgram(parallaxPP, pixelArgs);

            model.render(rd);
            rd.popState();

            rd.disableLighting();

            // The original passed Color4::clear() as the wire colour to draw
            // only the solid sphere.
            Draw.sphere(new Sphere(wsLight.xyz(), .1f), rd, new Color4(Color3.white(), 1), new Color4(Color3.white(), 0));

            if (app.sky != null)
            {
                app.sky.pointer().renderLensFlare(lighting);
            }

            rd.push2D();
            app.debugFont.pointer().draw2D("The surface is a single quad textured with parallax bump mapping and per-pixel shading.", new Vector2(10, 10), 10,
                new Color4(Color3.white(), 1), new Color4(Color3.black(), 1));
            app.debugFont.pointer().draw2D("Press TAB to toggle to first person camera controls.", new Vector2(10, 30), 10,
                new Color4(Color3.white(), 1), new Color4(Color3.black(), 1));
            rd.pop2D();
        }
    }
}
