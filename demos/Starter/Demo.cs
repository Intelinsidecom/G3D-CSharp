/**
  @file Starter/Demo.cs

  Port of the Demo class in demos/main.cpp.

  This simple demo applet uses the debug mode as the regular
  rendering mode so you can fly around the scene.
 */

using System;
using G3D;

namespace Starter
{
    public class Demo : GApplet
    {
        // Add state that should be visible to this applet.
        // If you have multiple applets that need to share
        // state, put it in the App.

        private App app;

        public Demo(App _app)
            : base(_app)
        {
            app = _app;
        }

        public override void onInit()
        {
            // Called before Demo::run() beings
            app.debugCamera.setPosition(new Vector3(0, 2, 10));
            app.debugCamera.lookAt(new Vector3(0, 2, 0));

            base.onInit();
        }

        public override void onCleanup()
        {
            // Called when Demo::run() exits
        }

        public override void onLogic()
        {
            // Add non-simulation game logic and AI code here
        }

        public override void onNetwork()
        {
            // Poll net messages here
        }

        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            // Add physical simulation here.  You can make your time advancement
            // based on any of the three arguments.
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
            LightingParameters lighting = new LightingParameters(G3DGameUnits.toSeconds(11, 00, 00, AMPM.AM));

            app.renderDevice.setProjectionAndCameraMatrix(app.debugCamera);

            // Cyan background
            app.renderDevice.setColorClearValue(new Color4(new Color3(0.1f, 0.5f, 1.0f), 1.0f));

            app.renderDevice.clear(app.sky == null, true, true);
            if (app.sky != null)
            {
                app.sky.pointer().render(rd, lighting);
            }

            // Setup lighting
            app.renderDevice.enableLighting();
            app.renderDevice.setLight(0, GLight.directional(lighting.lightDirection, lighting.lightColor));
            app.renderDevice.setAmbientLightColor(lighting.ambient);

            Draw.axes(new CoordinateFrame(new Vector3(0, 4, 0)), app.renderDevice);

            app.renderDevice.disableLighting();

            if (app.sky != null)
            {
                app.sky.pointer().renderLensFlare(rd, lighting);
            }
        }
    }
}
