/**
 @file Test/main.cpp

 This file runs unit conformance and performance tests for G3D.  
 To write a new test, add a file named t<class>.cpp to the project
 and provide two entry points: test<class> and perf<class> (even if they are
 empty).

 Call those two methods from main() in main.cpp.

 @maintainer Morgan McGuire, matrix@graphics3d.com
 @created 2002-01-01
 @edited  2006-04-10
 */

#define RUN_SLOW_TESTS

using System;
using global::System.Collections.Generic;
using global::System.Runtime.InteropServices;
using global::System.Threading;
using G3D;
using G3D.OpenGL;

namespace G3D.Tests
{
    /**
      Emulates the C runtime text-mode translation of "\n" to "\r\n" that the
      original printf output received on Windows, so stdout bytes match the
      C++ test executable.
     */
    internal sealed class CrLfTextWriter : global::System.IO.TextWriter
    {
        private readonly global::System.IO.TextWriter inner;

        public CrLfTextWriter(global::System.IO.TextWriter inner)
        {
            this.inner = inner;
        }

        public override global::System.Text.Encoding Encoding
        {
            get { return inner.Encoding; }
        }

        public override void Write(char value)
        {
            inner.Write(value == '\n' ? "\r\n" : value.ToString());
        }

        public override void Write(string value)
        {
            if (value != null)
            {
                inner.Write(value.Replace("\n", "\r\n"));
            }
        }

        public override void WriteLine()
        {
            inner.Write("\r\n");
        }

        public override void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }
    }

    /**
      Transitional helper. Every not-yet-ported test reports itself explicitly
      on stderr; removed together with its last call site when the stage that
      implements the test completes (see docs/PORT_STATUS.md).
     */
    internal static class SkipTest
    {
        public static void notImplemented(string name)
        {
            global::System.Console.Error.WriteLine("SKIPPED: " + name + " (not yet ported; see docs/PORT_STATUS.md)");
        }

        /**
          Reports that a test cannot run because its dependency has not been
          ported yet. The reason is always printed so that the skip is an
          explicit, documented capability limitation rather than a silent stub.
          The notice goes to stderr so that stdout keeps the exact structure of
          the original C++ test executable.
         */
        public static void skipped(string reason)
        {
            global::System.Console.Error.WriteLine("SKIPPED: " + reason);
        }
    }

    internal static partial class Program
    {
        private static int failures;
        private static int failuresAtTestStart;

        private static void printf(string fmt, params object[] args)
        {
            Console.Out.Write(Format.vformat(fmt, args));
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                failures++;
                Console.WriteLine("FAIL: " + name);
            }
        }

        private static void CheckEqual(int expected, int actual, string name)
        {
            Check(expected == actual, name + " (expected " + expected + ", actual " + actual + ")");
        }

        private static void CheckEqual(double expected, double actual, string name)
        {
            Check(g3dmath.fuzzyEq(expected, actual), name + " (expected " + expected + ", actual " + actual + ")");
        }

        private static void CheckEqual(string expected, string actual, string name)
        {
            Check(expected == actual, name + " (expected \"" + expected + "\", actual \"" + actual + "\")");
        }

        private static void CheckQueue(Queue<int> queue, params int[] expected)
        {
            Check(queue.size() == expected.Length, "Queue size");
            int count = queue.size() < expected.Length ? queue.size() : expected.Length;
            for (int i = 0; i < count; ++i)
            {
                CheckEqual(expected[i], queue[i], "Queue[" + i + "]");
            }
        }

        private static void CheckArray(Array<int> array, params int[] expected)
        {
            Check(array.size() == expected.Length, "Array size (expected " + expected.Length + ", actual " + array.size() + ")");
            string actualText = "";
            for (int i = 0; i < array.size(); ++i)
            {
                actualText += (i == 0 ? "" : ",") + array[i];
            }
            string expectedText = "";
            for (int i = 0; i < expected.Length; ++i)
            {
                expectedText += (i == 0 ? "" : ",") + expected[i];
            }
            Check(actualText == expectedText, "Array contents (expected [" + expectedText + "], actual [" + actualText + "])");
            int count = array.size() < expected.Length ? array.size() : expected.Length;
            for (int i = 0; i < count; ++i)
            {
                CheckEqual(expected[i], array[i], "Array[" + i + "]");
            }
        }

        private static void StartTest(string label)
        {
            failuresAtTestStart = failures;
            Console.Write(label);
        }

        private static void EndTest(string result)
        {
            Console.WriteLine(failures == failuresAtTestStart ? result : "FAILED");
        }

        /**
          Reports that a test cannot run because its dependency has not been
          ported yet. The reason is always printed so that the skip is an
          explicit, documented capability limitation rather than a silent stub.
         */
        private sealed class DisposableItem : IDisposable
        {
            public int DisposeCount;

            public void Dispose()
            {
                DisposeCount++;
            }
        }

        private sealed class BadHashKey
        {
            public int value;

            public override int GetHashCode()
            {
                return 0;
            }

            public override bool Equals(object obj)
            {
                return object.ReferenceEquals(this, obj);
            }
        }

        /**
          Called by alwaysAssertM in case of failure in release mode.  If returns
          true then the program exits with -1 (you can replace this with your own
          version that throws an exception or has other failure modes).
         */
        private static bool ThrowAssertionFailure(string expression, string message, string filename, int lineNumber, ref bool ignoreAlways, bool useGuiPrompt)
        {
            throw new InvalidOperationException(message);
        }

        private static bool TableRemoveMissingThrows<TKey, TValue>(Table<TKey, TValue> table, TKey key)
        {
            G3D.AssertionHook originalAssertionHook = Debug.assertionHook();
            G3D.AssertionHook originalFailureHook = Debug.failureHook();
            try
            {
                Debug.setAssertionHook(ThrowAssertionFailure);
                Debug.setFailureHook(ThrowAssertionFailure);
                table.remove(key);
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
            finally
            {
                Debug.setAssertionHook(originalAssertionHook);
                Debug.setFailureHook(originalFailureHook);
            }
        }

        private static bool TableGetMissingThrows<TKey, TValue>(Table<TKey, TValue> table, TKey key)
        {
            G3D.AssertionHook originalAssertionHook = Debug.assertionHook();
            G3D.AssertionHook originalFailureHook = Debug.failureHook();
            try
            {
                Debug.setAssertionHook(ThrowAssertionFailure);
                Debug.setFailureHook(ThrowAssertionFailure);
                TValue ignored = table[key];
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
            finally
            {
                Debug.setAssertionHook(originalAssertionHook);
                Debug.setFailureHook(originalFailureHook);
            }
        }

        /////////////////////////////////
        // Test/main.cpp inline tests
        /////////////////////////////////

        private static void testConvexPolygon2D()
        {
            Array<Vector2> v = new Array<Vector2>();
            v.append(new Vector2(0, 0), new Vector2(1,1), new Vector2(2, 0));
            ConvexPolygon2D C = new ConvexPolygon2D(v);
            Debug.debugAssert(! C.contains(new Vector2(10, 2)));
            Debug.debugAssert(C.contains(new Vector2(1, 0.5f)));
        }

        private static void testWildcards()
        {
            printf("filenameContainsWildcards\n");
            Debug.debugAssert(!FileUtils.filenameContainsWildcards("file1.exe"));
            Debug.debugAssert(FileUtils.filenameContainsWildcards("file?.exe"));
            Debug.debugAssert(FileUtils.filenameContainsWildcards("f*.exe"));
            Debug.debugAssert(FileUtils.filenameContainsWildcards("f*.e?e"));
            Debug.debugAssert(FileUtils.filenameContainsWildcards("*1.exe"));
            Debug.debugAssert(FileUtils.filenameContainsWildcards("?ile1.exe"));
        }

        private static void testBox()
        {
            printf("Box\n");
            Box box = new Box(new Vector3(0, 0, 0), new Vector3(1, 1, 1));

            Debug.debugAssert(box.contains(new Vector3(0, 0, 0)));
            Debug.debugAssert(box.contains(new Vector3(1, 1, 1)));
            Debug.debugAssert(box.contains(new Vector3(0.5f, 0.5f, 0.5f)));
            Debug.debugAssert(!box.contains(new Vector3(1.5f, 0.5f, 0.5f)));
            Debug.debugAssert(!box.contains(new Vector3(0.5f, 1.5f, 0.5f)));
            Debug.debugAssert(!box.contains(new Vector3(0.5f, 0.5f, 1.5f)));
            Debug.debugAssert(!box.contains(-new Vector3(0.5f, 0.5f, 0.5f)));
            Debug.debugAssert(!box.contains(-new Vector3(1.5f, 0.5f, 0.5f)));
            Debug.debugAssert(!box.contains(-new Vector3(0.5f, 1.5f, 0.5f)));
            Debug.debugAssert(!box.contains(-new Vector3(0.5f, 0.5f, 1.5f)));

            Vector3 v0, v1, v2, v3, n1, n2;

            v0 = box.getCorner(0);
            v1 = box.getCorner(1);
            v2 = box.getCorner(2);
            v3 = box.getCorner(3);

            Debug.debugAssert(v0 == new Vector3(0, 0, 1));
            Debug.debugAssert(v1 == new Vector3(1, 0, 1));
            Debug.debugAssert(v2 == new Vector3(1, 1, 1));
            Debug.debugAssert(v3 == new Vector3(0, 1, 1));

            Vector3[] n = new Vector3[2] { new Vector3(0, 0, 1), new Vector3(1, 0, 0) };

            int i;
            for (i = 0; i < 2; ++i)
            {
                box.getFaceCorners(i, out v0, out v1, out v2, out v3);
                n1 = (v1 - v0).cross(v3 - v0);
                n2 = (v2 - v1).cross(v0 - v1);

                Debug.debugAssert(n1 == n2);
                Debug.debugAssert(n1 == n[i]);
            }
        }

        private static void testAABoxCollision()
        {
            printf("intersectionTimeForMovingPointFixedAABox\n");

            Vector3 boxlocation, aaboxlocation, normal;

            for (int i = 0; i < 1000; ++i)
            {
                Vector3 pt1 = Vector3.random() * g3dmath.random(0, 10);
                Vector3 vel1 = Vector3.random();

                Vector3 low = Vector3.random() * 5;
                Vector3 extent = new Vector3((float)g3dmath.random(0, 4), (float)g3dmath.random(0, 4), (float)g3dmath.random(0, 4));
                AABox aabox = new AABox(low, low + extent);
                Box box = aabox.toBox();

                float boxTime = CollisionDetection.collisionTimeForMovingPointFixedBox(
                    pt1, vel1, box, out boxlocation, out normal);

                float aaTime = CollisionDetection.collisionTimeForMovingPointFixedAABox(
                    pt1, vel1, aabox, out aaboxlocation);

                Ray ray = Ray.fromOriginAndDirection(pt1, vel1);
                float rayboxTime = ray.intersectionTime(box);

                float rayaaTime = ray.intersectionTime(aabox);

                Debug.debugAssert(g3dmath.fuzzyEq(boxTime, aaTime));
                if (boxTime < (float)g3dmath.inf())
                {
                    Debug.debugAssert(boxlocation.fuzzyEq(aaboxlocation));
                }

                Debug.debugAssert(g3dmath.fuzzyEq(rayboxTime, rayaaTime));
            }
        }

        private static void testPlane()
        {
            printf("Plane\n");
            {
                Plane p = new Plane(new Vector3(1, 0, 0),
                                    new Vector3(0, 1, 0),
                                    new Vector3(0, 0, 0));

                Vector3 n = p.normal();
                Debug.debugAssert(n == new Vector3(0, 0, 1));
            }

            {
                Plane p = new Plane(new Vector3(4, 6, .1f),
                                    new Vector3(-.2f, 6, .1f),
                                    new Vector3(-.2f, 6, -.1f));

                Vector3 n = p.normal();
                Debug.debugAssert(n.fuzzyEq(new Vector3(0, -1, 0)));
            }

            {
                Plane p = new Plane(new Vector4(1, 0, 0, 0),
                                    new Vector4(0, 1, 0, 0),
                                    new Vector4(0, 0, 0, 1));
                Vector3 n = p.normal();
                Debug.debugAssert(n.fuzzyEq(new Vector3(0, 0, 1)));
            }

            {
                Plane p = new Plane(new Vector4(0, 0, 0, 1),
                                    new Vector4(1, 0, 0, 0),
                                    new Vector4(0, 1, 0, 0));
                Vector3 n = p.normal();
                Debug.debugAssert(n.fuzzyEq(new Vector3(0, 0, 1)));
            }

            {
                Plane p = new Plane(new Vector4(0, 1, 0, 0),
                                    new Vector4(0, 0, 0, 1),
                                    new Vector4(1, 0, 0, 0));
                Vector3 n = p.normal();
                Debug.debugAssert(n.fuzzyEq(new Vector3(0, 0, 1)));
            }
        }

        private static void testColor3uint8Array()
        {
            printf("Array<Color3uint8>\n");
            Array<Color3uint8> x = new Array<Color3uint8>(2);
            // holy moly, ai will prolly fix this later, TODO
            Debug.debugAssert(global::System.Runtime.InteropServices.Marshal.SizeOf(typeof(Color3uint8)) == 3);
            Color3uint8 color0 = x[0];
            color0.r = 60;
            color0.g = 61;
            color0.b = 62;
            x[0] = color0;

            Color3uint8 color1 = x[1];
            color1.r = 63;
            color1.g = 64;
            color1.b = 65;
            x[1] = color1;

            Color3uint8[] y = x.getCArray();
            Debug.debugAssert(y[0].r == 60);
            Debug.debugAssert(y[0].g == 61);
            Debug.debugAssert(y[0].b == 62);
            Debug.debugAssert(y[1].r == 63);
            Debug.debugAssert(y[1].g == 64);
            Debug.debugAssert(y[1].b == 65);
        }

        private static void testFloat()
        {
            printf("Test Float\n");
            /* changed from "nan" by ekern.  does this work on windows? */
            double x = g3dmath.nan();
            bool b1 = (x < 0.0);
            bool b2 = (x >= 0.0);
            bool b3 = !(b1 || b2);
            // The original C++ discards b1, b2, and b3 with (void) casts.
            if (b1 || b2 || b3) {
            }
            Debug.debugAssert(g3dmath.isNaN(g3dmath.nan()));
            Debug.debugAssert(!g3dmath.isNaN(4));
            Debug.debugAssert(!g3dmath.isNaN(0));
            Debug.debugAssert(!g3dmath.isNaN(g3dmath.inf()));
            Debug.debugAssert(!g3dmath.isNaN(-g3dmath.inf()));
            Debug.debugAssert(!g3dmath.isFinite(g3dmath.nan()));
            Debug.debugAssert(!g3dmath.isFinite(-g3dmath.inf()));
            Debug.debugAssert(!g3dmath.isFinite(g3dmath.inf()));
            Debug.debugAssert(g3dmath.isFinite(0));
        }

        private static void testglFormatOf()
        {
            printf("glFormatOf\n");
            Debug.debugAssert(GLFormat.glFormatOf<Color3>() == GLFormat.FLOAT);
            Debug.debugAssert(GLFormat.glFormatOf<Color3uint8>() == GLFormat.UNSIGNED_BYTE);
            Debug.debugAssert(GLFormat.glFormatOf<Vector3int16>() == GLFormat.SHORT);
            Debug.debugAssert(GLFormat.glFormatOf<float>() == GLFormat.FLOAT);
            Debug.debugAssert(GLFormat.glFormatOf<short>() == GLFormat.SHORT);
            Debug.debugAssert(GLFormat.glFormatOf<int>() == GLFormat.INT);
            Debug.debugAssert(GLFormat.sizeOfGLFormat(GLFormat.FLOAT) == 4);
            printf("passed\n");
        }

        private static void testSwizzle()
        {
            Vector4 v1 = new Vector4(1, 2, 3, 4);
            Vector2 v2 = v1.xy() + v1.yz();
        }

        private static void testCoordinateFrame()
        {
            printf("CoordinateFrame ");

            {
                // Easy case
                // Documented adaptation: C++ CoordinateFrame c; -> CoordinateFrame.identity()
                CoordinateFrame c = CoordinateFrame.identity();
                c.lookAt(new Vector3(-1, 0, -1));
                float h = c.getHeading();
                Debug.debugAssert(g3dmath.fuzzyEq(h, g3dmath.G3D_PI / 4));
            }

            // Test getHeading at a variety of angles
            for (int i = -175; i <= 175; i += 5)
            {
                CoordinateFrame c = CoordinateFrame.identity();
                float h = c.getHeading();
                Debug.debugAssert(h == 0);

                c.rotation = Matrix3.fromAxisAngle(Vector3.unitY(), (float)g3dmath.toRadians(i));

                h = c.getHeading();
                Debug.debugAssert(g3dmath.fuzzyEq(h, g3dmath.toRadians(i)));
            }

            printf("passed\n");
        }

        private static void measureMemsetPerformance()
        {
            printf("----------------------------------------------------------\n");

            ulong native = 0, g3d = 0;

            int n = 1024 * 1024;
            byte[] m1 = System.malloc(n);

            // First iteration just primes the system
            for (int i = 0; i < 2; ++i)
            {
                System.beginCycleCount(ref native);
                // Documented adaptation: the C++ benchmark compared the CRT
                // memset against G3D::System::memset. Safe C# cannot call the
                // CRT memset, so Array.Clear (the runtime's own memset) is
                // used as the native baseline.
                global::System.Array.Clear(m1, 0, n);
                System.endCycleCount(ref native);
                System.beginCycleCount(ref g3d);
                System.memset(m1, 31, n);
                System.endCycleCount(ref g3d);
            }
            System.free(m1);

            printf("System::memset:                     %d cycles/kb\n", (int)(g3d / (ulong)(n / 1024)));
            printf("::memset      :                     %d cycles/kb\n", (int)(native / (ulong)(n / 1024)));
        }

        private static void measureNormalizationPerformance()
        {
            printf("----------------------------------------------------------\n");
            ulong raw = 0, opt = 0, overhead = 0;
            int n = 1024 * 1024;

            double y;
            Vector3 x = new Vector3(10, -20, 3);

            int i, j;

            for (j = 0; j < 2; ++j)
            {
                x = new Vector3(10, -20, 3);
                y = 0;
                System.beginCycleCount(ref overhead);
                for (i = n - 1; i >= 0; --i)
                {
                    x.z = i;
                    y += x.z;
                }
                System.endCycleCount(ref overhead);
            }

            x = new Vector3(10, -20, 3);
            y = 0;
            System.beginCycleCount(ref raw);
            for (i = n - 1; i >= 0; --i)
            {
                x.z = i;
                y += x.direction().z;
                y += x.direction().z;
                y += x.direction().z;
            }
            System.endCycleCount(ref raw);

            x = new Vector3(10, -20, 3);
            y = 0;
            System.beginCycleCount(ref opt);
            for (i = n - 1; i >= 0; --i)
            {
                x.z = i;
                y += x.fastDirection().z;
                y += x.fastDirection().z;
                y += x.fastDirection().z;
            }
            System.endCycleCount(ref opt);

            double r = raw;
            double o = opt;
            double h = overhead;

            printf("%g %g %g\n", r - h, o - h, h);

            printf("Vector3::direction():               %d cycles\n", (int)((r - h) / (n * 3.0)));
            printf("Vector3::fastDirection():           %d cycles\n", (int)((o - h) / (n * 3.0)));
        }

        /**
          Touches the hot paths before the first timed benchmark so that JIT
          compilation is not charged to the measured run. The C++ benchmark is
          ahead-of-time compiled and has no equivalent cost.
         */
        private static void jitWarmup()
        {
            Array<int> a = new Array<int>();
            a.append(1);
            Table<int, int> t = new Table<int, int>();
            t.set(1, 1);
            Queue<int> q = new Queue<int>();
            q.pushBack(1);
            Matrix3 m = Matrix3.identity();
            m = m * m;
            TextOutput to = new TextOutput(new TextOutput.Options());
            to.printf("%d", 1);
            to.commitString();
            string s = Format.format("%d", 1);
            Vector3 v = new Vector3(1, 2, 3);
            v = v.direction();
            v = v.fastDirection();
            byte[] buf = System.malloc(64);
            System.memcpy(buf, buf, 64);
            System.memset(buf, 0, 64);
            System.free(buf);
            BinaryOutput bo = new BinaryOutput();
            bo.writeInt32(1);
            bo.commit(new byte[64]);
            global::System.Diagnostics.Stopwatch watch = new global::System.Diagnostics.Stopwatch();
            watch.Start();
            watch.Stop();
        }

        private static void testRenderDevice()
        {
            printf("RenderDevice ");
            
            // Create OpenGL window and backend
            OpenGLWindow window = new OpenGLWindow();
            OpenGLBackend backend = new OpenGLBackend();
            
            try
            {
                backend.Initialize(window);
                
                // Create and initialize RenderDevice
                RenderDevice renderDevice = new RenderDevice();
                renderDevice.setBackend(backend);
                
                // Wrap OpenGLWindow in GWindow for describeSystem
                GWindow gwindow = new GWindow(window);
                renderDevice.setWindow(gwindow);
                
                GWindow.Settings settings = new GWindow.Settings();
                settings.width = 800;
                settings.height = 600;
                settings.alphaBits = 0;
                settings.rgbBits = 8;
                settings.depthBits = 24;
                settings.stencilBits = 0;
                settings.fsaaSamples = 1;
                
                bool initResult = renderDevice.init(settings);
                Debug.debugAssert(initResult);
                Debug.debugAssert(renderDevice.initialized());
                
                // Test describeSystem
                string s = "";
                renderDevice.describeSystem(ref s);
                printf("%s\n", s);
                
                // Test push/pop performance (simplified)
                measureRDPushPopPerformance(renderDevice);
                
                renderDevice.cleanup();
            }
            finally
            {
                window.Dispose();
            }
        }

        private static void measureRDPushPopPerformance(RenderDevice rd)
        {
            ulong identityCycles = 0;
            int N = 500;
            
            rd.pushState();
            rd.popState();
            
            System.beginCycleCount(ref identityCycles);
            for (int i = 0; i < N; ++i)
            {
                rd.pushState();
                rd.popState();
            }
            System.endCycleCount(ref identityCycles);
            
            printf("RenderDevice::push+pop:             %g cycles\n", identityCycles / (double)N);
        }

        public static int Main()
        {
            // The original C++ test executable writes to a text-mode stdout,
            // so the C runtime translated every "\n" into "\r\n". Installing the
            // writer reproduces those bytes exactly.
            Console.SetOut(new CrLfTextWriter(Console.Out));

            NetworkDevice networkDevice = new NetworkDevice();
            networkDevice.init();

            string s = "";
            System.describeSystem(ref s);
            printf("%s\n", s);

            networkDevice.describeSystem(ref s);
            printf("%s\n", s);

#if !DEBUG
            printf("Performance analysis:\n\n");

            testGImage();

            jitWarmup();

            perfArray();

            perfTable();

            printf("%s\n", System.mallocPerformance());

            perfQueue();

            perfMatrix3();

            perfTextOutput();

            perfSystemMemcpy();

            perfBinaryIO();

            perfAABSPTree();

            perfCollisionDetection();

            measureMemsetPerformance();
            measureNormalizationPerformance();

            // RenderDevice tests (ported from main.cpp)
            testRenderDevice();
#else
            printf("\n\nTests:\n\n");

            testCollisionDetection();

            testCoordinateFrame();

            testReliableConduit(networkDevice);

            testDiscovery(networkDevice);

            testAABSPTree();

            testQuat();

            testReferenceCount();

            testAtomicInt32();

            testGThread();

            testSystemMemset();

            testSystemMemcpy();

            testQueue();

            testMatrix();

            testBigInt();

            testCoreValueMath();

            testCoreUtilities();

            testCrypto();

            testSpline();

            testMatrix3Advanced();

            testArray();

            testConvexPolyhedron();

            testMeshAlgWeld();

            testAdjacency();

            testMeshAlgTangentSpace();

            testMeshAlg();

            testMeshBuilder();

            testGChunk();

            testGImage();
            printf("  passed\n");

            printf("ConvexPolygon2D\n");
            testConvexPolygon2D();
            printf("  passed\n");

            testPlane();
            printf("  passed\n");

            testAABox();

            testRandom();
            printf("  passed\n");

            testAABoxCollision();
            printf("  passed\n");

            testWildcards();
            printf("  passed\n");

            testFloat();
            printf("  passed\n");

            testRandom();

            testTextInput();
            printf("  passed\n");

            testTable();

            testBox();
            printf("  passed\n");

            testColor3uint8Array();
            printf("  passed\n");

            testglFormatOf();
            printf("  passed\n");

            testSwizzle();

            testBinaryIO();

            testVAR();

#if RUN_SLOW_TESTS
            testHugeBinaryIO();
            printf("  passed\n");
#endif

            printf("%s\n", System.mallocPerformance());
            System.resetMallocPerformanceCounters();

            printf("\nAll tests succeeded.\n");
#endif

            networkDevice.cleanup();

            if (failures == 0)
            {
                return 0;
            }

            printf("\n%d test(s) failed.\n", failures);
            return 1;
        }
    }
}
