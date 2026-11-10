/**
  @file VAR_Demo/Demo.cs

  Port of the Demo class and App class bodies in VAR_Demo/main.cpp.

  Demonstration of begin/end style rendering and VAR (Vertex Arrays) for higher
  performance.  This demo predates the GApp infrastructure; consider
  using it instead of intializing RenderDevice yourself.

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2002-10-22
  @edited  2006-01-29
 */

using System;
using G3D;

namespace VAR_Demo
{
    public class Demo : GApplet
    {
        private const int N = 50;

        // Change this to change the model being rendered.
        private const string modelName = "p51-mustang.ifs";

        private App app;

        // Add state that should be visible to this applet.
        // If you have multiple applets that need to share
        // state, put it in the App.

        public ReferenceCountedPointer<Sky> sky;

        public VARArea varStream;
        public VARArea varStatic;
        public Model model;
        public GameTime gameTime;

        public Demo(App _app)
            : base(_app)
        {
            app = _app;

            // Allocate the two VARAreas used in this demo
            varStatic = VARArea.create(1024 * 640 * 5, VARArea.UsageHint.WRITE_ONCE);
            varStream = VARArea.create(1024 * 1280 * 5, VARArea.UsageHint.WRITE_EVERY_FRAME);
            Debug.debugAssertGLOk();
            Debug.debugAssert(varStatic != null);
            Debug.debugAssert(varStream != null);
        }

        public override void onInit()
        {
            // Called before Demo::run() beings
            app.debugCamera.setPosition(new Vector3(0, 2, 10));
            app.debugCamera.lookAt(new Vector3(0, 2, 0));

            model = new Model(app.dataDir + "ifs/" + modelName, varStatic);
            gameTime = G3DGameUnits.toSeconds(8, 00, 00, AMPM.AM);
            Model.renderMethod = RenderMethod.VARSTATIC;

            sky = Sky.create(app.renderDevice, app.dataDir + "sky/");

            app.debugController.setPosition(new Vector3(-25, 2, 0));
            app.debugController.lookAt(new Vector3(-20, 2.5f, 1));
        }

        public override void onCleanup()
        {
            varStatic.reset();

            app.debugLog.printf("Static VAR peak size was  %d bytes.\n",
                             varStatic.peakAllocatedSize());
            app.debugLog.printf("Streaming VAR peak size was %d bytes.\n",
                             varStream.peakAllocatedSize());

            model = null;
            sky = null;
        }

        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            // Add physical simulation here
            gameTime += 20;
        }

        public override void onUserInput(UserInput ui)
        {
            if (ui.keyPressed(UserInput.KeyCode.ESCAPE))
            {
                // Even when we aren't in debug mode, quit on escape.
                endApplet = true;
                app.endProgram = true;
            }

            if (ui.keyPressed(UserInput.KeyCode.SPACE))
            {
                Model.renderMethod =
                  (RenderMethod)(((int)Model.renderMethod + 1) % (int)RenderMethod.NUM_RENDER_METHODS);
            }
        }

        public override void onGraphics(RenderDevice rd)
        {
            app.renderDevice.clear(sky == null, true, false);
            app.renderDevice.setProjectionAndCameraMatrix(app.debugCamera);
            LightingParameters lighting = new LightingParameters(gameTime, false);

            if (sky != null)
            {
                sky.pointer().render(lighting);
            }

            app.renderDevice.pushState();
                // Setup lighting
                app.renderDevice.setSpecularCoefficient(1.0f);
                app.renderDevice.setShininess(64);
                Debug.debugAssertGLOk();
                app.renderDevice.enableLighting();
                app.renderDevice.setLight(0, GLight.directional(lighting.lightDirection, lighting.lightColor));
                app.renderDevice.setLight(1, GLight.directional(-lighting.lightDirection, Color3.white() * .25f, false));
                app.renderDevice.setAmbientLightColor(lighting.ambient);

                app.renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);

                Color3[] color = new Color3[] { Color3.black(), Color3.white(), Color3.orange(), Color3.blue() };
                double[] t = new double[2];
                Vector3[] c = new Vector3[2];

                t[0] = G3D.System.getTick();
                t[1] = t[0] + 0.01;

                // Draw the planes
                for (int x = 0; x < N; ++x)
                {

                    for (int i = 0; i < 2; ++i)
                    {
                        double a = x + t[i] + 2;
                        double a2 = t[i] * (x + 1) * .01 + 1005.1;
                        c[i] = new Vector3((float)(Math.Cos(a) * (10 + x / 2.0)), (float)(Math.Sin(a2) * 10), (float)(Math.Sin(a) * 15));
                    }

                    CoordinateFrame cframe = new CoordinateFrame(c[0]);

                    double angle = t[0] * (x + 1) * .1;
                    cframe.lookAt(c[1], new Vector3((float)Math.Cos(angle), 3, (float)Math.Sin(angle)).direction());

                    app.renderDevice.setColor(color[x % 4]);
                    model.render(app.renderDevice, cframe, lighting, varStream);
                }
            app.renderDevice.popState();

            if (sky != null)
            {
                sky.pointer().renderLensFlare(lighting);
            }

            app.renderDevice.push2D();
                string str = "";
                switch (Model.renderMethod)
                {
                    case RenderMethod.TRIANGLES:
                        str = "Using begin/end (SPACE to change)";
                        break;

                    case RenderMethod.VARSTREAM:
                        str = "Using streaming vertex array (SPACE to change)";
                        break;

                    case RenderMethod.VARSTATIC:
                        str = "Using static vertex array (SPACE to change)";
                        break;

                    default:;
                        break;
                }

               app.debugFont.pointer().draw2D(str, new Vector2(10, app.renderDevice.getHeight() - 40), 20,
                   new Color4(Color3.yellow(), 1), new Color4(Color3.black(), 1));

            app.renderDevice.pop2D();

            Debug.debugAssertGLOk();
            varStream.reset();
        }
    }
}
