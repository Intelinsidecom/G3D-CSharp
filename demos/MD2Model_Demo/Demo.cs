/**
  @file MD2Model_Demo/Demo.cs

  Port of the Demo and App class bodies in MD2Model_Demo/main.cpp.
 */

using System;
using G3D;

namespace MD2Model_Demo
{
    public class Demo : GApplet
    {
        private void loadModels(string dir)
        {
            model = MD2Model.create(dir + "tris.md2");

            Array<string> weaponFilename = new Array<string>();
            FileUtils.getFiles(dir + "w_*.md2", weaponFilename);
            FileUtils.getFiles(dir + "weapon.md2", weaponFilename);

            weapon = MD2Model.create(dir + weaponFilename.last());
        }

        private void loadSkins(string dir, double brighten)
        {
            Array<string> previewName = new Array<string>();
            Array<string> textureName = new Array<string>();

            // Find all of the skins that have preview files
            FileUtils.getFiles(dir + "*_i.*", previewName);
            for (int i = 0; i < previewName.size(); ++i)
            {
                string ext = FileUtils.filenameExt(previewName[i]);
                // Strip off the extension and "_i"
                string basename = previewName[i].Substring(0, previewName[i].Length - ext.Length - 3);

                FileUtils.getFiles(dir + basename + ".*", textureName);
            }

            // Now load the skins themselves
            for (int i = 0; i < textureName.size(); ++i)
            {
                string ext = FileUtils.filenameExt(textureName[i]);

                if (GImage.supportedFormat(ext))
                {
                    string filename = dir + textureName[i];
                    modelTexture.append(new ReferenceCountedPointer<Texture>(
                        Texture.fromFile(filename, TextureFormat.AUTO, Texture.WrapMode.TILE,
                            Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, brighten)));
                }
            }

            if (weapon.pointer().textureFilenames().size() > 0)
            {
                string filename = "data/" + weapon.pointer().textureFilenames()[0];
                if (FileUtils.fileExists(filename))
                {
                    weaponTexture = new ReferenceCountedPointer<Texture>(
                        Texture.fromFile(filename, TextureFormat.AUTO, Texture.WrapMode.TILE,
                            Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, brighten));
                }
                else
                {
                    filename = dir + "weapon.pcx";
                    if (FileUtils.fileExists(filename))
                    {
                        weaponTexture = new ReferenceCountedPointer<Texture>(
                            Texture.fromFile(filename, TextureFormat.AUTO, Texture.WrapMode.TILE,
                                Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, brighten));
                    }
                }
            }
            else
            {
                weaponTexture = null;
            }
        }

        private void load(string name)
        {
            double brighten = 2.0;

            string dir = string.Format("{0}quake2/players/{1}/", app.dataDir, name);

            loadModels(dir);
            model.pointer().setName(StringUtils.toUpper(name.Substring(0, 1)) + name.Substring(1));

            modelTexture.clear();
            weaponTexture = null;
            loadSkins(dir, brighten);
        }

        private void drawCharWithShadow(CoordinateFrame cframe, MD2Model.Pose pose)
        {
            app.renderDevice.setColor(Color3.white());
            model.pointer().pose(cframe, pose).pointer().render(app.renderDevice);

            //Draw::box(model->pose(cframe, pose)->worldSpaceBoundingBox(), renderDevice);

            if (!MD2Model.animationDeath(pose.animation))
            {
                // Weapons have no death animations
                app.renderDevice.setTexture(0, weaponTexture == null ? null : weaponTexture.pointer());
                weapon.pointer().pose(cframe, pose).pointer().render(app.renderDevice);
            }

            // Shadow
            app.renderDevice.setTexture(0, null);
            cframe.translation.y = 0;
            app.renderDevice.disableLighting();
            cframe.rotation.setColumn(1, Vector3.zero());
            app.renderDevice.setColor(Color3.gray() * .5f);
            model.pointer().pose(cframe, pose).pointer().render(app.renderDevice);
            app.renderDevice.enableLighting();
        }

        private App app;

        public ReferenceCountedPointer<Sky> sky;
        public ReferenceCountedPointer<GFont> font;

        public GameTime gameTime;

        public ReferenceCountedPointer<MD2Model> model;
        public Array<ReferenceCountedPointer<Texture>> modelTexture = new Array<ReferenceCountedPointer<Texture>>();
        public ReferenceCountedPointer<MD2Model> weapon;
        public ReferenceCountedPointer<Texture> weaponTexture;
        public MD2Model.Pose pose;

        /** Names of all of the models available to load */
        public Array<string> modelNameArray = new Array<string>();

        /** Index into modelNameArray*/
        public int currentModel;

        public Demo(App _app)
            : base(_app)
        {
            app = _app;
            pose = new MD2Model.Pose(MD2Model.Animation.STAND, 0);
            currentModel = 0;
            gameTime = 0;
        }

        public override void onInit()
        {
            // The original set the window caption; the backend-neutral window
            // contract does not expose a caption.
            app.debugCamera.setPosition(new Vector3(0, 1, -13));
            app.debugCamera.lookAt(new Vector3(0, 1.6f, -8));

            app.renderDevice.setColorClearValue(new Color4(new Color3(1, 1, 1), 1));

            FileUtils.getDirs(app.dataDir + "quake2/players/*", modelNameArray);

            // Make sure these files all exist
            for (int i = modelNameArray.size() - 1; i >= 0; --i)
            {
                string s = modelNameArray[i];
                if (!FileUtils.fileExists(app.dataDir + "quake2/players/" + modelNameArray[i] + "/tris.md2"))
                {
                    modelNameArray.remove(i);
                }
            }

            if (modelNameArray.size() == 0)
            {
                string[] choice = new string[] { "Ok" };
                G3D.Prompt.prompt("MD2 Demo Error",
                    "No MD2 models found in data/players.  (Download some from polycount.com and expand them into the data directory)",
                    choice, 1);
                app.endProgram = true;
                return;
            }

            currentModel = 0;

            load(modelNameArray[currentModel]);
        }

        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            RealTime timeStep = (double)sdt;

            app.debugController.doSimulation(g3dmath.clamp(timeStep, 0.0, 0.1));
            Vector3 v = app.debugController.getPosition();

            // Keep the camera above the ground plane
            if (v.y < .1f)
            {
                app.debugController.setPosition(new Vector3(v.x, .1f, v.z));
            }

            app.debugCamera.setCoordinateFrame(app.debugController.getCoordinateFrame());

            if (MD2Model.animationDeath(pose.animation))
            {
                if (pose.time > 2)
                {
                    // Bring back to life.
                    pose.animation = MD2Model.Animation.STAND;
                }
            }

            pose.doSimulation((GameTime)(double)sdt,
                app.userInput.keyDown(UserInput.KeyCode.LCTRL) || app.userInput.keyDown(UserInput.KeyCode.RCTRL) ||
                app.userInput.keyDown(UserInput.KeyCode.LSHIFT) || app.userInput.keyDown(UserInput.KeyCode.RSHIFT),
                app.userInput.keyDown((UserInput.KeyCode)'r'),
                app.userInput.keyDown((UserInput.KeyCode)'t'),
                app.userInput.keyPressed((UserInput.KeyCode)UserInput.CustomKeyCode.SDL_LEFT_MOUSE_KEY),
                app.userInput.keyPressed(UserInput.KeyCode.SPACE) || app.userInput.keyPressed(UserInput.KeyCode.BACKSPACE),
                app.userInput.keyPressed((UserInput.KeyCode)'1'),
                app.userInput.keyPressed((UserInput.KeyCode)'2'),
                app.userInput.keyPressed((UserInput.KeyCode)'3'),
                app.userInput.keyPressed((UserInput.KeyCode)'4'),
                app.userInput.keyPressed((UserInput.KeyCode)'5'),
                app.userInput.keyPressed((UserInput.KeyCode)'6'),
                app.userInput.keyPressed((UserInput.KeyCode)'7'),
                app.userInput.keyPressed((UserInput.KeyCode)'8'),
                app.userInput.keyPressed((UserInput.KeyCode)'9'),
                app.userInput.keyPressed((UserInput.KeyCode)'0'),
                app.userInput.keyPressed((UserInput.KeyCode)'-'));
        }

        public override void onGraphics(RenderDevice rd)
        {
            LightingParameters lighting = new LightingParameters(G3DGameUnits.toSeconds(10, 00, 00, AMPM.AM));

            // Some models have part of their geometry stored in the "weapon" file.
            // Darth Maul, for example, has his lower half in the weapon.
            double my = model.pointer().pose(CoordinateFrame.identity(), new MD2Model.Pose(MD2Model.Animation.STAND)).pointer().objectSpaceBoundingBox().getCorner(0).y;
            double wy = weapon.pointer().pose(CoordinateFrame.identity(), new MD2Model.Pose(MD2Model.Animation.STAND)).pointer().objectSpaceBoundingBox().getCorner(0).y;
            double footy = 0.98 * Math.Min(my, wy);

            app.renderDevice.clear(true, true, true);
            app.renderDevice.pushState();

            app.renderDevice.setProjectionAndCameraMatrix(app.debugCamera);

            app.renderDevice.enableLighting();
            app.renderDevice.setLight(0, GLight.directional(lighting.lightDirection, lighting.lightColor));
            app.renderDevice.setLight(1, GLight.directional(-lighting.lightDirection, Color3.white() * .25f));
            app.renderDevice.setAmbientLightColor(lighting.ambient);

            int n = 1;

            // Draw a bunch of characters
            for (int z = 0; z < 5; ++z)
            {
                for (int x = -2; x <= 2; ++x)
                {
                    MD2Model.Pose localPose = new MD2Model.Pose(MD2Model.Animation.STAND, (GameTime)(n + G3D.System.getTick()));

                    CoordinateFrame cframe = new CoordinateFrame(new Vector3((float)(x * 6 + (z % 2) * 2), (float)-footy, (float)(z * 6)));
                    cframe.rotation = Matrix3.fromAxisAngle(Vector3.unitY(), (float)(n * .5 + 4));

                    if (modelTexture.size() > 0)
                    {
                        app.renderDevice.setTexture(0, modelTexture[(n + 1 + z * 2) % modelTexture.size()].pointer());
                    }

                    drawCharWithShadow(cframe, localPose);

                    ++n;
                }
            }

            // Draw the main character
            {
                CoordinateFrame cframe = new CoordinateFrame(new Vector3(0, (float)-footy, -8));

                if (modelTexture.size() > 0)
                {
                    app.renderDevice.setTexture(0, modelTexture.last().pointer());
                }

                drawCharWithShadow(cframe, pose);
            }

            app.renderDevice.disableLighting();

            app.renderDevice.setObjectToWorldMatrix(CoordinateFrame.identity());

            // Ground plane (to hide parts of characters that stick through ground)
            app.renderDevice.setColor(Color3.white());
            app.renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
            app.renderDevice.sendVertex(new Vector3(-50, -.01f, 50));
            app.renderDevice.sendVertex(new Vector3(50, -.01f, 50));
            app.renderDevice.sendVertex(new Vector3(50, -.01f, -50));
            app.renderDevice.sendVertex(new Vector3(-50, -.01f, -50));
            app.renderDevice.endPrimitive();
            app.renderDevice.popState();

            app.renderDevice.push2D();
            double textX = 10;
            double textY = 10;
            double f = 16;
            app.debugFont.pointer().draw2D(string.Format("{0} fps", g3dmath.iRound(app.renderDevice.getFrameRate())), new Vector2((float)textX, (float)textY), 20, new Color4(Color3.yellow(), 1), new Color4(Color3.black(), 1)); textY += 30;
            app.debugFont.pointer().draw2D(string.Format("{0} characters", n), new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D(string.Format("{0:0.0} MB", model.pointer().mainMemorySize() / 1000000.0), new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D(string.Format("{0:0.0} Mtris/sec", app.renderDevice.getTriangleRate() / 1e6), new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;

            app.debugFont.pointer().draw2D(model.pointer().name(), new Vector2((float)(app.renderDevice.getWidth() / 2), (float)(app.renderDevice.getHeight() - 45)), 30,
                new Color4(Color3.black(), 1), new Color4(Color3.white(), 1), GFont.XAlign.XALIGN_CENTER);

            textX = app.renderDevice.getWidth() - 130;
            textY = 10;
            f = 12;
            app.debugFont.pointer().draw2D("CLICK   attack", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D("SPACE  jump", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D("CTRL     crouch", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D("1 . . 5    taunt", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D("6 . . 8    die", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D("9 . . -    pain", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D("R/T       run/back", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.debugFont.pointer().draw2D("e           new character", new Vector2((float)textX, (float)textY), f, new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1)); textY += f * 1.5;
            app.renderDevice.pop2D();
        }

        public override void onUserInput(UserInput ui)
        {
            base.onUserInput(ui);

            if (ui.keyPressed((UserInput.KeyCode)'e'))
            {
                currentModel = (currentModel + 1) % modelNameArray.size();
                load(modelNameArray[currentModel]);
                return;
            }
        }
    }
}
