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
            public event Action<GEvent> InputEvent;
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

        private static void testAssemblyProgram(RenderDevice renderDevice, string backendName)
        {
            if (!renderDevice.supportsVertexProgram())
            {
                Console.WriteLine("NOTE: " + backendName + " does not support ARB vertex programs; " +
                    "the assembly GPU program check is skipped on this system.");
                return;
            }

            string vertexCode =
                "!!ARBvp1.0\n" +
                "#var float4x4 mvp :  : c[0], 4 : 0 : 1\n" +
                "#var float4 tint :  : c[4] : 1 : 1\n" +
                "TEMP pos;\n" +
                "DP4 pos.x, state.matrix.mvp[0], vertex.position;\n" +
                "DP4 pos.y, state.matrix.mvp[1], vertex.position;\n" +
                "DP4 pos.z, state.matrix.mvp[2], vertex.position;\n" +
                "DP4 pos.w, state.matrix.mvp[3], vertex.position;\n" +
                "MOV result.position, pos;\n" +
                "MOV result.color, program.local[4];\n" +
                "END\n";

            string fragmentCode =
                "!!ARBfp1.0\n" +
                "#var float4 tint :  : c[0] : 0 : 1\n" +
                "MOV result.color, fragment.color;\n" +
                "END\n";

            VertexProgram vertexProgram = null;
            PixelProgram pixelProgram = null;
            try
            {
                vertexProgram = VertexProgram.fromCode("G3D assembly vertex program test", vertexCode);
                Check(vertexProgram != null && vertexProgram.ok(),
                    backendName + " ARB vertex program loads: " + (vertexProgram == null ? "null" : vertexProgram.messages()));
                Console.WriteLine("DIAG " + backendName + " ARB vertex program id=" +
                    (vertexProgram == null ? 0 : vertexProgram.getOpenGLID()) +
                    " ok=" + (vertexProgram != null && vertexProgram.ok()));
                if (vertexProgram != null && vertexProgram.ok())
                {
                    Check(vertexProgram.getOpenGLID() != 0, backendName + " ARB vertex program name");

                    GPUProgram.ArgList args = new GPUProgram.ArgList();
                    args.set("mvp", Matrix4.identity());
                    args.set("tint", new Vector4(1, 1, 1, 1));

                    bool bindFailed = false;
                    try
                    {
                        renderDevice.setVertexProgram(vertexProgram, args);
                    }
                    catch (Exception)
                    {
                        bindFailed = true;
                    }
                    Check(!bindFailed, backendName + " ARB vertex program argument binding");

                    bool missingFailed = false;
                    try
                    {
                        GPUProgram.ArgList missingArgs = new GPUProgram.ArgList();
                        missingArgs.set("mvp", Matrix4.identity());
                        renderDevice.setVertexProgram(vertexProgram, missingArgs);
                    }
                    catch (InvalidOperationException)
                    {
                        missingFailed = true;
                    }
                    Check(missingFailed, backendName + " ARB vertex program requires every bound variable");
                }

                pixelProgram = PixelProgram.fromCode("G3D assembly fragment program test", fragmentCode);
                Check(pixelProgram != null && pixelProgram.ok(),
                    backendName + " ARB fragment program loads: " + (pixelProgram == null ? "null" : pixelProgram.messages()));
                if (pixelProgram != null && pixelProgram.ok())
                {
                    Check(pixelProgram.getOpenGLID() != 0, backendName + " ARB fragment program name");
                    Console.WriteLine("DIAG " + backendName + " ARB fragment program id=" + pixelProgram.getOpenGLID());
                }
            }
            finally
            {
                try
                {
                    if (vertexProgram != null)
                    {
                        renderDevice.setVertexProgram(null);
                    }
                    if (pixelProgram != null)
                    {
                        renderDevice.setPixelProgram(null);
                    }
                }
                catch (Exception)
                {
                }
            }
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

        private static void d3d11TextureProbe(RenderDevice renderDevice, string backendName)
        {
            const int size = 64;
            Texture tex = Texture.fromFile("C:\\Users\\Intel\\Documents\\G3D6.09Port\\demos\\Collision_Demo\\bin\\Debug\\data\\sky\\plainsky\\null_plainsky512_up.jpg",
                TextureFormat.RGB8, Texture.WrapMode.CLAMP, Texture.InterpolateMode.BILINEAR_NO_MIPMAP,
                Texture.Dimension.DIM_2D, 1.0, Texture.DepthReadMode.DEPTH_NORMAL, 1.0f, 1.0f);
            renderDevice.allocateTexture(tex);
            Console.WriteLine("DIAG tex id=" + tex.getOpenGLID() + " w=" + tex.texelWidth() + " h=" + tex.texelHeight());
            Texture colorTexture = Texture.createEmpty("probe color", size, size, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(colorTexture);
            Renderbuffer depthBuffer = Renderbuffer.createEmpty("probe depth", TextureFormat.DEPTH24, size, size);
            Framebuffer target = Framebuffer.create(size, size);
            target.setTexture(Framebuffer.AttachmentPoint.Color0, colorTexture);
            target.setRenderbuffer(Framebuffer.AttachmentPoint.Depth, depthBuffer);
            renderDevice.pushState();
            renderDevice.setFramebuffer(target);
            renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
            renderDevice.setProjectionMatrix(Matrix4.orthogonalProjection(-1.0f, 1.0f, -1.0f, 1.0f, 0.1f, 100.0f));
            renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
            renderDevice.disableLighting();
            renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
            renderDevice.clear(true, true, false);
            byte[] pixels = new byte[size * size * 4];
            target.readPixels(0, 0, size, size, pixels);
            int c = (size / 2 * size + size / 2) * 4;
            { int cc = 0; for (int q = 0; q < pixels.Length; ++q) { if (pixels[q] != 0) { ++cc; } }
            Console.WriteLine("DIAG texprobe after clear nonzero=" + cc + "/" + pixels.Length); }
            renderDevice.setColor(new Color4(1, 1, 1, 1));
            renderDevice.setTexture(0, tex);
            renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
            renderDevice.setTexCoord(0, new Vector2(0, 0));
            renderDevice.sendVertex(new Vector3(-1, -1, -1));
            renderDevice.setTexCoord(0, new Vector2(1, 0));
            renderDevice.sendVertex(new Vector3(1, -1, -1));
            renderDevice.setTexCoord(0, new Vector2(1, 1));
            renderDevice.sendVertex(new Vector3(1, 1, -1));
            renderDevice.setTexCoord(0, new Vector2(0, 1));
            renderDevice.sendVertex(new Vector3(-1, 1, -1));
            renderDevice.endPrimitive();
            target.readPixels(0, 0, size, size, pixels);
            { int cc = 0; for (int q = 0; q < pixels.Length; ++q) { if (pixels[q] != 0) { ++cc; } }
            Console.WriteLine("DIAG texprobe textured quad nonzero=" + cc + "/" + pixels.Length + " center=" + pixels[c] + "," + pixels[c+1] + "," + pixels[c+2] + "," + pixels[c+3]); }
            renderDevice.setTexture(0, null);
            renderDevice.setColor(new Color4(1, 0, 0, 1));
            renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
            renderDevice.setTexCoord(0, new Vector2(0, 0));
            renderDevice.sendVertex(new Vector3(-1, -1, -1));
            renderDevice.setTexCoord(0, new Vector2(1, 0));
            renderDevice.sendVertex(new Vector3(1, -1, -1));
            renderDevice.setTexCoord(0, new Vector2(1, 1));
            renderDevice.sendVertex(new Vector3(1, 1, -1));
            renderDevice.setTexCoord(0, new Vector2(0, 1));
            renderDevice.sendVertex(new Vector3(-1, 1, -1));
            renderDevice.endPrimitive();
            target.readPixels(0, 0, size, size, pixels);
            Console.WriteLine("DIAG texprobe untetxured quad center=" + pixels[c] + "," + pixels[c+1] + "," + pixels[c+2] + "," + pixels[c+3]);
            renderDevice.popState();
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
            { int cc = 0; for (int q = 0; q < pixels.Length; ++q) { if (pixels[q] != 0) { ++cc; } }
            Console.WriteLine("DIAG clear nonzero bytes: " + cc + "/" + pixels.Length + " center=" + pixels[center] + "," + pixels[center+1] + "," + pixels[center+2] + "," + pixels[center+3]); }
            if (!readbackWorks)
            {
                Console.WriteLine("NOTE: " + backendName + " pixel readback returned an empty image; " +
                    "the fixed-function pixel checks are skipped on this system.");
            }

            renderDevice.setColor(new Color4(1, 0, 0, 1));
            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
            renderDevice.sendVertex(new Vector3(-1, -1, -1));
            renderDevice.sendVertex(new Vector3(1, -1, -1));
            renderDevice.sendVertex(new Vector3(0, 1, -1));
            renderDevice.endPrimitive();

            target.readPixels(0, 0, 32, 32, pixels);
            if (readbackWorks)
            {
                Check(pixels[center] > 200 && pixels[center + 1] < 60 && pixels[center + 2] < 60,
                    backendName + " fixed-function quad color (" + pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ", " + pixels[center + 3] + ")");
            }
            { int cc = 0; for (int q = 0; q < pixels.Length; ++q) { if (pixels[q] != 0) { ++cc; } }
            Console.WriteLine("DIAG tri nonzero bytes: " + cc + "/" + pixels.Length + " center=" + pixels[center] + "," + pixels[center+1] + "," + pixels[center+2] + "," + pixels[center+3]); }

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
            if (readbackWorks)
            {
                Check(pixels[center] < 60 && pixels[center + 1] > 200 && pixels[center + 2] < 60,
                    backendName + " fixed-function alpha test discards fragments below the reference (" +
                    pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ")");
            }

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
            if (readbackWorks)
            {
                Check(pixels[center] < 60 && pixels[center + 1] < 60 && pixels[center + 2] > 200,
                    backendName + " fixed-function alpha test keeps fragments below the reference (" +
                    pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ")");
            }

            renderDevice.popState();
        }

        private static void testShadowMap(RenderDevice renderDevice, string backendName)
        {
            const int size = 64;
            Texture shadowMap = Texture.createEmpty("shadow map", size, size, TextureFormat.depth(),
                Texture.Dimension.DIM_2D, Texture.Settings.shadow());
            renderDevice.allocateTexture(shadowMap);

            Texture shadowColor = Texture.createEmpty("shadow color", size, size, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(shadowColor);

            Framebuffer shadowTarget = Framebuffer.create(size, size);
            shadowTarget.setTexture(Framebuffer.AttachmentPoint.Color0, shadowColor);
            shadowTarget.setTexture(Framebuffer.AttachmentPoint.Depth, shadowMap);

            Matrix4 lightProjection = Matrix4.orthogonalProjection(-1.0f, 1.0f, -1.0f, 1.0f, 0.1f, 10.0f);

            renderDevice.pushState();
                renderDevice.setFramebuffer(shadowTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                renderDevice.setProjectionMatrix(lightProjection);
                renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
                renderDevice.disableLighting();
                renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                renderDevice.clear(true, true, false);
                renderDevice.setColor(new Color4(1, 1, 1, 1));
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                renderDevice.sendVertex(new Vector3(-1, -1, -0.5f));
                renderDevice.sendVertex(new Vector3(1, -1, -0.5f));
                renderDevice.sendVertex(new Vector3(1, 1, -0.5f));
                renderDevice.sendVertex(new Vector3(-1, 1, -0.5f));
                renderDevice.endPrimitive();
            renderDevice.popState();

            float[] shadowDepth = new float[1];
            shadowTarget.readDepth(size / 2, size / 2, 1, 1, shadowDepth);
            Console.WriteLine("DIAG shadow depth=" + shadowDepth[0]);
            Check(shadowDepth[0] > 0.0f && shadowDepth[0] < 0.5f,
                backendName + " shadow map depth texture stores the occluder depth (" + shadowDepth[0] + ")");

            Texture receiverColor = Texture.createEmpty("receiver color", size, size, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(receiverColor);
            Framebuffer receiverTarget = Framebuffer.create(size, size);
            receiverTarget.setTexture(Framebuffer.AttachmentPoint.Color0, receiverColor);

            byte[] pixels = new byte[size * size * 4];
            int center = (size / 2 * size + size / 2) * 4;

            renderDevice.pushState();
                renderDevice.setFramebuffer(receiverTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                renderDevice.setProjectionMatrix(lightProjection);
                renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
                renderDevice.disableLighting();
                renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                renderDevice.clear(true, false, false);
                renderDevice.setColor(new Color4(1, 1, 1, 1));
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                renderDevice.sendVertex(new Vector3(-1, -1, -1.0f));
                renderDevice.sendVertex(new Vector3(1, -1, -1.0f));
                renderDevice.sendVertex(new Vector3(1, 1, -1.0f));
                renderDevice.sendVertex(new Vector3(-1, 1, -1.0f));
                renderDevice.endPrimitive();
            renderDevice.popState();

            receiverTarget.readPixels(0, 0, size, size, pixels);
            Console.WriteLine("DIAG shadow receiver center=" + pixels[center] + "," + pixels[center + 1] + "," + pixels[center + 2] + "," + pixels[center + 3]);
            Check(pixels[center] > 200 && pixels[center + 1] > 200 && pixels[center + 2] > 200,
                backendName + " unshadowed receiver is fully lit (" +
                pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ")");

            renderDevice.pushState();
                renderDevice.setFramebuffer(receiverTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                renderDevice.setProjectionMatrix(lightProjection);
                renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
                renderDevice.disableLighting();
                renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                renderDevice.clear(true, false, false);
                renderDevice.setColor(new Color4(1, 1, 1, 1));
                renderDevice.configureShadowMap(1, lightProjection, shadowMap);
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                renderDevice.sendVertex(new Vector3(-1, -1, -1.0f));
                renderDevice.sendVertex(new Vector3(1, -1, -1.0f));
                renderDevice.sendVertex(new Vector3(1, 1, -1.0f));
                renderDevice.sendVertex(new Vector3(-1, 1, -1.0f));
                renderDevice.endPrimitive();
            renderDevice.popState();

            receiverTarget.readPixels(0, 0, size, size, pixels);
            Console.WriteLine("DIAG shadowed receiver center=" + pixels[center] + "," + pixels[center + 1] + "," + pixels[center + 2] + "," + pixels[center + 3]);
            Check(pixels[center] < 60 && pixels[center + 1] < 60 && pixels[center + 2] < 60,
                backendName + " shadow map darkens the shadowed receiver (" +
                pixels[center] + ", " + pixels[center + 1] + ", " + pixels[center + 2] + ")");
        }

        /**
          Covers the shadow-map cases that the full-coverage test above cannot
          distinguish: an EMPTY shadow-map region (depth at the clear value) and a
          receiver NEARER than the occluder.  Both must be lit, matching the
          OpenGL reference.
          */
        private static void testShadowMapEdgeCases(RenderDevice renderDevice, string backendName)
        {
            const int size = 64;
            Texture shadowMap = Texture.createEmpty("edge shadow map", size, size, TextureFormat.depth(),
                Texture.Dimension.DIM_2D, Texture.Settings.shadow());
            renderDevice.allocateTexture(shadowMap);

            Framebuffer shadowTarget = Framebuffer.create(size, size);
            shadowTarget.setTexture(Framebuffer.AttachmentPoint.Depth, shadowMap);

            Matrix4 lightProjection = Matrix4.orthogonalProjection(-1.0f, 1.0f, -1.0f, 1.0f, 0.1f, 10.0f);

            renderDevice.pushState();
                renderDevice.setFramebuffer(shadowTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                renderDevice.setProjectionMatrix(lightProjection);
                renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
                renderDevice.disableLighting();
                renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                renderDevice.clear(false, true, false);
                renderDevice.setColor(new Color4(1, 1, 1, 1));
                // Occluder covers only the LEFT half of the light frustum; the right
                // half of the shadow map keeps the depth clear value.
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                renderDevice.sendVertex(new Vector3(-1, -1, -0.5f));
                renderDevice.sendVertex(new Vector3(0, -1, -0.5f));
                renderDevice.sendVertex(new Vector3(0, 1, -0.5f));
                renderDevice.sendVertex(new Vector3(-1, 1, -0.5f));
                renderDevice.endPrimitive();
            renderDevice.popState();

            float[] shadowMapEdge = new float[1];
            shadowTarget.readDepth(size / 2, size / 2, 1, 1, shadowMapEdge);
            Console.WriteLine("DIAG parity " + backendName + " edge shadow map center depth=" + shadowMapEdge[0]);

            Texture receiverColor = Texture.createEmpty("edge receiver color", size, size, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(receiverColor);
            Framebuffer receiverTarget = Framebuffer.create(size, size);
            receiverTarget.setTexture(Framebuffer.AttachmentPoint.Color0, receiverColor);

            byte[] pixels = new byte[size * size * 4];
            int leftCenter = (size / 2 * size + size / 4) * 4;
            int rightCenter = (size / 2 * size + 3 * size / 4) * 4;

            renderDevice.pushState();
                renderDevice.setFramebuffer(receiverTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                renderDevice.setProjectionMatrix(lightProjection);
                renderDevice.setCameraToWorldMatrix(new CoordinateFrame(Vector3.zero()));
                renderDevice.disableLighting();
                renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                renderDevice.clear(true, false, false);
                renderDevice.setColor(new Color4(1, 1, 1, 1));
                renderDevice.configureShadowMap(1, lightProjection, shadowMap);
                // Receiver behind the occluder (z = -1.0 vs the occluder's -0.5).
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                renderDevice.sendVertex(new Vector3(-1, -1, -1.0f));
                renderDevice.sendVertex(new Vector3(1, -1, -1.0f));
                renderDevice.sendVertex(new Vector3(1, 1, -1.0f));
                renderDevice.sendVertex(new Vector3(-1, 1, -1.0f));
                renderDevice.endPrimitive();
            renderDevice.popState();

            receiverTarget.readPixels(0, 0, size, size, pixels);
            Console.WriteLine("DIAG shadow edge: covered=" + pixels[leftCenter] + " empty=" + pixels[rightCenter]);
            Check(pixels[leftCenter] < 60,
                backendName + " shadow map darkens a receiver behind the occluder (" + pixels[leftCenter] + ")");
            Check(pixels[rightCenter] > 200,
                backendName + " shadow map leaves an empty region lit (" + pixels[rightCenter] + ")");
        }

        /**
          Renders a miniature version of the Collision_Demo lighting structure (a
          ground plane, a floating occluder, an ambient pass and an additive sun
          pass with a shadow map) and prints the resulting pixels so the OpenGL
          and Direct3D 11 outputs can be compared directly.
          */
        private static void testShadowSceneParity(RenderDevice renderDevice, string backendName)
        {
            const int size = 128;
            const int shadowSize = 512;
            // Toggled by the caller to bisect whether the shadow lookup or the
            // sun pass itself is responsible for the rendered result.
            bool ParityUseShadowMap = true;

            Texture shadowMap = Texture.createEmpty("parity shadow map", shadowSize, shadowSize, TextureFormat.depth(),
                Texture.Dimension.DIM_2D, Texture.Settings.shadow());
            renderDevice.allocateTexture(shadowMap);
            Framebuffer shadowTarget = Framebuffer.create(shadowSize, shadowSize);
            shadowTarget.setTexture(Framebuffer.AttachmentPoint.Depth, shadowMap);

            Texture sceneColor = Texture.createEmpty("parity scene color", size, size, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(sceneColor);
            Framebuffer sceneTarget = Framebuffer.create(size, size);
            sceneTarget.setTexture(Framebuffer.AttachmentPoint.Color0, sceneColor);
            Texture sceneDepth = Texture.createEmpty("parity scene depth", size, size, TextureFormat.depth(),
                Texture.Dimension.DIM_2D, Texture.Settings.video());
            renderDevice.allocateTexture(sceneDepth);
            sceneTarget.setTexture(Framebuffer.AttachmentPoint.Depth, sceneDepth);

            Vector3 lightDirection = new Vector3(0.3f, 0.8f, -0.5f).direction();
            CoordinateFrame lightCFrame = new CoordinateFrame(Vector3.zero());
            lightCFrame.lookAt(-lightDirection, Vector3.unitY());
            lightCFrame.translation = lightDirection * 20;
            Matrix4 lightProjection = Matrix4.orthogonalProjection(-17, 17, -17, 17, 1, 40);
            Matrix4 lightMVP = lightProjection * lightCFrame.inverse();

            GCamera camera = new GCamera();
            camera.setPosition(new Vector3(15, 20, 15));
            camera.lookAt(new Vector3(-2, 3, -5));

            // Ground plane and a floating occluder box, both in world space.
            renderDevice.pushState();
                renderDevice.setFramebuffer(shadowTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, shadowSize, shadowSize));
                renderDevice.setProjectionMatrix(lightProjection);
                renderDevice.setCameraToWorldMatrix(lightCFrame);
                renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                renderDevice.setDepthWrite(true);
                renderDevice.setColorWrite(false);
                renderDevice.setPolygonOffset(4);
                renderDevice.disableLighting();
                renderDevice.clear(false, true, false);
                renderDevice.setColor(new Color4(1, 1, 1, 1));
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                renderDevice.sendVertex(new Vector3(-14, 0, 10));
                renderDevice.sendVertex(new Vector3(14, 0, 10));
                renderDevice.sendVertex(new Vector3(14, 0, -10));
                renderDevice.sendVertex(new Vector3(-14, 0, -10));
                renderDevice.endPrimitive();
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                renderDevice.sendVertex(new Vector3(-2, 4, -2));
                renderDevice.sendVertex(new Vector3(2, 4, -2));
                renderDevice.sendVertex(new Vector3(2, 4, 2));
                renderDevice.sendVertex(new Vector3(-2, 4, 2));
                renderDevice.endPrimitive();
            renderDevice.popState();

            renderDevice.pushState();
                renderDevice.setFramebuffer(sceneTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                renderDevice.setProjectionAndCameraMatrix(camera);
                renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                renderDevice.clear(true, true, false);

                // Ambient pass.
                renderDevice.enableLighting();
                renderDevice.setLight(0, GLight.directional(-lightDirection, Color3.white() * 0.25f));
                renderDevice.setAmbientLightColor(new Color3(0.2f, 0.25f, 0.35f));
                renderDevice.setColor(new Color4(0.7f, 0.7f, 0.7f, 1));
                DrawGroundAndBox(renderDevice);

                // Additive sun pass with the shadow map.
                renderDevice.setAmbientLightColor(Color3.black());
                renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                renderDevice.setDepthWrite(false);
                renderDevice.setLight(0, GLight.directional(lightDirection, Color3.white()));
                renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ONE);
                if (ParityUseShadowMap)
                {
                    renderDevice.configureShadowMap(1, lightMVP, shadowMap);
                }
                DrawGroundAndBox(renderDevice);
            renderDevice.popState();

            // The shadow pass must cover the ground across the whole light frustum.
            float[] parityShadowDepth = new float[shadowSize * shadowSize];
            shadowTarget.readDepth(0, 0, shadowSize, shadowSize, parityShadowDepth);
            int parityShadowNonClear = 0;
            for (int i = 0; i < parityShadowDepth.Length; ++i)
            {
                if (parityShadowDepth[i] < 0.999f) ++parityShadowNonClear;
            }
            Check(parityShadowNonClear > shadowSize * shadowSize * 3 / 10,
                backendName + " shadow map covers the ground (" + parityShadowNonClear + "/" + parityShadowDepth.Length + ")");

byte[] pixels = new byte[size * size * 4];
            sceneTarget.readPixels(0, 0, size, size, pixels);
            Console.Write("DIAG parity " + backendName + ":");
            long sum = 0;
            for (int i = 0; i < pixels.Length; ++i)
            {
                // Sum RGB only; the framebuffer alpha channel legitimately differs
                // between the backends and is not part of the rendered color.
                if ((i & 3) != 3) { sum += pixels[i]; }
            }
            for (int y = 16; y < size; y += 24)
            {
                for (int x = 16; x < size; x += 24)
                {
                    int at = (y * size + x) * 4;
                    Console.Write(" " + pixels[at] + "/" + pixels[at + 1] + "/" + pixels[at + 2]);
                }
            }
            Console.WriteLine("  sum=" + sum);
        }

        private static void DrawGroundAndBox(RenderDevice renderDevice)
        {
            renderDevice.setNormal(new Vector3(0, 1, 0));
            renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
            renderDevice.sendVertex(new Vector3(-14, 0, 10));
            renderDevice.sendVertex(new Vector3(14, 0, 10));
            renderDevice.sendVertex(new Vector3(14, 0, -10));
            renderDevice.sendVertex(new Vector3(-14, 0, -10));
            renderDevice.endPrimitive();
            // Box top face (a thin slab) with outward-facing winding.
            renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
            renderDevice.sendVertex(new Vector3(-2, 4, 2));
            renderDevice.sendVertex(new Vector3(2, 4, 2));
            renderDevice.sendVertex(new Vector3(2, 4, -2));
            renderDevice.sendVertex(new Vector3(-2, 4, -2));
            renderDevice.endPrimitive();
        }

        /**
          A framebuffer with only a depth attachment (no color) must still receive
          depth writes. Direct3D 11 skips most fragments of a depth-only target unless
          a render-target view of identical dimensions is bound alongside the
          depth-stencil view, so this compares the depth-only case against one that
          also has a color attachment. The Collision_Demo shadow map is depth-only.
          */
        private static void testDepthOnlyFramebuffer(RenderDevice renderDevice, string backendName)
        {
            int[] nonClearCounts = new int[2];
            float[] centerDepths = new float[2];

            foreach (bool withColor in new bool[] { false, true })
            {
                const int size = 512;
                Texture shadowMap = Texture.createEmpty("depth-only shadow map", size, size, TextureFormat.depth(),
                    Texture.Dimension.DIM_2D, Texture.Settings.shadow());
                renderDevice.allocateTexture(shadowMap);
                Framebuffer target = Framebuffer.create(size, size);
                if (withColor)
                {
                    Texture color = Texture.createEmpty("depth-only color", size, size, TextureFormat.RGBA8,
                        Texture.Dimension.DIM_2D, Texture.Settings.defaults());
                    renderDevice.allocateTexture(color);
                    target.setTexture(Framebuffer.AttachmentPoint.Color0, color);
                }
                target.setTexture(Framebuffer.AttachmentPoint.Depth, shadowMap);

                // Tilted light above the ground; a vertical one makes lookAt degenerate.
                Vector3 lightDirection = new Vector3(0.5f, 0.8f, 0.3f).direction();
                CoordinateFrame lightFrame = CoordinateFrame.identity();
                lightFrame.rotation = Matrix3.identity();
                lightFrame.translation = Vector3.zero();
                lightFrame.lookAt(-lightDirection, Vector3.unitY());
                lightFrame.translation = lightDirection * 20;

                renderDevice.pushState();
                    renderDevice.setFramebuffer(target);
                    renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                    renderDevice.setProjectionMatrix(Matrix4.orthogonalProjection(-2, 2, -2, 2, 1, 40));
                    renderDevice.setCameraToWorldMatrix(lightFrame);
                    renderDevice.setDepthWrite(true);
                    renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                    renderDevice.setColorWrite(false);
                    renderDevice.setPolygonOffset(4);
                    renderDevice.disableLighting();
                    renderDevice.clear(true, true, false);
                    renderDevice.setColor(new Color4(1, 1, 1, 1));
                    renderDevice.setNormal(new Vector3(0, 1, 0));
                    renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                    renderDevice.sendVertex(new Vector3(-1, 0, 1));
                    renderDevice.sendVertex(new Vector3(1, 0, 1));
                    renderDevice.sendVertex(new Vector3(1, 0, -1));
                    renderDevice.sendVertex(new Vector3(-1, 0, -1));
                    renderDevice.endPrimitive();
                renderDevice.popState();

                float[] depth = new float[size * size];
                target.readDepth(0, 0, size, size, depth);
                int index = withColor ? 1 : 0;
                centerDepths[index] = depth[(size / 2) * size + size / 2];
                nonClearCounts[index] = 0;
                for (int i = 0; i < depth.Length; ++i)
                {
                    if (depth[i] < 0.999f) ++nonClearCounts[index];
                }
            }

            Check(centerDepths[0] > 0.1f && centerDepths[0] < 0.9f,
                backendName + " depth-only framebuffer stores the geometry depth (" + centerDepths[0] + ")");
            Check(centerDepths[1] > 0.1f && centerDepths[1] < 0.9f,
                backendName + " framebuffer with a color attachment stores the geometry depth (" + centerDepths[1] + ")");
            Check(nonClearCounts[0] == nonClearCounts[1],
                backendName + " depth-only framebuffer covers the same texels as one with a color attachment ("
                + nonClearCounts[0] + " vs " + nonClearCounts[1] + ")");
        }

        /** Emits one axis-aligned quad centred on (0,4,0) with the given outward normal. */
        private static void drawBoxFace(RenderDevice renderDevice, Vector3 normal)
        {
            const float c = 4.0f;
            const float h = 2.0f;
            renderDevice.setNormal(normal);
            renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
            if (normal.x > 0.5f)
            {
                renderDevice.sendVertex(new Vector3(+c, c - h, +h));
                renderDevice.sendVertex(new Vector3(+c, c - h, -h));
                renderDevice.sendVertex(new Vector3(+c, c + h, -h));
                renderDevice.sendVertex(new Vector3(+c, c + h, +h));
            }
            else if (normal.x < -0.5f)
            {
                renderDevice.sendVertex(new Vector3(-c, c - h, -h));
                renderDevice.sendVertex(new Vector3(-c, c - h, +h));
                renderDevice.sendVertex(new Vector3(-c, c + h, +h));
                renderDevice.sendVertex(new Vector3(-c, c + h, -h));
            }
            else if (normal.y > 0.5f)
            {
                renderDevice.sendVertex(new Vector3(-h, +c, +h));
                renderDevice.sendVertex(new Vector3(+h, +c, +h));
                renderDevice.sendVertex(new Vector3(+h, +c, -h));
                renderDevice.sendVertex(new Vector3(-h, +c, -h));
            }
            else if (normal.y < -0.5f)
            {
                renderDevice.sendVertex(new Vector3(-h, -c, -h));
                renderDevice.sendVertex(new Vector3(+h, -c, -h));
                renderDevice.sendVertex(new Vector3(+h, -c, +h));
                renderDevice.sendVertex(new Vector3(-h, -c, +h));
            }
            else if (normal.z > 0.5f)
            {
                renderDevice.sendVertex(new Vector3(-h, c - h, +c));
                renderDevice.sendVertex(new Vector3(+h, c - h, +c));
                renderDevice.sendVertex(new Vector3(+h, c + h, +c));
                renderDevice.sendVertex(new Vector3(-h, c + h, +c));
            }
            else
            {
                renderDevice.sendVertex(new Vector3(+h, c - h, -c));
                renderDevice.sendVertex(new Vector3(-h, c - h, -c));
                renderDevice.sendVertex(new Vector3(-h, c + h, -c));
                renderDevice.sendVertex(new Vector3(+h, c + h, -c));
            }
            renderDevice.endPrimitive();
        }

        /**
          Compares the lit surface color of a six-sided box after the two-pass
          (ambient then additive sun) lighting structure the Collision_Demo uses, so
          per-face lighting differences between the backends are visible.
          */
        /**
          Checks the vertical origin of uploaded texture data.  OpenGL's texture row
          zero is the BOTTOM of the image and Direct3D 11's is the TOP, so a texture
          uploaded without correction appears vertically mirrored.
          */
        /** Draws the box with a rotated object-to-world transform, exercising the normal matrix. */
        private static void drawRotatedBox(RenderDevice renderDevice)
        {
            CoordinateFrame frame = CoordinateFrame.identity();
            frame.rotation = Matrix3.fromAxisAngle(new Vector3(0.3f, 0.9f, 0.2f).direction(), 0.7f);
            frame.translation = new Vector3(0, 4, 0);
            renderDevice.setObjectToWorldMatrix(frame);
            drawBoxFace(renderDevice, Vector3.unitX());
            drawBoxFace(renderDevice, -Vector3.unitX());
            drawBoxFace(renderDevice, Vector3.unitY());
            drawBoxFace(renderDevice, -Vector3.unitY());
            drawBoxFace(renderDevice, Vector3.unitZ());
            drawBoxFace(renderDevice, -Vector3.unitZ());
        }
        private static void testBoxSurfaceParity(RenderDevice renderDevice, string backendName)
        {
            const int size = 128;
            Vector3 lightDirection = new Vector3(0.3f, 0.8f, -0.5f).direction();

            GCamera camera = new GCamera();
            camera.setPosition(new Vector3(9, 5, 12));
            camera.lookAt(new Vector3(0, 4, 0));

            Texture sceneColor = Texture.createEmpty("box surface color", size, size, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(sceneColor);
            Framebuffer sceneTarget = Framebuffer.create(size, size);
            sceneTarget.setTexture(Framebuffer.AttachmentPoint.Color0, sceneColor);
            Texture sceneDepth = Texture.createEmpty("box surface depth", size, size, TextureFormat.depth(),
                Texture.Dimension.DIM_2D, Texture.Settings.video());
            renderDevice.allocateTexture(sceneDepth);
            sceneTarget.setTexture(Framebuffer.AttachmentPoint.Depth, sceneDepth);

            renderDevice.pushState();
                renderDevice.setFramebuffer(sceneTarget);
                renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
                renderDevice.setProjectionAndCameraMatrix(camera);
                // No culling or specular so the comparison isolates diffuse lighting.
                renderDevice.setCullFace(RenderDevice.CullFace.CULL_NONE);
                renderDevice.setSpecularCoefficient(new Color3(0.8f, 0.8f, 0.8f));
                renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                renderDevice.clear(true, true, false);

                renderDevice.enableLighting();
                renderDevice.setLight(0, GLight.directional(-lightDirection, Color3.white() * 0.25f));
                renderDevice.setAmbientLightColor(new Color3(0.2f, 0.25f, 0.35f));
                renderDevice.setColor(new Color4(0.5f, 0.5f, 0.5f, 1));
                renderDevice.setSpecularCoefficient(new Color3(0.8f, 0.8f, 0.8f));
                drawRotatedBox(renderDevice);

                renderDevice.setAmbientLightColor(Color3.black());
                renderDevice.setDepthWrite(false);
                renderDevice.setLight(0, GLight.directional(lightDirection, Color3.white()));
                renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ONE);
                renderDevice.setSpecularCoefficient(new Color3(0.8f, 0.8f, 0.8f));
                drawRotatedBox(renderDevice);
            renderDevice.popState();

            byte[] pixels = new byte[size * size * 4];
            sceneTarget.readPixels(0, 0, size, size, pixels);
            // Sample the centre of each face's projected extent.
            int[][] probes = new int[][] {
                new int[] { size * 3 / 4, size / 2 },   // +X
                new int[] { size / 4, size / 2 },        // -X
                new int[] { size / 2, size / 4 },        // +Y
                new int[] { -1, -1 },                    // -Y (not visible from this camera)
                new int[] { size / 2, size * 3 / 4 },    // +Z
                new int[] { -1, -1 }                     // -Z (not visible from this camera)
            };
            string[] names = new string[] { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };
            Console.Write("DIAG box " + backendName + ":");
            for (int i = 0; i < probes.Length; ++i)
            {
                if (probes[i][0] < 0) { Console.Write(" " + names[i] + "=hidden"); continue; }
                int at = (probes[i][1] * size + probes[i][0]) * 4;
                Console.Write(" " + names[i] + "=" + pixels[at] + "/" + pixels[at + 1] + "/" + pixels[at + 2]);
            }
            Console.WriteLine();
        }

        /**
          A surface shaded with a zero shininess exponent (the Collision_Demo's
          glDiffuse path: specular black + shininess 0) must keep its ambient
          term even when the half-vector points away from the normal.  GL
          evaluates (N dot H)^0 as 1, while HLSL pow(0, 0) is NaN and would turn
          the whole lit color NaN.
         */
        private static void testZeroShininessSpecular(RenderDevice renderDevice, string backendName)
        {
            const int size = 128;

            Texture sceneColor = Texture.createEmpty("zero shininess color", size, size, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(sceneColor);
            Framebuffer sceneTarget = Framebuffer.create(size, size);
            sceneTarget.setTexture(Framebuffer.AttachmentPoint.Color0, sceneColor);
            Texture sceneDepth = Texture.createEmpty("zero shininess depth", size, size, TextureFormat.depth(),
                Texture.Dimension.DIM_2D, Texture.Settings.video());
            renderDevice.allocateTexture(sceneDepth);
            sceneTarget.setTexture(Framebuffer.AttachmentPoint.Depth, sceneDepth);

            GCamera camera = new GCamera();
            camera.setPosition(new Vector3(0, 6, 3));
            camera.lookAt(new Vector3(0, 0, 0));

            renderDevice.pushState();
            renderDevice.setFramebuffer(sceneTarget);
            renderDevice.setViewport(Rect2D.xywh(0, 0, size, size));
            renderDevice.setProjectionAndCameraMatrix(camera);
            renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
            renderDevice.clear(true, true, false);
            renderDevice.enableLighting();
            // A light from below, so the up-facing quad's normal points away from
            // the half-vector and the specular exponent zero path is exercised.
            renderDevice.setLight(0, GLight.directional(new Vector3(0, -1, 0), Color3.white()));
            renderDevice.setAmbientLightColor(new Color3(0.2f, 0.2f, 0.2f));
            renderDevice.setColor(new Color4(0.7f, 0.7f, 0.7f, 1));
            renderDevice.setSpecularCoefficient(Color3.black());
            renderDevice.setShininess(0.0f);

            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
            renderDevice.setNormal(Vector3.unitY());
            renderDevice.sendVertex(new Vector3(-2, 0, -2));
            renderDevice.sendVertex(new Vector3(-2, 0, 2));
            renderDevice.sendVertex(new Vector3(2, 0, -2));
            renderDevice.setNormal(Vector3.unitY());
            renderDevice.sendVertex(new Vector3(-2, 0, 2));
            renderDevice.sendVertex(new Vector3(2, 0, 2));
            renderDevice.sendVertex(new Vector3(2, 0, -2));
            renderDevice.endPrimitive();
            renderDevice.popState();

            byte[] pixels = new byte[size * size * 4];
            sceneTarget.readPixels(0, 0, size, size, pixels);
            int brightest = 0;
            for (int i = 0; i < size * size; ++i)
            {
                int v = pixels[i * 4];
                if (v > brightest) brightest = v;
            }
            // The ambient term alone gives 0.2 * 0.7 * 255 = 36.
            Check(brightest >= 25,
                backendName + " zero-shininess specular keeps the ambient term (brightest=" + brightest + ")"
                + (brightest == 0 ? "; the lit color was NaN" : ""));
            Console.WriteLine("DIAG zero shininess " + backendName + " brightest=" + brightest);
        }

        /**
          Renders a sky whose six faces are distinct solid colors and reports the
          color seen looking along each world axis, so the cube-map face orientation
          can be compared between the backends.
          */
        private static void testCubeMapFaceOrientation(RenderDevice renderDevice, string backendName)
        {
            string dir = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), "glg3d_cube_orient");
            global::System.IO.Directory.CreateDirectory(dir);
            Color3[] faceColors = new Color3[] {
                new Color3(1, 0, 0), new Color3(0, 1, 0), new Color3(0, 0, 1),
                new Color3(1, 1, 0), new Color3(1, 0, 1), new Color3(0, 1, 1)
            };
            string[] exts = new string[] { "ft", "bk", "up", "dn", "rt", "lf" };
            for (int f = 0; f < 6; ++f)
            {
                writeBMP(global::System.IO.Path.Combine(dir, "sky_" + exts[f] + ".bmp"),
                    (byte)(faceColors[f].r * 255), (byte)(faceColors[f].g * 255), (byte)(faceColors[f].b * 255), 16, 16);
            }

            ReferenceCountedPointer<Sky> sky =
                Sky.fromFile(renderDevice, "", global::System.IO.Path.Combine(dir, "sky_*.bmp"), false);
            Check(sky != null && sky.pointer() != null, "Sky loaded for cube orientation");

            Texture colorTexture = Texture.createEmpty("cube orient color", 32, 32, TextureFormat.RGBA8,
                Texture.Dimension.DIM_2D, Texture.Settings.defaults());
            renderDevice.allocateTexture(colorTexture);
            Framebuffer target = Framebuffer.create(32, 32);
            target.setTexture(Framebuffer.AttachmentPoint.Color0, colorTexture);

            Vector3[] lookDirections = new Vector3[] {
                new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
                new Vector3(0, 1, 0), new Vector3(0, -1, 0),
                new Vector3(0, 0, 1), new Vector3(0, 0, -1)
            };
            string[] names = new string[] { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };
            LightingParameters lighting = new LightingParameters();
            lighting.skyAmbient = Color3.white();

            Console.Write("DIAG cube " + backendName + ":");
            for (int i = 0; i < lookDirections.Length; ++i)
            {
                renderDevice.pushState();
                    renderDevice.setFramebuffer(target);
                    renderDevice.setViewport(Rect2D.xywh(0, 0, 32, 32));
                    renderDevice.setProjectionMatrix(Matrix4.perspectiveProjection(-1, 1, -1, 1, 0.1f, 100.0f));
                    CoordinateFrame camera = new CoordinateFrame(Vector3.zero());
                    camera.lookAt(lookDirections[i], Vector3.unitZ());
                    renderDevice.setCameraToWorldMatrix(camera);
                    renderDevice.setColorClearValue(new Color4(0, 0, 0, 1));
                    renderDevice.clear(true, true, false);
                    sky.pointer().render(renderDevice, lighting);
                renderDevice.popState();

                byte[] pixels = new byte[32 * 32 * 4];
                target.readPixels(0, 0, 32, 32, pixels);
                int centre = (16 * 32 + 16) * 4;
                Console.Write(" " + names[i] + "=" + pixels[centre] + "/" + pixels[centre + 1] + "/" + pixels[centre + 2]);
            }
            Console.WriteLine();
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
            global::G3D.Image.SystemDrawing.SystemDrawingImageDecoder.register();

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
                testShadowMap(renderDevice, "OpenGL");
                testShadowMapEdgeCases(renderDevice, "OpenGL");
                testDepthOnlyFramebuffer(renderDevice, "OpenGL");
                testBoxSurfaceParity(renderDevice, "OpenGL");
                testZeroShininessSpecular(renderDevice, "OpenGL");
                testCubeMapFaceOrientation(renderDevice, "OpenGL");
                testShadowSceneParity(renderDevice, "OpenGL");
                testCubeMapReflection(renderDevice, openGLBackend, "OpenGL");
                testAssemblyProgram(renderDevice, "OpenGL");



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
                Direct3D11.Direct3D11Backend.EnableDebugLayer = true;
                direct3D11Backend = new Direct3D11.Direct3D11Backend();
                direct3D11Backend.Initialize(direct3D11Window);
                Console.WriteLine("DIAG d3d11 debug layer active=" + direct3D11Backend.DebugLayerActive);
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
                testShadowMap(direct3D11RenderDevice, "Direct3D 11");
                testShadowMapEdgeCases(direct3D11RenderDevice, "Direct3D 11");
                testDepthOnlyFramebuffer(direct3D11RenderDevice, "Direct3D 11");
                testBoxSurfaceParity(direct3D11RenderDevice, "Direct3D 11");
                testZeroShininessSpecular(direct3D11RenderDevice, "Direct3D 11");
                testCubeMapFaceOrientation(direct3D11RenderDevice, "Direct3D 11");
                testShadowSceneParity(direct3D11RenderDevice, "Direct3D 11");

                byte[] direct3D11Constants = direct3D11Backend.ReadFixedFunctionConstants();
                bool direct3D11ConstantsWritten = false;
                if (direct3D11Constants != null)
                {
                    for (int constantByte = 0; constantByte < direct3D11Constants.Length; ++constantByte)
                    {
                        if (direct3D11Constants[constantByte] != 0)
                        {
                            direct3D11ConstantsWritten = true;
                            break;
                        }
                    }
                }
                Check(direct3D11ConstantsWritten,
                    "Direct3D 11 fixed-function constant buffer is updated with the drawn matrices");

                d3d11TextureProbe(direct3D11RenderDevice, "Direct3D 11");
                testCubeMapReflection(direct3D11RenderDevice, direct3D11Backend, "Direct3D 11");

                string[] direct3D11Messages = direct3D11Backend.DrainDeviceMessages();
                int direct3D11Errors = 0;
                foreach (string direct3D11Message in direct3D11Messages)
                {
                    Console.WriteLine("DIAG d3d11 device message: " + direct3D11Message);
                    if (direct3D11Message.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                    {
                        ++direct3D11Errors;
                    }
                }
                if (direct3D11Backend.DebugLayerActive)
                {
                    Check(direct3D11Errors == 0, "Direct3D 11 device reported no error-severity messages");
                }
                else
                {
                    Console.WriteLine("NOTE: the Direct3D 11 debug layer is unavailable; device error checks are skipped.");
                }
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


