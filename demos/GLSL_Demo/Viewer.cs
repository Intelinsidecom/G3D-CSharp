/**
  @file GLSL_Demo/Viewer.cs

  Port of the Viewer class in GLSL_Demo/header.h and GLSL_Demo/Viewer.cpp.
 */

using System;
using G3D;

namespace GLSL_Demo
{
    public class Viewer : GApplet
    {
        private App app;

        public Array<ReferenceCountedPointer<Entity>> entityArray = new Array<ReferenceCountedPointer<Entity>>();
        public float bumpScale;

        public Viewer(App _app)
            : base(_app)
        {
            app = _app;
        }

        public override void onInit()
        {
            app.debugCamera.setPosition(new Vector3(0, 0, 4));
            app.debugCamera.lookAt(Vector3.zero());

            entityArray.append(Entity.create(Mesh.quad().pointer(), CoordinateFrame.identity()));
            bumpScale = 0.04f;

            app.debugLog.println("Done Viewer::init");
        }

        public override void onSimulation(RealTime rdt, SimTime dt, SimTime idt)
        {
            double t = G3D.System.getTick();
            for (int e = 0; e < entityArray.size(); ++e)
            {
                ReferenceCountedPointer<Entity> entity = entityArray[e];

                double a = t + e;

                Vector3 target =
                    entity.pointer().cframe.translation +
                    new Vector3((float)(Math.Cos(a) * .5), -1, (float)(-1 + Math.Sin(a) * .5));

                entity.pointer().cframe.lookAt(target);
            }
        }

        public override void onUserInput(UserInput ui)
        {
            if (ui.keyPressed(UserInput.KeyCode.ESCAPE))
            {
                // Even when we aren't in debug mode, quit on escape.
                endApplet = true;
                app.endProgram = true;
            }

            if (ui.keyPressed(UserInput.KeyCode.P))
            {
                // Toggle parallax
                if (bumpScale > 0)
                {
                    bumpScale = 0.0f;
                }
                else
                {
                    bumpScale = 0.05f;
                }
            }
        }

        public override void onGraphics(RenderDevice rd)
        {
            LightingParameters lighting = new LightingParameters(G3DGameUnits.toSeconds(11, 00, 00, AMPM.AM));

            rd.setProjectionAndCameraMatrix(app.debugCamera);

            // Cyan background
            rd.setColorClearValue(new Color4(new Color3(0.1f, 0.5f, 1.0f), 1));

            rd.clear(app.sky == null, true, true);
            if (app.sky != null)
            {
                app.sky.pointer().render(lighting);
            }

            rd.enableLighting();
            rd.setLight(0, GLight.directional(lighting.lightDirection, lighting.lightColor));
            rd.setAmbientLightColor(lighting.ambient);

            CoordinateFrame camera = rd.getCameraToWorldMatrix();
            app.bumpShader.args.set("wsLightPos", new Vector4(lighting.lightDirection, 0));
            app.bumpShader.args.set("wsEyePos", camera.translation);
            app.bumpShader.args.set("texture", app.textureMap == null ? null : app.textureMap.pointer());
            app.bumpShader.args.set("normalBumpMap", app.normalBumpMap == null ? null : app.normalBumpMap.pointer());
            app.bumpShader.args.set("reflectivity", 0);//0.35);
            app.bumpShader.args.set("specularity", 0);//0.4);
            app.bumpShader.args.set("bumpScale", bumpScale);
            app.bumpShader.args.set("environmentMap",
                app.sky == null ? null : (app.sky.pointer().getEnvironmentMap() == null ? null : app.sky.pointer().getEnvironmentMap().pointer()));

            rd.setShader(app.bumpShader);
            Debug.debugAssertGLOk();
            for (int e = 0; e < entityArray.size(); ++e)
            {
                entityArray[e].pointer().render(rd);
                Debug.debugAssertGLOk();
            }
            rd.setShader(null);

            rd.disableLighting();

            if (app.sky != null)
            {
                app.sky.pointer().renderLensFlare(lighting);
            }
        }
    }
}
