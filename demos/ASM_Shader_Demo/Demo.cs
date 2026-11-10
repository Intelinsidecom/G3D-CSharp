/**
  @file ASM_Shader_Demo/Demo.cs

  Port of the Demo class in ASM_Shader_Demo/main.cpp.

  This program demonstrates how to use assembly-level shaders with
  G3D::VertexProgram and G3D::PixelProgram.  This API is provided for
  backwards compatibility only; consider using high-level shaders
  with G3D::Shader.
 */

using System;
using G3D;

namespace ASM_Shader_Demo
{
    public class Demo : GApplet
    {
        private App app;

        public Model model;
        public GameTime gameTime;

        public VertexProgram distort;

        public Demo(App _app)
            : base(_app)
        {
            app = _app;
        }

        public override void onInit()
        {
            model = new Model(app.dataDir + "ifs/cow.ifs");

            app.debugController.setMoveRate(1);

            app.debugController.setPosition(new Vector3(2, 0.2f, -2));
            app.debugController.lookAt(new Vector3(-2, 0, 2));
            app.renderDevice.setColorClearValue(new Color4(new Color3(0.1f, 0.5f, 1.0f), 1));

            string p = "ASM_Shader_Demo/";
            if (!System.IO.File.Exists(p + "twist.vp"))
            {
                p = "";
            }
            distort = VertexProgram.fromFile(p + "twist.vp");
        }

        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            // Simulation
            app.debugController.doSimulation(Math.Max(0.1, Math.Min(0.0, (double)rdt)));
            app.debugCamera.setCoordinateFrame(app.debugController.getCoordinateFrame());
        }

        public override void onGraphics(RenderDevice rd)
        {
            rd.setAmbientLightColor(Color3.white() * .5f);
            rd.clear(true, true, true);

            rd.pushState();
            rd.setProjectionAndCameraMatrix(app.debugCamera);
            double angle = Math.Cos(G3D.System.getTick()) / 2;

            // The original passed this straight to
            // glProgramLocalParameter4fARB; the port exposes the same call
            // through GPUProgram.setLocalParameter.
            distort.setLocalParameter(0, new Vector4((float)Math.Cos(angle), 1, (float)Math.Sin(angle), 1));

            rd.setVertexProgram(distort);

            // Set depth buffer
            rd.disableColorWrite();
            model.render(CoordinateFrame.identity(), new LightingParameters(G3DGameUnits.toSeconds(11, 00, 00, AMPM.AM)));
            rd.enableColorWrite();

            // Draw translucent
            rd.setDepthTest(RenderDevice.DepthTest.DEPTH_ALWAYS_PASS);
            rd.disableDepthWrite();
            rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ONE);
            model.render(CoordinateFrame.identity(), new LightingParameters(G3DGameUnits.toSeconds(11, 00, 00, AMPM.AM)));

            // Wireframe
            rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ZERO);
            rd.setRenderMode(RenderDevice.RenderMode.RENDER_WIREFRAME);
            rd.setPolygonOffset(-.25);
            rd.setColor(Color3.black());
            rd.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
            model.render(CoordinateFrame.identity(), new LightingParameters(G3DGameUnits.toSeconds(11, 00, 00, AMPM.AM)));
            rd.setRenderMode(RenderDevice.RenderMode.RENDER_SOLID);
            rd.popState();

            Draw.axes(rd);
        }

        public override void onLogic()
        {
            if (app.userInput.keyPressed(UserInput.KeyCode.ESCAPE))
            {
                app.endProgram = true;
                endApplet = true;
            }
        }
    }
}
