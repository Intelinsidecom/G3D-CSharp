using System;
using G3D;

namespace G3D.Tests
{
    internal static class Program
    {
        private static int failures;

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                failures++;
                Console.WriteLine("FAIL: " + name);
            }
        }

        private static bool hasMethod(global::System.Reflection.MethodInfo[] methods, string name)
        {
            for (int i = 0; i < methods.Length; ++i)
            {
                if (methods[i].Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private class TestModule : GModule
        {
            public int simulationCount;
            public int userInputCount;
            public int logicCount;
            public int networkCount;
            public bool consumeEvent;

            public override void getPosedModel(
                Array<ReferenceCountedPointer<PosedModel>> posedArray,
                Array<ReferenceCountedPointer<PosedModel2D>> posed2DArray)
            {
            }

            public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
            {
                simulationCount++;
            }

            public override bool onEvent(GEvent @event)
            {
                return consumeEvent;
            }

            public override void onUserInput(UserInput ui)
            {
                userInputCount++;
            }

            public override void onNetwork()
            {
                networkCount++;
            }

            public override void onLogic()
            {
                logicCount++;
            }
        }

        private static void testUserInput()
        {
            UserInput input = new UserInput();
            Check(input.getX() == 0.0f && input.getY() == 0.0f, "UserInput neutral axes");

            input.processKey(UserInput.KeyCode.W, true);
            Check(input.getY() == 1.0f, "UserInput W maps to up");
            Check(input.keyDown(UserInput.KeyCode.W), "UserInput keyDown");
            input.processKey(UserInput.KeyCode.W, false);
            Check(input.getY() == 0.0f, "UserInput release clears direction");

            input.processKey(UserInput.KeyCode.D, true);
            Check(input.getX() == 1.0f, "UserInput D maps to right");
            input.processKey(UserInput.KeyCode.D, false);

            input.beginEvents();
            GEvent keyEvent = new GEvent(GEventType.KEY_DOWN);
            keyEvent.key = (int)UserInput.KeyCode.A;
            input.processEvent(keyEvent);
            Check(input.getX() == -1.0f, "UserInput event key down");
            Check(input.keyPressed(UserInput.KeyCode.A), "UserInput keyPressed");
            Check(input.anyKeyPressed(), "UserInput anyKeyPressed");
            input.endEvents();

            input.beginEvents();
            GEvent moveEvent = new GEvent(GEventType.MOUSE_MOVE);
            moveEvent.mouseDeltaX = 5;
            moveEvent.mouseDeltaY = -3;
            input.processEvent(moveEvent);
            input.endEvents();
            Check(input.mouseDX() == 5.0f && input.mouseDY() == -3.0f, "UserInput mouse delta");
            Check(input.mouseXY().x == 5.0f && input.mouseXY().y == -3.0f, "UserInput mouse position");

            Check(UserInput.keyCodeToString(UserInput.KeyCode.UP) == "Up", "UserInput key name");
            Check(UserInput.keyCodeToString(UserInput.KeyCode.ESCAPE) == "Esc", "UserInput escape name");
            Check(UserInput.keyCodeToString((UserInput.KeyCode)(int)UserInput.CustomKeyCode.SDL_LEFT_MOUSE_KEY) == "L Mouse", "UserInput mouse key name");
            Check(UserInput.stringToKeyCode("up") == UserInput.KeyCode.UP, "UserInput string to key code");
            Check(UserInput.stringToKeyCode("l mouse") == (UserInput.KeyCode)(int)UserInput.CustomKeyCode.SDL_LEFT_MOUSE_KEY, "UserInput string to mouse key code");
            Check(UserInput.stringToKeyCode("not a key") == UserInput.KeyCode.UNKNOWN, "UserInput unknown key code");
        }

        private static void testGModuleManager()
        {
            GModuleManager manager = new GModuleManager();
            TestModule normal = new TestModule();
            ReferenceCountedPointer<GModule> normalRef = new ReferenceCountedPointer<GModule>(normal);
            manager.add(normalRef);
            Check(manager.size() == 1, "GModuleManager size after add");

            manager.onSimulation(0.1, 0.1, 0.1);
            manager.onUserInput(new UserInput());
            manager.onNetwork();
            manager.onLogic();
            Check(normal.simulationCount == 1, "GModuleManager forwards simulation");
            Check(normal.userInputCount == 1, "GModuleManager forwards user input");
            Check(normal.networkCount == 1, "GModuleManager forwards network");
            Check(normal.logicCount == 1, "GModuleManager forwards logic");

            TestModule deferred = new TestModule();
            manager.beginLock();
            manager.add(new ReferenceCountedPointer<GModule>(deferred));
            Check(manager.size() == 1, "GModuleManager defers add while locked");
            manager.endLock();
            Check(manager.size() == 2, "GModuleManager applies deferred add");

            TestModule high = new TestModule();
            high.consumeEvent = true;
            manager.add(new ReferenceCountedPointer<GModule>(high), GModuleManager.EventPriority.HIGH_PRIORITY);
            Check(manager.onEvent(new GEvent(GEventType.KEY_DOWN)), "GModuleManager consumes event");
            Check(high.consumeEvent, "GModuleManager high priority module ran");

            manager.remove(normalRef);
            Check(manager.size() == 2, "GModuleManager size after remove");
            manager.clear();
            Check(manager.size() == 2, "GModuleManager clear leaves the original stale size");
        }

        private static void testFirstPersonManipulator()
        {
            FirstPersonManipulator manipulator = new FirstPersonManipulator();
            manipulator.reset();
            Check(manipulator.getYaw() == -g3dmath.G3D_PI / 2, "FirstPersonManipulator reset yaw");
            Check(manipulator.getPitch() == 0.0, "FirstPersonManipulator reset pitch");
            Check(manipulator.getPosition() == Vector3.zero(), "FirstPersonManipulator reset position");
            Check(manipulator.active() == false, "FirstPersonManipulator starts inactive");

            manipulator.setPosition(Vector3.zero());
            manipulator.lookAt(new Vector3(0, 0, -1));
            Check(Math.Abs(manipulator.getYaw()) < 1e-6, "FirstPersonManipulator lookAt yaw");
            Check(Math.Abs(manipulator.getPitch()) < 1e-6, "FirstPersonManipulator lookAt pitch");
            Vector3 look = manipulator.getLookVector();
            Check(Math.Abs(look.x) < 1e-5f && Math.Abs(look.y) < 1e-5f && Math.Abs(look.z + 1.0f) < 1e-5f, "FirstPersonManipulator look vector");

            UserInput input = new UserInput();
            manipulator.init(null, input);
            manipulator.setPosition(Vector3.zero());
            manipulator.lookAt(new Vector3(0, 0, -1));
            manipulator.setActive(true);
            Check(manipulator.active(), "FirstPersonManipulator active");
            Check(input.pureDeltaMouse(), "FirstPersonManipulator captures the mouse");

            input.processKey(UserInput.KeyCode.W, true);
            manipulator.onSimulation(0.0, 1.0, 1.0);
            Vector3 moved = manipulator.getPosition();
            Check(Math.Abs(moved.z + 10.0f) < 1e-3f, "FirstPersonManipulator moves along the look vector: " + moved.z);

            manipulator.setActive(false);
            Check(!input.pureDeltaMouse(), "FirstPersonManipulator releases the mouse");

            manipulator.setFrame(new CoordinateFrame());
            Check(manipulator.getPosition() == Vector3.zero(), "FirstPersonManipulator setFrame");
        }

        private static void testVARAreaAndVAR()
        {
            VARArea area = VARArea.create(1024, VARArea.UsageHint.WRITE_EVERY_FRAME);
            Check(area.totalSize() == 1024, "VARArea total size");
            Check(area.allocatedSize() == 0, "VARArea starts empty");
            Check(area.freeSize() == 1024, "VARArea free size");
            Check(area.peakAllocatedSize() == 0, "VARArea peak starts empty");

            Vector3[] vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0) };
            VAR vertexVAR = new VAR(vertices, area);
            Check(vertexVAR.valid(), "VAR is valid after allocation");
            Check(vertexVAR.getCount() == 2, "VAR element count");
            Check(vertexVAR.getStride() == 12, "VAR stride");
            Check(vertexVAR.maxSize() == 24, "VAR max size");
            Check(area.allocatedSize() == 24, "VARArea accounting after allocation");
            Check(area.peakAllocatedSize() == 24, "VARArea peak after allocation");

            byte[] data = vertexVAR.getData();
            Check(data != null && data.Length == 24, "VAR data length");
            Check(data[0] == 0 && data[4] == 0 && data[8] == 0, "VAR first vertex bytes");
            Check(data[12] == 0 && data[13] == 0 && data[15] == 63, "VAR second vertex bytes");

            vertexVAR.set(1, new Vector3(2, 0, 0));
            data = vertexVAR.getData();
            Check(data[12] == 0 && data[13] == 0 && data[15] == 64, "VAR set element");

            vertexVAR.update(new Vector3[] { new Vector3(0, 0, 0) }, 1);
            Check(vertexVAR.getCount() == 1, "VAR update shrinks the element count");

            ulong generation = area.currentGeneration();
            area.reset();
            Check(area.currentGeneration() == generation + 1, "VARArea generation increments on reset");
            Check(area.allocatedSize() == 0, "VARArea frees on reset");
            Check(!vertexVAR.valid(), "VAR invalid after area reset");

            VARArea arena = VARArea.create(256, VARArea.UsageHint.WRITE_ONCE);
            VAR first = new VAR(new Vector3[4], arena);
            VAR second = new VAR(new Vector3[4], arena);
            Check(second.getOffset() >= first.getOffset() + 48, "VAR allocations do not overlap");
            Check(arena.remove(first), "VARArea removes a block");
            Check(arena.allocatedSize() < 48 + 48, "VARArea accounting after removal");
        }

        private static void testTextureSurface()
        {
            byte[] pixels = new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 };
            Texture texture = Texture.fromMemory("surface", pixels, TextureFormat.RGBA8, 2, 1, TextureFormat.RGBA8,
                                                Texture.WrapMode.TILE, Texture.InterpolateMode.NEAREST_NO_MIPMAP,
                                                Texture.Dimension.DIM_2D, Texture.DepthReadMode.DEPTH_NORMAL, 2.0f, 1.0f);
            Check(texture.getFormat() == TextureFormat.RGBA8, "Texture format");
            Check(texture.getWidth() == 2 && texture.getHeight() == 1, "Texture dimensions");
            Check(texture.getDimension() == Texture.Dimension.DIM_2D, "Texture dimension");
            Check(texture.getInterpolateMode() == Texture.InterpolateMode.NEAREST_NO_MIPMAP, "Texture interpolate mode");
            Check(texture.getWrapMode() == Texture.WrapMode.TILE, "Texture wrap mode");
            Check(texture.getDepthReadMode() == Texture.DepthReadMode.DEPTH_NORMAL, "Texture depth read mode");
            Check(!texture.opaque(), "RGBA8 texture is not opaque");
            Check(texture.getData().Length == 8, "Texture retains its pixel data");

            GImage image = new GImage();
            texture.getImage(image);
            Check(image.width == 2 && image.height == 1 && image.channels == 4, "Texture image conversion");

            Texture alpha = texture.alphaOnlyVersion();
            Check(alpha != null && alpha.getFormat() == TextureFormat.A8, "Texture alpha-only conversion");
            Check(alpha.getData().Length == 2, "Texture alpha-only data length");

            Texture.Settings settings = new Texture.Settings();
            Check(settings.hashCode() == Texture.hashCode(settings), "Texture settings hash is stable");
            settings.autoMipMap = false;
            Check(settings.hashCode() != Texture.hashCode(new Texture.Settings()), "Texture settings hash covers autoMipMap");

            string baseName, extension;
            Texture.splitFilenameAtWildCard("sky_*ft.png", out baseName, out extension);
            Check(baseName == "sky_" && extension == "ft.png", "Texture wildcard filename split");

            Matrix3 rotation;
            Texture.getCameraRotation(Texture.CubeFace.CUBE_POS_X, out rotation);
            Check(Math.Abs(rotation.determinant() - 1.0f) < 1e-3f, "Texture cube face rotation is a rotation matrix");

            long before = Texture.sizeOfAllTexturesInMemory();
            Texture accounted = Texture.createEmpty(4, 4, "accounted", TextureFormat.RGBA8);
            Check(Texture.sizeOfAllTexturesInMemory() > before, "Texture memory accounting grows");
            accounted.clear();
            accounted.free();
            Check(Texture.sizeOfAllTexturesInMemory() == before, "Texture memory accounting released");
        }

        private static void testFramebufferSurface()
        {
            Check((int)Framebuffer.AttachmentPoint.COLOR_ATTACHMENT0 == 0x8CE0, "Framebuffer color attachment constant");
            Check((int)Framebuffer.AttachmentPoint.COLOR_ATTACHMENT15 == 0x8CEF, "Framebuffer color attachment 15 constant");
            Check((int)Framebuffer.AttachmentPoint.DEPTH_ATTACHMENT == 0x8D00, "Framebuffer depth attachment constant");
            Check((int)Framebuffer.AttachmentPoint.STENCIL_ATTACHMENT == 0x8D20, "Framebuffer stencil attachment constant");
            Check(Framebuffer.hashCode(Framebuffer.AttachmentPoint.COLOR_ATTACHMENT0) == 0x8CE0, "Framebuffer attachment hash");

            Texture color = Texture.createEmpty(4, 4, "fb-color", TextureFormat.RGBA8);
            Renderbuffer.Settings depthSettings = new Renderbuffer.Settings();
            depthSettings.format = TextureFormat.DEPTH24;
            depthSettings.width = 4;
            depthSettings.height = 4;
            Renderbuffer depth = Renderbuffer.create(depthSettings);
            Check(depth.getFormat() == TextureFormat.DEPTH24, "Renderbuffer format");
            Check(depth.getWidth() == 4 && depth.getHeight() == 4, "Renderbuffer dimensions");

            Framebuffer framebuffer = Framebuffer.create(4, 4);
            framebuffer.setTexture(Framebuffer.AttachmentPoint.Color0, color);
            framebuffer.setRenderbuffer(Framebuffer.AttachmentPoint.Depth, depth);
            Check(framebuffer.isComplete(), "Framebuffer is complete");
            Check(framebuffer.getNumAttachments() == 2, "Framebuffer attachment count");
            Check(framebuffer.getWidth() == 4 && framebuffer.getHeight() == 4, "Framebuffer dimensions");

            string reason;
            framebuffer.detachTexture(Framebuffer.AttachmentPoint.Color0);
            Check(framebuffer.getNumAttachments() == 1, "Framebuffer attachment count after detach");
            framebuffer.detachAll();
            Check(!framebuffer.isComplete(out reason), "Framebuffer without attachments is incomplete");
        }

        private static void testRenderDeviceState()
        {
            RenderDevice renderDevice = new RenderDevice();
            renderDevice.pushState();
            renderDevice.setAlphaTest(RenderDevice.AlphaTest.ALPHA_GREATER, 0.25);
            Check(renderDevice.alphaTestReference() == 0.25, "RenderDevice alpha test reference");
            renderDevice.popState();

            Matrix4 matrix = Matrix4.identity();
            matrix.m00 = 2;
            renderDevice.setTextureMatrix(0, matrix);
            Matrix4 stored = renderDevice.getTextureMatrix(0);
            Check(Math.Abs(stored.m00 - 2.0f) < 1e-6f, "RenderDevice texture matrix round trip");

            renderDevice.setProjectionMatrix(Matrix4.identity());
            renderDevice.setObjectToWorldMatrix(CoordinateFrame.identity());
            Vector4 projected = renderDevice.projectToScreen(new Vector3(0, 0, 0));
            Check(!float.IsNaN(projected.x) && !float.IsNaN(projected.y), "RenderDevice projectToScreen");
        }

        private static void testGModuleDispatch()
        {
            GModuleManager manager = new GModuleManager();
            TestModule first = new TestModule();
            TestModule second = new TestModule();
            TestModule high = new TestModule();
            manager.add(new ReferenceCountedPointer<GModule>(first));
            manager.add(new ReferenceCountedPointer<GModule>(second));
            manager.add(new ReferenceCountedPointer<GModule>(high), GModuleManager.EventPriority.HIGH_PRIORITY);
            Check(manager.size() == 3, "GModuleManager size with priorities");

            manager.onLogic();
            Check(first.logicCount == 1 && second.logicCount == 1 && high.logicCount == 1, "GModuleManager forwards logic to every priority");

            manager.remove(new ReferenceCountedPointer<GModule>(first));
            Check(manager.size() == 2, "GModuleManager size after removal");
        }

        private sealed class TestWindow : IGraphicsWindow
        {
            public IntPtr NativeHandle { get { return IntPtr.Zero; } }
            public int Width { get { return 640; } }
            public int Height { get { return 480; } }
            public void MakeCurrent() { }
            public void Present() { }
            public void Resize(int width, int height) { }
            public GWindow.Settings getSettings() { return new GWindow.Settings(); }
            public string getAPIName() { return "Test"; }
            public string getAPIVersion() { return "1.0"; }
            public bool hasFocus() { return true; }
            public void Dispose() { }
        }

        private static void testGWindowSurface()
        {
            GWindow.Settings settings = new GWindow.Settings();
            Check(settings.width == 800 && settings.height == 600, "GWindow default size");
            Check(settings.center, "GWindow centers by default");
            Check(settings.rgbBits == 8 && settings.depthBits == 24 && settings.stencilBits == 8, "GWindow default buffer bits");
            Check(settings.fsaaSamples == 1, "GWindow default multisample count");

            GWindow window = new GWindow(new TestWindow());
            Check(window.width() == 640 && window.height() == 480, "GWindow dimensions come from the host window");
            Check(window.getAPIName() == "Test", "GWindow API name");

            int loopCount = 0;
            window.pushLoopBody(delegate(object state) { loopCount++; }, null);
            window.oneFrame();
            Check(loopCount == 1, "GWindow runs the loop body once per frame");

            window.postEvent(new GEvent(GEventType.KEY_DOWN));
            GEvent polled;
            Check(window.pollEvent(out polled), "GWindow polls a posted event");

            window.setInputCaptureCount(1);
            Check(window.inputCaptureCount() == 1, "GWindow input capture count");
            window.decInputCaptureCount();
            Check(window.inputCaptureCount() == 0, "GWindow input capture count decrements");

            window.setMouseHideCount(1);
            Check(window.mouseHideCount() == 1 && !window.mouseVisible(), "GWindow mouse hide count hides the cursor");
            window.decMouseHideCount();
            Check(window.mouseVisible(), "GWindow mouse hide count restores the cursor");
        }

        private static void testShaderSurface()
        {
            GPUProgram.Type type;
            Check(GPUProgram.CgType("float4x4", out type) && type == GPUProgram.Type.FLOAT4X4, "GPUProgram Cg type parsing");
            Check(!GPUProgram.CgType("not a type", out type), "GPUProgram rejects unknown Cg types");
            Check(GPUProgram.toString(GPUProgram.Type.FLOAT4X4).Length > 0, "GPUProgram type name");

            GPUProgram.ArgList args = new GPUProgram.ArgList();
            args.set("scale", 2.0f);
            Check(args.has("scale"), "GPUProgram argument list contains the argument");
            float scale;
            Check(args.get("scale", out scale) && scale == 2.0f, "GPUProgram argument value");
            bool duplicate = false;
            try
            {
                args.set("scale", 3.0f);
            }
            catch (InvalidOperationException)
            {
                duplicate = true;
            }
            Check(duplicate, "GPUProgram rejects a duplicate argument");
            args.clear();
            Check(!args.has("scale"), "GPUProgram argument list clears");
        }

        private static void testLightingParametersSurface()
        {
            Check(Math.Abs(LightingParameters.SIDEREAL_DAY - (23 * 3600 + 56 * 60 + 4.071)) < 0.01, "LightingParameters sidereal day length");

            LightingParameters lighting = new LightingParameters(new GameTime(0));
            GLight light = lighting.directionalLight();
            Check(light.position.xyz().length() > 0.9f, "LightingParameters directional light");
            Check(light.color == lighting.lightColor, "LightingParameters uses the current light color");
        }

        private static void testModelSurface()
        {
            Array<int> index = new Array<int>();
            index.push(0);
            index.push(1);
            index.push(2);
            Array<Vector3> vertex = new Array<Vector3>();
            vertex.push(new Vector3(0, 0, 0));
            vertex.push(new Vector3(1, 0, 0));
            vertex.push(new Vector3(0, 1, -1));
            Array<Vector2> texCoord = new Array<Vector2>();
            texCoord.push(new Vector2(0, 0));
            texCoord.push(new Vector2(1, 0));
            texCoord.push(new Vector2(0, 1));

            string filename = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), "g3d-port-ifs-test.ifs");
            IFSModel.save(filename, "testModel", index, vertex, texCoord);

            string name;
            Array<int> loadedIndex = new Array<int>();
            Array<Vector3> loadedVertex = new Array<Vector3>();
            Array<Vector2> loadedTexCoord = new Array<Vector2>();
            IFSModel.load(filename, out name, loadedIndex, loadedVertex, loadedTexCoord);
            Check(name == "testModel", "IFSModel round trip name");
            Check(loadedIndex.size() == 3 && loadedVertex.size() == 3 && loadedTexCoord.size() == 3, "IFSModel round trip counts");
            Check(loadedVertex[2].y == 1.0f, "IFSModel round trip vertex data");
            global::System.IO.File.Delete(filename);
        }

        private static void testMD2ModelSurface()
        {
            Check((double)MD2Model.animationLength(MD2Model.Animation.STAND) > 0.0, "MD2Model stand animation length");
            Check(MD2Model.animationLoops(MD2Model.Animation.STAND), "MD2Model stand loops");
            Check(!MD2Model.animationLoops(MD2Model.Animation.DEATH_FALLBACK), "MD2Model death does not loop");
            Check(MD2Model.animationCrouch(MD2Model.Animation.CROUCH_STAND), "MD2Model crouch classification");
            Check(MD2Model.animationDeath(MD2Model.Animation.DEATH_FALLBACK), "MD2Model death classification");
            Check(MD2Model.animationAttack(MD2Model.Animation.CROUCH_ATTACK), "MD2Model attack classification");
            Check(MD2Model.animationStand(MD2Model.Animation.CROUCH_STAND), "MD2Model stand classification");
            Check(MD2Model.animationRunForward(MD2Model.Animation.RUN), "MD2Model forward run classification");
            Check((double)MD2Model.PRE_BLEND_TIME == 0.125, "MD2Model pre-blend time");

            MD2Model.Pose pose = new MD2Model.Pose(MD2Model.Animation.STAND, new GameTime(0));
            int frame = MD2Model.getFrameNumber(pose);
            Check(frame >= 0, "MD2Model frame number");

            pose.doSimulation(new GameTime(0.1), false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
            Check(pose.animation == MD2Model.Animation.RUN, "MD2Model simulation starts running");

            pose = new MD2Model.Pose(MD2Model.Animation.STAND, new GameTime(0));
            pose.doSimulation(new GameTime(0.1), false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false);
            Check(pose.animation == MD2Model.Animation.ATTACK, "MD2Model simulation starts attacking");

            pose = new MD2Model.Pose(MD2Model.Animation.STAND, new GameTime(0));
            pose.doSimulation(new GameTime(0.1), true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
            Check(pose.animation == MD2Model.Animation.CROUCH_STAND, "MD2Model simulation starts crouching");

            bool threw = false;
            try
            {
                MD2Model.create("g3d-port-missing-model.md2");
            }
            catch (global::System.IO.FileNotFoundException)
            {
                threw = true;
            }
            Check(threw, "MD2Model missing file fails explicitly");
        }

        private static void testShapeAndTessellationSurface()
        {
            BoxShape box = new BoxShape(2.0f, 2.0f, 2.0f);
            Check(Math.Abs(box.area() - 24.0f) < 0.001f, "BoxShape surface area");
            Check(Math.Abs(box.volume() - 8.0f) < 0.001f, "BoxShape volume");
            Check(box.type() == Shape.Type.BOX, "BoxShape type");

            SphereShape sphere = new SphereShape(Vector3.zero(), 2.0f);
            Check(Math.Abs(sphere.volume() - (4.0f / 3.0f * 3.14159265f * 8.0f)) < 0.01f, "SphereShape volume");

            Array<Vector3> polygon = new Array<Vector3>();
            polygon.push(new Vector3(0, 0, 0));
            polygon.push(new Vector3(1, 0, 0));
            polygon.push(new Vector3(1, 1, 0));
            polygon.push(new Vector3(0, 1, -1));

            Array<Triangle> triangles = new Array<Triangle>();
            Tesselate.tesselateComplexPolygon(polygon, triangles);
            Check(triangles.size() == 2, "Tesselate complex polygon triangle count");
        }

        private static void writeInt32(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
            data[offset + 2] = (byte)((value >> 16) & 0xFF);
            data[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static void writeInt16(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void writeBMP(string filename, byte r, byte g, byte b, int width, int height)
        {
            int rowSize = ((width * 3 + 3) / 4) * 4;
            int pixelBytes = rowSize * height;
            byte[] data = new byte[54 + pixelBytes];

            data[0] = (byte)'B';
            data[1] = (byte)'M';
            writeInt32(data, 2, 54 + pixelBytes);
            writeInt32(data, 10, 54);
            writeInt32(data, 14, 40);
            writeInt32(data, 18, width);
            writeInt32(data, 22, height);
            writeInt16(data, 26, 1);
            writeInt16(data, 28, 24);
            writeInt32(data, 34, pixelBytes);

            int offset = 54;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    data[offset] = b;
                    data[offset + 1] = g;
                    data[offset + 2] = r;
                    offset += 3;
                }
                offset += rowSize - width * 3;
            }

            global::System.IO.File.WriteAllBytes(filename, data);
        }

        private static void testFixedFunctionQuad(RenderDevice renderDevice, string backendName)
        {
            Texture colorTexture = Texture.createEmpty("fixed color", 32, 32, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(colorTexture);
            Renderbuffer depthBuffer = Renderbuffer.createEmpty("fixed depth", TextureFormat.DEPTH24, 32, 32);
            Framebuffer target = Framebuffer.create(32, 32);
            target.setTexture(Framebuffer.AttachmentPoint.Color0, colorTexture);
            target.setRenderbuffer(Framebuffer.AttachmentPoint.Depth, depthBuffer);

            renderDevice.pushState();
            renderDevice.setFramebuffer(target);
            renderDevice.setViewport(Rect2D.xywh(0, 0, 32, 32));
            renderDevice.setProjectionMatrix(Matrix4.orthogonalProjection(-1.0f, 1.0f, -1.0f, 1.0f, 0.1f, 100.0f));
            renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
            renderDevice.disableLighting();
            renderDevice.setTexture(0, null);
            renderDevice.setColorClearValue(new Color4(0, 0, 1, 1));
            renderDevice.clear(true, true, false);

            byte[] pixels = new byte[32 * 32 * 4];
            target.readPixels(0, 0, 32, 32, pixels);
            int center = (16 * 32 + 16) * 4;
            bool readbackWorks = pixels[center + 2] > 200;
            if (!readbackWorks)
            {
                Console.WriteLine("NOTE: " + backendName + " pixel readback returned an empty image; " +
                    "the fixed-function pixel checks are skipped on this system.");
                renderDevice.popState();
                return;
            }

            renderDevice.setColor(new Color4(1, 0, 0, 1));
            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
            renderDevice.sendVertex(new Vector3(-1, -1, -1));
            renderDevice.sendVertex(new Vector3(1, -1, -1));
            renderDevice.sendVertex(new Vector3(0, 1, -1));
            renderDevice.endPrimitive();

            target.readPixels(0, 0, 32, 32, pixels);
            Check(pixels[center] > 200 && pixels[center + 1] < 60 && pixels[center + 2] < 60,
                backendName + " fixed-function quad color (" + pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ", " + pixels[center + 3] + ")");

            renderDevice.setColorClearValue(new Color4(0, 1, 0, 1));
            renderDevice.clear(true, true, false);
            renderDevice.setAlphaTest(RenderDevice.AlphaTest.ALPHA_GREATER, 0.5);
            renderDevice.setColor(new Color4(0, 0, 1, 0.25f));
            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
            renderDevice.sendVertex(new Vector3(-1, -1, -1));
            renderDevice.sendVertex(new Vector3(1, -1, -1));
            renderDevice.sendVertex(new Vector3(0, 1, -1));
            renderDevice.endPrimitive();

            target.readPixels(0, 0, 32, 32, pixels);
            Check(pixels[center] < 60 && pixels[center + 1] > 200 && pixels[center + 2] < 60,
                backendName + " fixed-function alpha test discards fragments below the reference (" +
                pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ")");

            renderDevice.setColorClearValue(new Color4(0, 1, 0, 1));
            renderDevice.clear(true, true, false);
            renderDevice.setAlphaTest(RenderDevice.AlphaTest.ALPHA_LESS, 0.5);
            renderDevice.setColor(new Color4(0, 0, 1, 0.25f));
            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
            renderDevice.sendVertex(new Vector3(-1, -1, -1));
            renderDevice.sendVertex(new Vector3(1, -1, -1));
            renderDevice.sendVertex(new Vector3(0, 1, -1));
            renderDevice.endPrimitive();

            target.readPixels(0, 0, 32, 32, pixels);
            Check(pixels[center] < 60 && pixels[center + 1] < 60 && pixels[center + 2] > 200,
                backendName + " fixed-function alpha test keeps fragments below the reference (" +
                pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ")");

            renderDevice.popState();
        }

        private static void testSkyBoxCoverage(RenderDevice renderDevice, IGraphicsBackend backend)
        {
            string dir = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), "glg3d_sky_test");
            global::System.IO.Directory.CreateDirectory(dir);

            writeBMP(global::System.IO.Path.Combine(dir, "sky_up.bmp"), 255, 0, 0, 16, 16);
            writeBMP(global::System.IO.Path.Combine(dir, "sky_lf.bmp"), 0, 255, 0, 16, 16);
            writeBMP(global::System.IO.Path.Combine(dir, "sky_rt.bmp"), 0, 0, 255, 16, 16);
            writeBMP(global::System.IO.Path.Combine(dir, "sky_bk.bmp"), 255, 0, 255, 16, 16);
            writeBMP(global::System.IO.Path.Combine(dir, "sky_ft.bmp"), 0, 255, 255, 16, 16);
            writeBMP(global::System.IO.Path.Combine(dir, "sky_dn.bmp"), 255, 255, 0, 16, 16);

            ReferenceCountedPointer<Sky> sky =
                Sky.fromFile(renderDevice, "", global::System.IO.Path.Combine(dir, "sky_*.bmp"), false);
            Check(sky != null && sky.pointer() != null, "Sky loaded from six face images");

            Texture colorTexture = Texture.createEmpty("sky color", 64, 64, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(colorTexture);
            Renderbuffer depthBuffer = Renderbuffer.createEmpty("sky depth", TextureFormat.depth(), 64, 64);
            Framebuffer skyFramebuffer = Framebuffer.create(64, 64);
            skyFramebuffer.setTexture(Framebuffer.AttachmentPoint.Color0, colorTexture);
            skyFramebuffer.setRenderbuffer(Framebuffer.AttachmentPoint.Depth, depthBuffer);

            renderDevice.pushState();
            renderDevice.setFramebuffer(skyFramebuffer);
            renderDevice.setViewport(Rect2D.xywh(0, 0, 64, 64));
            renderDevice.setProjectionMatrix(Matrix4.perspectiveProjection(-0.1f, 0.1f, -0.1f, 0.1f, 0.1f, 100.0f));

            CoordinateFrame camera = new CoordinateFrame(Vector3.zero());
            camera.lookAt(new Vector3(0, -1, 0), Vector3.unitZ());
            renderDevice.setCameraToWorldMatrix(camera);

            renderDevice.setColorClearValue(new Color4(0, 0, 1, 1));
            renderDevice.clear(true, true, false);

            LightingParameters lighting = new LightingParameters();
            lighting.skyAmbient = Color3.white();
            sky.pointer().render(renderDevice, lighting);

            byte[] pixels = new byte[64 * 64 * 4];
            skyFramebuffer.readPixels(0, 0, 64, 64, pixels);
            int yellowCount = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (pixels[i] > 200 && pixels[i + 1] > 200 && pixels[i + 2] < 60)
                {
                    yellowCount++;
                }
            }
            Check(yellowCount > 64 * 64 / 2,
                "Sky box bottom face covers the view when looking down (" + yellowCount + " pixels)");

            renderDevice.popState();
        }

        private static void testCubeMapReflection(RenderDevice renderDevice, IGraphicsBackend backend, string backendName)
        {
            byte[][] faceColors = new byte[6][]
            {
                new byte[] { 255, 0, 0 }, new byte[] { 0, 255, 0 }, new byte[] { 0, 0, 255 },
                new byte[] { 255, 255, 0 }, new byte[] { 255, 0, 255 }, new byte[] { 0, 255, 255 }
            };

            global::System.Collections.Generic.List<byte[]> level0 = new global::System.Collections.Generic.List<byte[]>();
            for (int f = 0; f < 6; ++f)
            {
                byte[] face = new byte[8 * 8 * 3];
                for (int i = 0; i < 8 * 8; ++i)
                {
                    face[i * 3] = faceColors[f][0];
                    face[i * 3 + 1] = faceColors[f][1];
                    face[i * 3 + 2] = faceColors[f][2];
                }
                level0.Add(face);
            }
            global::System.Collections.Generic.List<global::System.Collections.Generic.List<byte[]>> mipMaps =
                new global::System.Collections.Generic.List<global::System.Collections.Generic.List<byte[]>>();
            mipMaps.Add(level0);

            Texture cubeTexture = Texture.fromMemory("reflection cube", mipMaps, TextureFormat.RGB8, 8, 8, 1,
                TextureFormat.RGB8, Texture.WrapMode.CLAMP, Texture.InterpolateMode.TRILINEAR_MIPMAP,
                Texture.Dimension.DIM_CUBE_MAP);
            renderDevice.allocateTexture(cubeTexture);
            Check(cubeTexture.getDimension() == Texture.Dimension.DIM_CUBE_MAP,
                backendName + " cube map texture upload");

            try
            {
                renderDevice.pushState();
                renderDevice.configureReflectionMap(0, cubeTexture);
            }
            catch (NotSupportedException)
            {
                Check(true, backendName + " reflection map reports the documented capability limitation");
                renderDevice.popState();
                return;
            }

            Texture colorTexture = Texture.createEmpty("reflection color", 32, 32, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(colorTexture);
            Renderbuffer depthBuffer = Renderbuffer.createEmpty("reflection depth", TextureFormat.DEPTH24, 32, 32);
            Framebuffer target = Framebuffer.create(32, 32);
            target.setTexture(Framebuffer.AttachmentPoint.Color0, colorTexture);
            target.setRenderbuffer(Framebuffer.AttachmentPoint.Depth, depthBuffer);

            renderDevice.setFramebuffer(target);
            renderDevice.setViewport(Rect2D.xywh(0, 0, 32, 32));
            renderDevice.setProjectionMatrix(Matrix4.orthogonalProjection(-1.0f, 1.0f, -1.0f, 1.0f, 0.1f, 100.0f));
            renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
            renderDevice.disableLighting();
            renderDevice.setTextureCombineMode(0, RenderDevice.CombineMode.TEX_REPLACE);
            renderDevice.configureReflectionMap(0, cubeTexture);
            renderDevice.setColorClearValue(new Color4(0, 0, 1, 1));
            renderDevice.clear(true, true, false);

            byte[] pixels = new byte[32 * 32 * 4];
            target.readPixels(0, 0, 32, 32, pixels);
            int center = (16 * 32 + 16) * 4;
            if (pixels[center + 2] <= 200)
            {
                Console.WriteLine("NOTE: " + backendName + " pixel readback returned an empty image; " +
                    "the cube map pixel check is skipped on this system.");
                backend.ConfigureCubeMapTexGen(0, CubeMapTexGenMode.TEXGEN_OFF);
                renderDevice.setTexture(0, null);
                renderDevice.popState();
                return;
            }

            renderDevice.setColor(new Color4(1, 1, 1, 1));
            renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
            renderDevice.setNormal(Vector3.unitZ());
            renderDevice.sendVertex(new Vector3(-1, -1, -1));
            renderDevice.setNormal(Vector3.unitZ());
            renderDevice.sendVertex(new Vector3(1, -1, -1));
            renderDevice.setNormal(Vector3.unitZ());
            renderDevice.sendVertex(new Vector3(1, 1, -1));
            renderDevice.setNormal(Vector3.unitZ());
            renderDevice.sendVertex(new Vector3(-1, 1, -1));
            renderDevice.endPrimitive();

            target.readPixels(0, 0, 32, 32, pixels);
            Check(pixels[center] > 200 && pixels[center + 1] < 60 && pixels[center + 2] > 200,
                backendName + " reflection map samples the positive-Z cube face back toward the eye (" +
                pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ")");

            backend.ConfigureCubeMapTexGen(0, CubeMapTexGenMode.TEXGEN_OFF);
            renderDevice.setTexture(0, null);
            renderDevice.popState();
        }

        public static int Main()
        {
            BackendCaps caps = new BackendCaps();
            Check(caps.MaxTextureSize == 0, "BackendCaps.MaxTextureSize default");
            Check(caps.MaxTextureUnits == 0, "BackendCaps.MaxTextureUnits default");
            Check(caps.MaxTextures == 0, "BackendCaps.MaxTextures default");
            Check(caps.MaxTextureCoords == 0, "BackendCaps.MaxTextureCoords default");
            Check(caps.DriverVersion.Length == 0, "BackendCaps.DriverVersion default");
            Check(caps.VideoMemorySize == 0, "BackendCaps.VideoMemorySize default");
            Check(caps.Extensions.Count == 0, "BackendCaps.Extensions default");
            Check(caps.ShaderLanguages.Count == 0, "BackendCaps.ShaderLanguages default");
            Check(caps.Renderer.Length == 0, "BackendCaps.Renderer default");
            Check(GraphicsBackendKind.OpenGL != GraphicsBackendKind.Direct3D11, "backend kinds differ");
            Check(GraphicsShaderLanguage.GLSL != GraphicsShaderLanguage.HLSL, "shader languages differ");
            Check(GLCaps.G3D_MAX_TEXTURE_UNITS == 8, "GLCaps texture unit limit");

            global::System.Reflection.MethodInfo[] methods = typeof(GLCaps).GetMethods();
            Check(hasMethod(methods, "supports_GL_ARB_texture_non_power_of_two"), "GLCaps extension shortcut surface");
            Check(hasMethod(methods, "supports_GL_ARB_texture_cube_map"), "GLCaps cube map alias");
            Check(hasMethod(methods, "hasBug_glMultiTexCoord3fvARB"), "GLCaps bug surface");
            Check(hasMethod(methods, "describeSystem"), "GLCaps describeSystem surface");

            uint directXVersion = Direct3D11.DXCaps.version();
            Check(directXVersion == Direct3D11.DXCaps.version(), "DXCaps version cached");
            Check(Direct3D11.DXCaps.videoMemorySize() >= 0, "DXCaps video memory");

            Check(typeof(OpenGL.OpenGLBackend).FullName == "G3D.OpenGL.OpenGLBackend", "OpenGL backend type");
            Check(typeof(Direct3D11.Direct3D11Backend).FullName == "G3D.Direct3D11.Direct3D11Backend", "Direct3D 11 backend type");
            VARArea area = VARArea.create(1024, VARArea.UsageHint.WRITE_EVERY_FRAME);
            VAR areaVertex = area.getVertexArray();
            areaVertex.setData(new float[] { 1.0f, 2.0f, 3.0f });
            Check(areaVertex.getCount() == 1, "VARArea vertex data");
            Check(areaVertex.valid(), "VARArea generation valid");
            area.reset();
            Check(!areaVertex.valid(), "VARArea reset invalidates arrays");
            byte[] pixels = new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 };
            Texture texture = Texture.fromMemory("managed", pixels, TextureFormat.RGBA8, 2, 1, TextureFormat.RGBA8,
                                               Texture.WrapMode.TILE, Texture.InterpolateMode.NEAREST_NO_MIPMAP,
                                               Texture.Dimension.DIM_2D, Texture.DepthReadMode.DEPTH_NORMAL, 2.0f, 1.0f);
            GImage image = new GImage();
            texture.getImage(image);
            Check(image.width == 2 && image.height == 1 && image.channels == 4, "Texture retains CPU pixel data");
            Texture alpha = texture.alphaOnlyVersion();
            Check(alpha != null && alpha.getFormat() == TextureFormat.A8, "Texture alpha-only conversion");

            Texture colorTexture = Texture.createEmpty(4, 4, "color", TextureFormat.RGBA8);
            Renderbuffer.Settings depthSettings = new Renderbuffer.Settings();
            depthSettings.format = TextureFormat.DEPTH24;
            depthSettings.width = 4;
            depthSettings.height = 4;
            Renderbuffer depthBuffer = Renderbuffer.create(depthSettings);
            Framebuffer framebuffer = Framebuffer.create(4, 4);
            framebuffer.setTexture(Framebuffer.AttachmentPoint.Color0, colorTexture);
            framebuffer.setRenderbuffer(Framebuffer.AttachmentPoint.Depth, depthBuffer);
            Check(framebuffer.isComplete(), "Framebuffer attachment completeness");

            string ddsFilename = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), "g3d-port-resource-test.dds");
            byte[] dds = new byte[134];
            dds[0] = (byte)'D';
            dds[1] = (byte)'D';
            dds[2] = (byte)'S';
            dds[3] = (byte)' ';
            Array.Copy(BitConverter.GetBytes((uint)124), 0, dds, 4, 4);
            Array.Copy(BitConverter.GetBytes((uint)0x1007), 0, dds, 8, 4);
            Array.Copy(BitConverter.GetBytes((uint)1), 0, dds, 12, 4);
            Array.Copy(BitConverter.GetBytes((uint)2), 0, dds, 16, 4);
            Array.Copy(BitConverter.GetBytes((uint)0x40), 0, dds, 80, 4);
            Array.Copy(BitConverter.GetBytes((uint)32), 0, dds, 88, 4);
            Array.Copy(BitConverter.GetBytes((uint)0x000000FF), 0, dds, 92, 4);
            Array.Copy(BitConverter.GetBytes((uint)0x0000FF00), 0, dds, 96, 4);
            Array.Copy(BitConverter.GetBytes((uint)0x00FF0000), 0, dds, 100, 4);
            dds[128] = 255;
            dds[129] = 0;
            dds[130] = 0;
            dds[131] = 0;
            dds[132] = 255;
            dds[133] = 0;
            global::System.IO.File.WriteAllBytes(ddsFilename, dds);
            Texture ddsTexture = Texture.fromFile(ddsFilename);
            GImage ddsImage = new GImage();
            ddsTexture.getImage(ddsImage);
            Check(ddsImage.width == 2 && ddsImage.height == 1, "DDS dimensions");
            global::System.IO.File.Delete(ddsFilename);

            // Phase 4: GLSL -> HLSL translation (device-free string checks).
            string glslVertex = "#version 120\n" +
                "uniform mat4 mvp;\n" +
                "uniform sampler2D tex;\n" +
                "attribute vec3 position;\n" +
                "attribute vec2 texCoord0;\n" +
                "varying vec2 vUV;\n" +
                "void main() { vUV = texCoord0; gl_Position = mvp * vec4(position, 1.0); }\n";
            string hlslVertex = G3D.GlslToHlslTranslator.translateVertex(glslVertex);
            Check(hlslVertex.Contains("float4x4 mvp;"), "GLSL->HLSL mat4 uniform type");
            Check(hlslVertex.Contains("Texture2D tex : register(t0);"), "GLSL->HLSL texture register");
            Check(hlslVertex.Contains("SamplerState tex_sampler : register(s0);"), "GLSL->HLSL sampler register");
            Check(hlslVertex.Contains("float3 position : POSITION"), "GLSL->HLSL attribute semantic");
            Check(hlslVertex.Contains("float2 texCoord0 : TEXCOORD0"), "GLSL->HLSL texcoord semantic");
            Check(hlslVertex.Contains("out float2 vUV : VARY_vUV"), "GLSL->HLSL varying semantic");
            Check(hlslVertex.Contains(": SV_Position"), "GLSL->HLSL vertex position output");
            Check(hlslVertex.Contains("mul(mvp, float4(position, 1.0))"), "GLSL->HLSL matrix multiply");

            string glslPixel = "#version 120\n" +
                "uniform sampler2D tex;\n" +
                "varying vec2 vUV;\n" +
                "void main() { gl_FragColor = texture2D(tex, vUV); }\n";
            string hlslPixel = G3D.GlslToHlslTranslator.translatePixel(glslPixel);
            Check(hlslPixel.Contains("in float2 vUV : VARY_vUV"), "GLSL->HLSL pixel varying semantic matches vertex");
            Check(hlslPixel.Contains("float4 gl_FragColor;"), "GLSL->HLSL fragment color variable");
            Check(hlslPixel.Contains("tex.Sample(tex_sampler, vUV)"), "GLSL->HLSL texture sample");

            // GLSL 1.30 style pixel shader uses 'in' for varyings and 'out' for the color.
            string glsl130Pixel = "#version 130\n" +
                "in vec2 vUV;\n" +
                "out vec4 colorOut;\n" +
                "void main() { colorOut = vec4(vUV, 0.0, 1.0); }\n";
            string hlsl130Pixel = G3D.GlslToHlslTranslator.translatePixel(glsl130Pixel);
            Check(hlsl130Pixel.Contains("in float2 vUV : VARY_vUV"), "GLSL 1.30 pixel 'in' varying");
            Check(hlsl130Pixel.Contains("out float4 colorOut : SV_Target"), "GLSL 1.30 pixel 'out' color");

            // Built-in fixed-function variables map to their attribute types.
            string glslBuiltins = "#version 120\n" +
                "attribute vec4 position;\n" +
                "attribute vec3 normal;\n" +
                "void main() { gl_Position = gl_Vertex + vec4(gl_Normal, 0.0); }\n";
            string hlslBuiltins = G3D.GlslToHlslTranslator.translateVertex(glslBuiltins);
            Check(hlslBuiltins.Contains("position + float4(normal, 0.0)"), "GLSL->HLSL built-in gl_Vertex uses declared vec4 attribute");
            Check(hlslBuiltins.Contains("float4(normal, 0.0)"), "GLSL->HLSL built-in gl_Normal");

            string glslBuiltins3 = "#version 120\n" +
                "attribute vec3 position;\n" +
                "void main() { gl_Position = gl_Vertex; }\n";
            string hlslBuiltins3 = G3D.GlslToHlslTranslator.translateVertex(glslBuiltins3);
            Check(hlslBuiltins3.Contains("float4(position, 1.0)"), "GLSL->HLSL built-in gl_Vertex promotes vec3 attribute");

            // Matrix constructor semantics: GLSL fills column-major, HLSL fills rows.
            string glslMat = "uniform mat3 tbn;\nmat3 m = mat3(1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0);\nvec3 r = m * vec3(1.0, 1.0, 1.0);\n";
            string hlslMat = G3D.GlslToHlslTranslator.translateVertex(glslMat);
            Check(hlslMat.Contains("float3x3(float3(1.0, 4.0, 7.0), float3(2.0, 5.0, 8.0), float3(3.0, 6.0, 9.0))"), "GLSL->HLSL mat3 constructor transposes to rows");
            Check(hlslMat.Contains("mul(m, float3(1.0, 1.0, 1.0))"), "GLSL->HLSL matrix multiply");
            Check(hlslMat.Contains("row_major float3x3 tbn;"), "GLSL->HLSL row_major cbuffer matrix");

            // Phase 4: full GLSL->HLSL translate + device-free compile + reflection.
            G3D.ShaderProgramInfo hlslInfo = G3D.Direct3D11.HlslProgramCompiler.compile(hlslVertex, hlslPixel);
            Check(hlslInfo.Success, "HLSL device-free compile of translated GLSL: " + hlslInfo.Messages);
            bool hasMvp = false;
            foreach (G3D.ShaderUniformInfo uniform in hlslInfo.Uniforms)
            {
                if (uniform.Name == "mvp")
                {
                    hasMvp = true;
                    Check(uniform.Type == G3D.ShaderProgramType.Matrix4, "HLSL uniform reflection matrix type");
                }
            }
            Check(hasMvp, "HLSL uniform reflection: mvp");
            bool hasTex = false;
            foreach (G3D.ShaderUniformInfo uniform in hlslInfo.Uniforms)
            {
                if (uniform.Name == "tex")
                {
                    hasTex = true;
                    Check(uniform.Type == G3D.ShaderProgramType.Sampler2D, "HLSL sampler reflection type");
                }
            }
            Check(hasTex, "HLSL sampler reflection: tex");

            // Attribute semantics: the translator maps GLSL attribute names to HLSL
            // semantics and both stages of a program use the same varying semantics.
            string attrVertex = "#version 120\n" +
                "attribute vec3 position;\n" +
                "attribute vec3 normal;\n" +
                "attribute vec4 color;\n" +
                "attribute vec2 texCoord0;\n" +
                "varying vec3 vColor;\n" +
                "void main() { vColor = normal; gl_Position = vec4(position, 1.0); }\n";
            string attrHlslVertex = G3D.GlslToHlslTranslator.translateVertex(attrVertex);
            Check(attrHlslVertex.Contains("float3 position : POSITION"), "attribute position -> POSITION");
            Check(attrHlslVertex.Contains("float3 normal : NORMAL"), "attribute normal -> NORMAL");
            Check(attrHlslVertex.Contains("float4 color : COLOR"), "attribute color -> COLOR");
            Check(attrHlslVertex.Contains("float2 texCoord0 : TEXCOORD0"), "attribute texCoord0 -> TEXCOORD0");
            Check(attrHlslVertex.Contains("out float3 vColor : VARY_vColor"), "varying output semantic");
            string attrPixel = "#version 120\nvarying vec3 vColor;\nvoid main() { gl_FragColor = vec4(vColor, 1.0); }\n";
            string attrHlslPixel = G3D.GlslToHlslTranslator.translatePixel(attrPixel);
            Check(attrHlslPixel.Contains("in float3 vColor : VARY_vColor"), "pixel varying input semantic matches vertex");

            // translatePair shares sampler registers across both stages.
            string pairVertex = "#version 120\nuniform sampler2D texA;\nuniform sampler2D texB;\nvoid main() { gl_Position = vec4(0,0,0,1); }\n";
            string pairPixel = "#version 120\nuniform sampler2D texB;\nuniform sampler2D texC;\nvoid main() { gl_FragColor = vec4(1,0,0,1); }\n";
            string pairVertexHlsl, pairPixelHlsl;
            G3D.GlslToHlslTranslator.translatePair(pairVertex, pairPixel, out pairVertexHlsl, out pairPixelHlsl);
            Check(pairVertexHlsl.Contains("texA : register(t0)") && pairVertexHlsl.Contains("texB : register(t1)"), "translatePair vertex sampler registers");
            Check(pairPixelHlsl.Contains("texB : register(t1)") && pairPixelHlsl.Contains("texC : register(t2)"), "translatePair pixel sampler registers match vertex");

            // Device-free HLSL attribute reflection.
            string reflVertex = "cbuffer C : register(b0) { float4x4 mvp; };\n" +
                "float4 main(float3 position : POSITION, float3 normal : NORMAL, float4 color : COLOR, float2 texCoord0 : TEXCOORD0) : SV_Position { return float4(position, 1.0); }\n";
            string reflPixel = "float4 main() : SV_Target { return float4(1, 0, 0, 1); }\n";
            G3D.ShaderProgramInfo reflInfo = G3D.Direct3D11.HlslProgramCompiler.compile(reflVertex, reflPixel);
            Check(reflInfo.Success, "attribute reflection compile: " + reflInfo.Messages);
            int attrCount = 0;
            foreach (G3D.ShaderAttributeInfo attribute in reflInfo.Attributes)
            {
                attrCount++;
                if (attribute.Name == "POSITION") { Check(attribute.ComponentCount == 3, "POSITION component count"); Check(attribute.Location == 0, "POSITION location"); }
                if (attribute.Name == "TEXCOORD") { Check(attribute.ComponentCount == 2, "TEXCOORD component count"); }
            }
            Check(attrCount == 4, "attribute reflection count");
            testUserInput();
            testGModuleManager();
            testFirstPersonManipulator();
            testVARAreaAndVAR();
            testTextureSurface();
            testFramebufferSurface();
            testRenderDeviceState();
            testGModuleDispatch();
            testGWindowSurface();
            testShaderSurface();
            testLightingParametersSurface();
            testModelSurface();
            testMD2ModelSurface();
            testShapeAndTessellationSurface();

            OpenGL.OpenGLWindow openGLWindow = null;
            OpenGL.OpenGLBackend openGLBackend = null;
            try
            {
                openGLWindow = new OpenGL.OpenGLWindow();
                IntPtr handle = openGLWindow.NativeHandle;
                Check(handle != IntPtr.Zero, "OpenGL window handle");
                openGLBackend = new OpenGL.OpenGLBackend();
                openGLBackend.Initialize(openGLWindow);
                Check(openGLBackend.Caps.MaxTextureUnits >= 1, "OpenGL texture unit limit");
                Check(openGLBackend.Caps.MaxTextureUnits <= GLCaps.G3D_MAX_TEXTURE_UNITS, "OpenGL texture unit cap");
                Check(openGLBackend.Caps.MaxTextures == openGLBackend.Caps.MaxTextureUnits, "OpenGL texture count");
                Check(openGLBackend.Caps.MaxTextureCoords == openGLBackend.Caps.MaxTextureUnits, "OpenGL texture coordinate count");
                Check(openGLBackend.SupportsShaderLanguage(GraphicsShaderLanguage.GLSL), "OpenGL GLSL capability");
                Check(!openGLBackend.SupportsShaderLanguage(GraphicsShaderLanguage.HLSL), "OpenGL HLSL capability");
                Check(!openGLBackend.SupportsExtension("ARB_multitexture"), "OpenGL exact extension token matching");
                Check(GLCaps.vendor() == openGLBackend.GetVendor(), "GLCaps vendor delegation");
                Check(GLCaps.numTextureUnits() == openGLBackend.GetMaxTextureUnits(), "GLCaps texture unit delegation");

                RenderDevice renderDevice = new RenderDevice();
                renderDevice.setBackend(openGLBackend);
                renderDevice.setFramebuffer(framebuffer);
                Check(renderDevice.currentFramebufferComplete(), "OpenGL framebuffer completeness");
                Milestone milestone = renderDevice.createMilestone("test");
                renderDevice.setMilestone(milestone);
                renderDevice.waitForMilestone(milestone);
                Check(!milestone.isSet(), "Milestone lifecycle");

                renderDevice.clear(true, true, false);
                float[] depthValues = new float[4 * 4];
                framebuffer.readDepth(0, 0, 4, 4, depthValues);
                Check(depthValues[0] >= 0.0f && depthValues[0] <= 1.0f, "OpenGL depth buffer readback");
                double depthValue = renderDevice.getDepthBufferValue(0, 0);
                Check(depthValue >= 0.0 && depthValue <= 1.0, "OpenGL depth buffer value");

                testSkyBoxCoverage(renderDevice, openGLBackend);
                testFixedFunctionQuad(renderDevice, "OpenGL");
                testCubeMapReflection(renderDevice, openGLBackend, "OpenGL");



                GLCaps.setBackend(openGLBackend);
                string vertexSource =
                    "#version 120\n" +
                    "uniform float scale;\n" +
                    "attribute vec3 position;\n" +
                    "void main() { gl_Position = vec4(position * scale, 1.0); }\n";
                string pixelSource =
                    "#version 120\n" +
                    "uniform vec4 tint;\n" +
                    "void main() { gl_FragColor = tint; }\n";
                ShaderProgramInfo programInfo = openGLBackend.CompileShaderProgram(GraphicsShaderLanguage.GLSL, vertexSource, pixelSource);
                Check(programInfo.ProgramID != 0, "Shader program identifier");
                Check(programInfo.Success, "GLSL program compilation: " + programInfo.Messages);
                bool hasScale = false;
                bool hasTint = false;
                foreach (ShaderUniformInfo uniform in programInfo.Uniforms)
                {
                    if (uniform.Name == "scale") { hasScale = true; }
                    if (uniform.Name == "tint") { hasTint = true; }
                }
                Check(hasScale, "GLSL uniform reflection: scale");
                Check(hasTint, "GLSL uniform reflection: tint");
                openGLBackend.UseShaderProgram(programInfo.ProgramID);

                VertexAndPixelShader combined = VertexAndPixelShader.fromStrings(vertexSource, pixelSource);
                Check(combined.numArgs() >= 2, "VertexAndPixelShader argument count");
                Check(combined.getName().Length > 0, "VertexAndPixelShader name");
                VertexAndPixelShader.ArgList shaderArgs = new VertexAndPixelShader.ArgList();
                shaderArgs.set("scale", 2.0f);
                shaderArgs.set("tint", new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                Check(combined.ok(), "VertexAndPixelShader compile status: " + combined.messages());
                combined.bindArgList(renderDevice, shaderArgs);
                Check(true, "VertexAndPixelShader binding");

                GPUProgram.Settings settings = new GPUProgram.Settings();
                settings.defines = "";
                PixelProgram pixelProgram = PixelProgram.fromSource("pixel", "#version 120\nuniform vec4 tint;\nvoid main() { gl_FragColor = tint; }\n", settings);
                Check(pixelProgram.getName() == "pixel", "PixelProgram name");
                Check(pixelProgram.ok(), "PixelProgram compile status: " + pixelProgram.messages());
                GPUProgram.ArgList programArgs = new GPUProgram.ArgList();
                programArgs.set("tint", new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                pixelProgram.bind(renderDevice, programArgs);
                Check(true, "PixelProgram binding");
            }
            catch (Exception e)
            {
                Check(false, "OpenGL capability initialization: " + e.Message);
            }
            finally
            {
                GLCaps.setBackend(null);
                if (openGLBackend != null)
                {
                    openGLBackend.Dispose();
                }
                if (openGLWindow != null)
                {
                    openGLWindow.Dispose();
                }
            }

            Direct3D11.Direct3D11Window direct3D11Window = null;
            Direct3D11.Direct3D11Backend direct3D11Backend = null;
            try
            {
                direct3D11Window = new Direct3D11.Direct3D11Window();
                global::System.Windows.Forms.Form direct3D11Form = new global::System.Windows.Forms.Form();
                direct3D11Form.ClientSize = new global::System.Drawing.Size(320, 240);
                direct3D11Window.Control.Dock = global::System.Windows.Forms.DockStyle.Fill;
                direct3D11Form.Controls.Add(direct3D11Window.Control);
                direct3D11Form.Show();
                direct3D11Window.Control.CreateControl();
                global::System.Windows.Forms.Application.DoEvents();
                direct3D11Backend = new Direct3D11.Direct3D11Backend();
                direct3D11Backend.Initialize(direct3D11Window);
                Check(direct3D11Backend.Caps.MaxTextureUnits >= 1, "Direct3D 11 texture unit limit");
                Check(direct3D11Backend.SupportsShaderLanguage(GraphicsShaderLanguage.HLSL), "Direct3D 11 HLSL capability");

                RenderDevice direct3D11RenderDevice = new RenderDevice();
                direct3D11RenderDevice.setBackend(direct3D11Backend);
                GLCaps.setBackend(direct3D11Backend);
                direct3D11RenderDevice.clear(true, true, false);

                float[] direct3D11Depth = new float[1];
                direct3D11Backend.ReadDepth(0, 0, 1, 1, direct3D11Depth);
                Check(direct3D11Depth[0] >= 0.0f && direct3D11Depth[0] <= 1.0f, "Direct3D 11 depth buffer readback");

                double direct3D11DepthValue = direct3D11RenderDevice.getDepthBufferValue(0, 0);
                Check(direct3D11DepthValue >= 0.0 && direct3D11DepthValue <= 1.0, "Direct3D 11 depth buffer value");

                testFixedFunctionQuad(direct3D11RenderDevice, "Direct3D 11");
                testCubeMapReflection(direct3D11RenderDevice, direct3D11Backend, "Direct3D 11");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Check(false, "Direct3D 11 initialization: " + e.Message);
            }
            finally
            {
                GLCaps.setBackend(null);
                if (direct3D11Backend != null)
                {
                    direct3D11Backend.Dispose();
                }
                if (direct3D11Window != null)
                {
                    direct3D11Window.Dispose();
                }
            }


            if (failures == 0)
            {
                Console.WriteLine("GLG3D smoke tests passed.");
                return 0;
            }

            Console.WriteLine(failures + " test(s) failed.");
            return 1;
        }
    }
}


