/**
  @file gfxmeter/Report.cs

  Port of gfxmeter/Report.h and gfxmeter/Report.cpp.

  @author Morgan McGuire, matrix@graphics3d.com
*/

using System;
using G3D;

namespace gfxmeter
{
    public class MD2
    {
        public CoordinateFrame cframe;
        public ReferenceCountedPointer<MD2Model> model;
        public MD2Model.Pose pose;

        public GMaterial material = new GMaterial();

        public void load(string filename)
        {
            model = new ReferenceCountedPointer<MD2Model>(MD2Model.create(filename + ".md2"));
            material.diffuseTexture = new ReferenceCountedPointer<Texture>(
                Texture.fromFile(filename + ".pcx",
                    TextureFormat.AUTO, Texture.WrapMode.TILE, Texture.InterpolateMode.TRILINEAR_MIPMAP,
                    Texture.Dimension.DIM_2D, 1.0));
        }

        public void render(RenderDevice rd)
        {
            model.pointer().pose(cframe, pose, material).pointer().render(rd);
        }

        public void renderShadow(RenderDevice rd)
        {
            CoordinateFrame cframe2 = cframe;
            cframe2.rotation.setColumn(1, Vector3.zero());
            cframe2.translation.y -= 1.7f;
            rd.setColor(new Color3(.9f, .9f, 1));

            ReferenceCountedPointer<PosedModel> m = model.pointer().pose(cframe2, pose);
            // Intentionally render a lot of shadows to gauge rendering performance
            for (int i = 0; i < 20; ++i)
            {
                m.pointer().render(rd);
            }
        }

        public void doSimulation(GameTime dt)
        {
            pose = new MD2Model.Pose(MD2Model.Animation.STAND, G3D.System.getTick());
            /*
            pose.doSimulation(dt,
                false, false, false,
                true,  false, false,
                false, false, false,
                false, false, false,
                false, false, false,
                false);
                */
        }
    }

    /**
     Renders the summary page
     */
    public class Report : GApplet
    {
        /** Converts a bug count into a quality rating*/
        private static string quality(int bugCount)
        {
            string[] q = new string[] { "A+", "A-", "B+", "B-", "C+", "C-", "D+", "D-", "F" };
            return q[g3dmath.iClamp(bugCount, 0, 8)];
        }

        private MD2 knight;
        private MD2 ogre;

        private enum Popup { NONE, PERFORMANCE };

        private Popup popup;

        private Rect2D performanceButton;
        private Rect2D closeButton;

        private App app;

        /** Draw some nice graphics */
        private void doFunStuff()
        {
            app.renderDevice.pushState();
            GCamera camera = new GCamera();
            camera.setCoordinateFrame(new CoordinateFrame(new Vector3(0, 1, 10)));
            camera.lookAt(new Vector3(0, 2.8f, 0));

            app.renderDevice.setProjectionAndCameraMatrix(camera);

            knight.renderShadow(app.renderDevice);
            ogre.renderShadow(app.renderDevice);

            app.renderDevice.enableLighting();
            app.renderDevice.setLight(0, GLight.directional(new Vector3(-1, 1, 2).direction(), new Color3(.8f, .8f, .7f)));
            app.renderDevice.setLight(1, GLight.directional(new Vector3(.5f, -1, 1).direction(), Color3.red() * 0.2f));
            app.renderDevice.setAmbientLightColor(new Color3(.5f, .5f, .6f));

            knight.render(app.renderDevice);
            ogre.render(app.renderDevice);

            app.renderDevice.popState();
        }

        private static void drawBar(RenderDevice rd, int value, Vector2 p)
        {
            float s = rd.getWidth() * 0.35f / 100.0f;
            Draw.rect2D(Rect2D.xywh(p.x, p.y, 100 * s, 20), rd, new Color4(Color3.white() * 0.9f, 1));
            Draw.rect2D(Rect2D.xywh(p.x, p.y, value * s, 20), rd, new Color4(Color3.yellow(), 1));
            Draw.rect2DBorder(Rect2D.xywh(p.x, p.y, 100 * s, 20), rd, new Color4(Color3.black(), 1));
        }

        /** Draws the popup window, but not its contents.  Returns the window bounds. */
        private Rect2D drawPopup(string title)
        {
            int w = app.renderDevice.getWidth();
            int h = app.renderDevice.getHeight();

            // Drop shadow
            app.renderDevice.pushState();
            app.renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
            Rect2D rect = Rect2D.xywh(w / 2 - 20, h / 2 - 20, w / 2, h / 2);
            Draw.rect2D(rect + new Vector2(5, 5), app.renderDevice, new Color4(0, 0, 0, 0.15f));
            app.renderDevice.popState();

            // White box
            Draw.rect2D(rect, app.renderDevice, new Color4(Color3.white(), 1));
            Draw.rect2DBorder(rect, app.renderDevice, new Color4(Color3.black(), 1));

            // The close box
            Draw.rect2DBorder(Rect2D.xywh(rect.x1() - 16, rect.y0(), 16, 16), app.renderDevice, new Color4(Color3.black(), 1));
            app.renderDevice.setColor(new Color4(Color3.black(), 1));
            app.renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
            app.renderDevice.sendVertex(new Vector2(rect.x1() - 14, rect.y0() + 2));
            app.renderDevice.sendVertex(new Vector2(rect.x1() - 2, rect.y0() + 14));
            app.renderDevice.sendVertex(new Vector2(rect.x1() - 2, rect.y0() + 2));
            app.renderDevice.sendVertex(new Vector2(rect.x1() - 14, rect.y0() + 14));
            app.renderDevice.endPrimitive();

            float s = w * 0.013f;
            app.titleFont.pointer().draw2D(app.renderDevice, title, new Vector2(rect.x0() + 4, rect.y0()), s * 1.5, new Color4(Color3.black(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_LEFT, GFont.YAlign.YALIGN_TOP);

            return rect;
        }

        public Report(App _app)
            : base(_app)
        {
            app = _app;
        }

        public override void init()
        {
            popup = Popup.NONE;

            // Called before Report::run() beings
            app.debugCamera.setPosition(new Vector3(0, 2, 10));
            app.debugCamera.lookAt(new Vector3(0, 2, 0));

            // Init the fun stuff
            knight = new MD2();
            // The original loads "pknight" from the working directory, which its
            // main sets to the install directory; the port's data directory is
            // beside the executable, so it is prefixed here.
            knight.load(app.dataDir + "pknight");
            knight.cframe.translation = new Vector3(-5, 0, 0);

            ogre = new MD2();
            ogre.load(app.dataDir + "ogro");
            ogre.cframe.translation = new Vector3(-1.5f, 0, 0);

            knight.cframe.lookAt(ogre.cframe.translation + new Vector3(0, 0, 1));
            ogre.cframe.lookAt(knight.cframe.translation + new Vector3(0, 0, 1));
        }

        public override void cleanup()
        {
            // Called when Report::run() exits
        }

        public override void doNetwork()
        {
            // Poll net messages here
        }
        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            // Add physical simulation here

            GameTime deltaTime = 0.02;

            knight.doSimulation(deltaTime);
            ogre.doSimulation(deltaTime);
        }

        public override void doLogic()
        {
            Vector2 mouse = app.userInput.mouseXY();

            if (app.userInput.keyPressed((UserInput.KeyCode)UserInput.CustomKeyCode.SDL_LEFT_MOUSE_KEY))
            {
                if (popup == Popup.NONE)
                {
                    if (performanceButton.contains(mouse))
                    {
                        popup = Popup.PERFORMANCE;
                    }
                }
                else
                {
                    popup = Popup.NONE;
                }
            }

            if (app.userInput.keyPressed(UserInput.KeyCode.ESCAPE))
            {
                if (popup == Popup.NONE)
                {
                    // Even when we aren't in debug mode, quit on escape.
                    endApplet = true;
                    app.endProgram = true;
                }
                else
                {
                    popup = Popup.NONE;
                }
            }
        }

        /** The original's LABEL macro: a white-shadowed title, advanced by the
            returned height. */
        private float LABEL(ref Vector2 p, float s, string str)
        {
            int w = app.renderDevice.getWidth();
            return app.titleFont.pointer().draw2D(app.renderDevice, str,
                p - new Vector2(w * 0.0075f, 0), s * 2, new Color4(Color3.white() * 0.4f, 1)).y;
        }

        /** The original's PRINT macro: black body text, advanced by the returned
            height. */
        private float PRINT(ref Vector2 p, float s, string str)
        {
            return app.reportFont.pointer().draw2D(app.renderDevice, str, p, s, new Color4(Color3.black(), 1)).y;
        }

        /** The original's redefined PRINT macro in the footnote section, which
            uses a fixed 8-point size. */
        private float PRINT8(ref Vector2 p, string str)
        {
            return app.reportFont.pointer().draw2D(app.renderDevice, str, p, 8, new Color4(Color3.black(), 1)).y;
        }

        /** The original's PRINT macro in the performance popup, which prints a
            caption and the two coherence columns for a pair of measurements. */
        private void PRINT_MEASUREMENT(ref Vector2 p, float s, string cap, float[] vals)
        {
            float factor = 3 * app.vertexPerformance.numTris / 1e6f;

            app.reportFont.pointer().draw2D(app.renderDevice, cap, p, s, new Color4(Color3.black(), 1));
            app.reportFont.pointer().draw2D(app.renderDevice, (vals[0] > 0) ?
                Format.format("%5.1f", vals[0]) :
                "X", p + new Vector2(190, 0), s, new Color4(Color3.red() * 0.5f, 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT);
            app.reportFont.pointer().draw2D(app.renderDevice, (vals[0] > 0) ?
                Format.format("%5.1f", factor * vals[0]) :
                "X", p + new Vector2(240, 0), s, new Color4(Color3.red() * 0.5f, 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT);
            app.reportFont.pointer().draw2D(app.renderDevice, (vals[1] > 0) ?
                Format.format("%5.1f", vals[1]) :
                "X", p + new Vector2(330, 0), s, new Color4(Color3.red() * 0.5f, 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT);
            p.y += app.reportFont.pointer().draw2D(app.renderDevice, (vals[1] > 0) ?
                Format.format("%5.1f", factor * vals[1]) :
                "X", p + new Vector2(380, 0), s, new Color4(Color3.red() * 0.5f, 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT).y;
        }

        /** The original's overridden GApplet members map onto the port's onXxx
            hooks; doGraphics keeps the original's name and is called from
            onGraphics. */
        public override void onGraphics(RenderDevice rd)
        {
            doGraphics();
        }

        private void doGraphics()
        {
            app.renderDevice.setColorClearValue(new Color4(Color3.white(), 1));
            app.renderDevice.clear();

            doFunStuff();

            app.renderDevice.push2D();

            int w = app.renderDevice.getWidth();
            int h = app.renderDevice.getHeight();

            ///////////////////////////////////////
            // Left panel

            // Cursor position
            Vector2 p = new Vector2(w * 0.015f, h * 0.02f);

            // Font size
            float s = w * 0.013f;

            p.y += LABEL(ref p, s, "Shaders");
            p.y += PRINT(ref p, s, "Combiners: " + app.combineShader);
            p.y += PRINT(ref p, s, "Assembly:   " + app.asmShader);
            p.y += PRINT(ref p, s, "GLSL:         " + app.glslShader);

            p.y += s * 2;
            p.y += LABEL(ref p, s, "Extensions");
            p.y += PRINT(ref p, s, "FSAA:                           " + ((GLCaps.supports("WGL_ARB_multisample") || GLCaps.supports("GL_ARB_multisample")) ? "Yes" : "No"));
            p.y += PRINT(ref p, s, "Two-sided Stencil:        " + ((GLCaps.supports_two_sided_stencil() ? "Yes" : "No")));
            p.y += PRINT(ref p, s, "Stencil Wrap:               " + (GLCaps.supports("GL_EXT_stencil_wrap") ? "Yes" : "No"));
            p.y += PRINT(ref p, s, "Texture Compression: " + (GLCaps.supports("GL_EXT_texture_compression_s3tc") ? "Yes" : "No"));
            p.y += PRINT(ref p, s, "Shadow Maps:             " + (GLCaps.supports("GL_ARB_shadow") ? "Yes" : "No"));
            p.y += PRINT(ref p, s, "Frame Buffer Object:   " + (GLCaps.supports("GL_EXT_framebuffer_object") ? "Yes" : "No"));
            p.y += PRINT(ref p, s, "Vertex Arrays:              " + (GLCaps.supports_GL_ARB_vertex_buffer_object() ? "Yes" : "No"));


            ///////////////////////////////////////
            // Right Panel

            // Cursor position
            p = new Vector2(w * 0.6f, h * 0.02f);

            // Graphics Card
            p.y += LABEL(ref p, s, "Graphics Card");
            app.renderDevice.setTexture(0, app.cardLogo);
            Draw.rect2D(Rect2D.xywh(p.x - s * 6, p.y, s * 5, s * 5), app.renderDevice, new Color4(Color3.white(), 1));
            app.renderDevice.setTexture(0, null);

            p.y += PRINT(ref p, s, GLCaps.vendor());
            p.y += PRINT(ref p, s, GLCaps.renderer());
            p.y += PRINT(ref p, s, Format.format("Driver Version %s", GLCaps.driverVersion()));

            // The original prints the video memory and DirectX version here on
            // Win32.  In the port DXCaps reads the Direct3D 11 backend, so the
            // lines appear only when that backend is the one in use; the
            // backends are not interchangeable for these two queries.
            if (app.backend == "d3d11")
            {
                p.y += PRINT(ref p, s, Format.format("%d MB Video RAM", G3D.Direct3D11.DXCaps.videoMemorySize() / (1024 * 1024)));
                uint ver = G3D.Direct3D11.DXCaps.version();
                p.y += PRINT(ref p, s, Format.format("DirectX %d.%d", ver / 100, ver % 100));
            }

            p.y += s * 2;

            // Processor
            p.y += LABEL(ref p, s, "Processor");
            app.renderDevice.setTexture(0, app.chipLogo);
            Draw.rect2D(Rect2D.xywh(p.x - s * 6, p.y, s * 5, s * 5), app.renderDevice, new Color4(Color3.white(), 1));
            app.renderDevice.setTexture(0, null);

            p.y += PRINT(ref p, s, G3D.System.cpuVendor());
            p.y += PRINT(ref p, s, G3D.System.cpuArchitecture());

            Array<string> features = new Array<string>();
            if (G3D.System.has3DNow())
            {
                features.append("3DNow");
            }
            if (G3D.System.hasMMX())
            {
                features.append("MMX");
            }
            if (G3D.System.hasSSE())
            {
                features.append("SSE");
            }
            if (G3D.System.hasSSE2())
            {
                features.append("SSE2");
            }
            if (app.chipSpeed != "")
            {
                p.y += PRINT(ref p, s, app.chipSpeed + " " + stringJoin(features, '/'));
            }
            else
            {
                p.y += PRINT(ref p, s, stringJoin(features, '/'));
            }

            p.y += s * 2;

            // Operating System
            p.y += LABEL(ref p, s, "OS");
            app.renderDevice.setTexture(0, app.osLogo);
            Draw.rect2D(Rect2D.xywh(p.x - s * 6, p.y - s * 2, s * 5, s * 5), app.renderDevice);
            app.renderDevice.setTexture(0, null);


            if (G3D.System.operatingSystem().StartsWith("Windows 5.0"))
            {
                p.y += PRINT(ref p, s, "Windows 2000");
            }
            else if (G3D.System.operatingSystem().StartsWith("Windows 5.1"))
            {
                p.y += PRINT(ref p, s, "Windows XP");
            }
            p.y += PRINT(ref p, s, G3D.System.operatingSystem());

            p.y += s * 3;

            float x0 = w - s * 10;
            app.titleFont.pointer().draw2D(app.renderDevice, "Features", p - new Vector2(w * 0.0075f, 0), s * 2, new Color4(Color3.white() * 0.4f, 1));
            p.y += app.reportFont.pointer().draw2D(app.renderDevice, Format.format("f%d", app.featureRating), new Vector2(x0, p.y), s * 2, new Color4(Color3.red() * 0.5f, 1)).y;
            drawBar(app.renderDevice, app.featureRating, p);

            // Designed to put NV40 at 50
            app.performanceRating = (float)app.renderDevice.getFrameRate() / 5.0f;

            p.y += s * 4;
            performanceButton = Rect2D.xywh(p,
                app.titleFont.pointer().draw2D(app.renderDevice, "Speed", p - new Vector2(w * 0.0075f, 0), s * 2, new Color4(Color3.white() * 0.4f, 1)));

            p.y += app.reportFont.pointer().draw2D(app.renderDevice, Format.format("%5.1f", g3dmath.iRound(app.performanceRating * 10) / 10.0f), new Vector2(x0 - s * 2, p.y), s * 2, new Color4(Color3.red() * 0.5f, 1)).y;
            drawBar(app.renderDevice, (int)app.performanceRating, p);

            p.y += s * 4;
            app.titleFont.pointer().draw2D(app.renderDevice, "Quality", p - new Vector2(w * 0.0075f, 0), s * 2, new Color4(Color3.white() * 0.4f, 1));
            p.y += app.reportFont.pointer().draw2D(app.renderDevice, quality(app.bugCount), new Vector2(x0, p.y), s * 2, new Color4(Color3.red() * 0.5f, 1)).y;
            drawBar(app.renderDevice, g3dmath.iClamp(100 - app.bugCount * 10, 0, 100), p);


            p.y = h - 50;
            p.y += PRINT8(ref p, "These ratings are based on the performance of G3D apps.");
            p.y += PRINT8(ref p, "They may not be representative of overall 3D performance.");
            p.y += PRINT8(ref p, "Speed is based on both processor and graphics card. Upgrading");
            p.y += PRINT8(ref p, "your graphics driver may improve Quality and Features.");

            switch (popup)
            {
            case Popup.NONE:
                break;

            case Popup.PERFORMANCE:
                {
                    //  Draw the popup box
                    Rect2D box = drawPopup("Performance Details");
                    p.x = box.x0() + 10;
                    p.y = box.y0() + 30;

                    app.reportFont.pointer().draw2D(app.renderDevice, "Incoherent", p + new Vector2(220, 0), s, new Color4(Color3.black(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT);
                    p.y += app.reportFont.pointer().draw2D(app.renderDevice, "Coherent", p + new Vector2(350, 0), s, new Color4(Color3.black(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT).y;

                    app.reportFont.pointer().draw2D(app.renderDevice, "FPS*   MVerts/s", p + new Vector2(240, 0), s, new Color4(Color3.black(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT);
                    p.y += app.reportFont.pointer().draw2D(app.renderDevice, "FPS*   MVerts/s", p + new Vector2(370, 0), s, new Color4(Color3.black(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT).y;

                    PRINT_MEASUREMENT(ref p, s, "glBegin/glEnd", app.vertexPerformance.beginEndFPS);
                    PRINT_MEASUREMENT(ref p, s, "glDrawElements", app.vertexPerformance.drawElementsRAMFPS);
                    PRINT_MEASUREMENT(ref p, s, "  + VBO", app.vertexPerformance.drawElementsVBOFPS);
                    PRINT_MEASUREMENT(ref p, s, "  + uint16", app.vertexPerformance.drawElementsVBO16FPS);
                    PRINT_MEASUREMENT(ref p, s, "  + interleaving", app.vertexPerformance.drawElementsVBOIFPS);
                    PRINT_MEASUREMENT(ref p, s, "  (without shading)", app.vertexPerformance.drawElementsVBOPeakFPS);

                    app.reportFont.pointer().draw2D(app.renderDevice, "glDrawArrays", p, s, new Color4(Color3.black(), 1));
                    app.reportFont.pointer().draw2D(app.renderDevice, (app.vertexPerformance.drawArraysVBOPeakFPS > 0) ?
                        Format.format("%5.1f", app.vertexPerformance.drawArraysVBOPeakFPS) :
                        "X", p + new Vector2(330, 0), s, new Color4(Color3.red() * 0.5f, 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT);
                    p.y += app.reportFont.pointer().draw2D(app.renderDevice, (app.vertexPerformance.drawArraysVBOPeakFPS > 0) ?
                        Format.format("%5.1f", 3 * app.vertexPerformance.numTris / 1e6f * app.vertexPerformance.drawArraysVBOPeakFPS) :
                        "X", p + new Vector2(380, 0), s, new Color4(Color3.red() * 0.5f, 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_RIGHT).y;

                    p.y += s;
                    p.y += app.reportFont.pointer().draw2D(app.renderDevice, Format.format("* FPS at %d k polys/frame.",
                        g3dmath.iRound(app.vertexPerformance.numTris / 1000.0)), p + new Vector2(20, 0), s, new Color4(Color3.black(), 1)).y;
                }
                break;
            }

            app.renderDevice.pop2D();
        }

        private static string stringJoin(Array<string> a, char joinChar)
        {
            return G3D.StringUtils.stringJoin(a, joinChar);
        }
    }
}
