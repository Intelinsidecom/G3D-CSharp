using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testSpline()
        {
            printf("G3D::Spline  ");

            double[] controlX = new double[] { 0.0, 2.0, 4.0 };
            Color3[] controlY = new Color3[]
            {
                new Color3(0.0f, 0.0f, 0.0f),
                new Color3(1.0f, 0.0f, 0.0f),
                new Color3(0.0f, 0.0f, 1.0f)
            };

            Check(Spline.linearSpline(1.0, controlX, controlY, 3) == new Color3(0.5f, 0.0f, 0.0f),
                  "linearSpline interpolates between controls");
            Check(Spline.linearSpline(3.0, controlX, controlY, 3) == new Color3(0.5f, 0.0f, 0.5f),
                  "linearSpline interpolates in the final interval");
            Check(Spline.linearSpline(-1.0, controlX, controlY, 3) == controlY[0],
                  "linearSpline clamps before the first control");
            Check(Spline.linearSpline(5.0, controlX, controlY, 3) == controlY[2],
                  "linearSpline clamps after the last control");
            Check(Spline.linearSpline(100.0, controlX, controlY, 1) == controlY[0],
                  "linearSpline supports one control");
            Check(Spline.linearSpline(3.0, controlX, controlY, 2) == controlY[1],
                  "linearSpline honors numControl");

            float[] floatControlX = new float[] { 0.0f, 2.0f, 4.0f };
            int[] intControlX = new int[] { 0, 2, 4 };
            Check(Spline.linearSpline(1.0, floatControlX, controlY, 3) == new Color3(0.5f, 0.0f, 0.0f),
                  "linearSpline supports float control coordinates");
            Check(Spline.linearSpline(1.0, intControlX, controlY, 3) == new Color3(0.5f, 0.0f, 0.0f),
                  "linearSpline supports int control coordinates");

            Check(g3dmath.fuzzyEq(Spline.linearSpline(1.0, controlX, new double[] { 0.0, 2.0, 4.0 }, 3), 1.0),
                  "linearSpline supports double values");
            Check(g3dmath.fuzzyEq(Spline.linearSpline(1.0, controlX, new float[] { 0.0f, 2.0f, 4.0f }, 3), 1.0f),
                  "linearSpline supports float values");
            Check(Spline.linearSpline(1.0, controlX, new int[] { 0, 2, 4 }, 3) == 1,
                  "linearSpline supports int values");
            Check(Spline.linearSpline(1.0, controlX, new Vector2[] { Vector2.zero(), new Vector2(2.0f, 0.0f), new Vector2(4.0f, 0.0f) }, 3) ==
                  new Vector2(1.0f, 0.0f), "linearSpline supports Vector2 values");
            Check(Spline.linearSpline(1.0, controlX, new Vector3[] { Vector3.zero(), new Vector3(2.0f, 0.0f, 0.0f), new Vector3(4.0f, 0.0f, 0.0f) }, 3) ==
                  new Vector3(1.0f, 0.0f, 0.0f), "linearSpline supports Vector3 values");
            Check(Spline.linearSpline(1.0, controlX, new Vector4[] { new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(2.0f, 0.0f, 0.0f, 0.0f), new Vector4(4.0f, 0.0f, 0.0f, 0.0f) }, 3) ==
                  new Vector4(1.0f, 0.0f, 0.0f, 0.0f), "linearSpline supports Vector4 values");
            Check(Spline.linearSpline(1.0, controlX, new Color4[] { new Color4(0.0f, 0.0f, 0.0f, 0.0f), new Color4(2.0f, 0.0f, 0.0f, 0.0f), new Color4(4.0f, 0.0f, 0.0f, 0.0f) }, 3) ==
                  new Color4(1.0f, 0.0f, 0.0f, 0.0f), "linearSpline supports Color4 values");

            double[] scalarControls = new double[] { 0.0, 1.0, 4.0, 9.0 };
            CheckEqual(0.0, Spline.cyclicCatmullRomSpline(0.0, scalarControls, 4),
                       "cyclicCatmullRomSpline returns the first control at t = 0");
            CheckEqual(1.0, Spline.cyclicCatmullRomSpline(1.0, scalarControls, 4),
                       "cyclicCatmullRomSpline returns a control at an integer parameter");
            CheckEqual(2.25, Spline.cyclicCatmullRomSpline(1.5, scalarControls, 4),
                       "cyclicCatmullRomSpline interpolates a segment");
            CheckEqual(-0.25, Spline.cyclicCatmullRomSpline(0.5, scalarControls, 4),
                       "cyclicCatmullRomSpline uses cyclic neighbors");
            CheckEqual(0.0, Spline.cyclicCatmullRomSpline(4.0, scalarControls, 4),
                       "cyclicCatmullRomSpline wraps at the end");
            CheckEqual(4.75, Spline.cyclicCatmullRomSpline(-0.5, scalarControls, 4),
                       "cyclicCatmullRomSpline wraps negative parameters");

            Vector2[] vectorControls = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f)
            };
            Check(Spline.cyclicCatmullRomSpline(0.5, vectorControls, 4).fuzzyEq(new Vector2(0.5f, -0.125f)),
                  "cyclicCatmullRomSpline supports Vector2 values");

            Array<double> scalarArray = new Array<double>();
            scalarArray.append(0.0, 1.0, 4.0, 9.0);
            CheckEqual(Spline.cyclicCatmullRomSpline(0.5, scalarControls, 4),
                       Spline.cyclicCatmullRomSpline(0.5, scalarArray),
                       "cyclicCatmullRomSpline Array overload matches the array overload");

            Array<Vector2> vectorArray = new Array<Vector2>();
            vectorArray.append(vectorControls[0], vectorControls[1], vectorControls[2], vectorControls[3]);
            Check(Spline.cyclicCatmullRomSpline(0.5, vectorArray).fuzzyEq(new Vector2(0.5f, -0.125f)),
                  "cyclicCatmullRomSpline supports Array<Vector2>");

            Check(g3dmath.fuzzyEq(Spline.cyclicCatmullRomSpline(0.0, new float[] { 1.0f, 2.0f, 3.0f, 4.0f }, 4), 1.0f),
                  "cyclicCatmullRomSpline supports float values");
            Check(Spline.cyclicCatmullRomSpline(0.0, new int[] { 1, 2, 3, 4 }, 4) == 1,
                  "cyclicCatmullRomSpline supports int values");
            Check(Spline.cyclicCatmullRomSpline(0.0, new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), Vector3.zero(), Vector3.zero(), Vector3.zero() }, 4) ==
                  new Vector3(1.0f, 0.0f, 0.0f), "cyclicCatmullRomSpline supports Vector3 values");
            Check(Spline.cyclicCatmullRomSpline(0.0, new Vector4[] { new Vector4(1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f) }, 4) ==
                  new Vector4(1.0f, 0.0f, 0.0f, 0.0f), "cyclicCatmullRomSpline supports Vector4 values");
            Check(Spline.cyclicCatmullRomSpline(0.0, new Color3[] { new Color3(1.0f, 0.0f, 0.0f), Color3.black(), Color3.black(), Color3.black() }, 4) ==
                  new Color3(1.0f, 0.0f, 0.0f), "cyclicCatmullRomSpline supports Color3 values");
            Check(Spline.cyclicCatmullRomSpline(0.0, new Color4[] { new Color4(1.0f, 0.0f, 0.0f, 0.0f), new Color4(0.0f, 0.0f, 0.0f, 0.0f), new Color4(0.0f, 0.0f, 0.0f, 0.0f), new Color4(0.0f, 0.0f, 0.0f, 0.0f) }, 4) ==
                  new Color4(1.0f, 0.0f, 0.0f, 0.0f), "cyclicCatmullRomSpline supports Color4 values");

            Array<float> floatArray = new Array<float>();
            floatArray.append(1.0f, 2.0f, 3.0f, 4.0f);
            Check(g3dmath.fuzzyEq(Spline.cyclicCatmullRomSpline(0.0, floatArray), 1.0f),
                  "cyclicCatmullRomSpline supports Array<float>");
            Array<int> intArray = new Array<int>();
            intArray.append(1, 2, 3, 4);
            Check(Spline.cyclicCatmullRomSpline(0.0, intArray) == 1,
                  "cyclicCatmullRomSpline supports Array<int>");
            Array<Vector3> vector3Array = new Array<Vector3>();
            vector3Array.append(new Vector3(1.0f, 0.0f, 0.0f), Vector3.zero(), Vector3.zero(), Vector3.zero());
            Check(Spline.cyclicCatmullRomSpline(0.0, vector3Array) == new Vector3(1.0f, 0.0f, 0.0f),
                  "cyclicCatmullRomSpline supports Array<Vector3>");
            Array<Vector4> vector4Array = new Array<Vector4>();
            vector4Array.append(new Vector4(1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            Check(Spline.cyclicCatmullRomSpline(0.0, vector4Array) == new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                  "cyclicCatmullRomSpline supports Array<Vector4>");
            Array<Color3> color3Array = new Array<Color3>();
            color3Array.append(new Color3(1.0f, 0.0f, 0.0f), Color3.black(), Color3.black(), Color3.black());
            Check(Spline.cyclicCatmullRomSpline(0.0, color3Array) == new Color3(1.0f, 0.0f, 0.0f),
                  "cyclicCatmullRomSpline supports Array<Color3>");
            Array<Color4> color4Array = new Array<Color4>();
            color4Array.append(new Color4(1.0f, 0.0f, 0.0f, 0.0f), new Color4(0.0f, 0.0f, 0.0f, 0.0f), new Color4(0.0f, 0.0f, 0.0f, 0.0f), new Color4(0.0f, 0.0f, 0.0f, 0.0f));
            Check(Spline.cyclicCatmullRomSpline(0.0, color4Array) == new Color4(1.0f, 0.0f, 0.0f, 0.0f),
                  "cyclicCatmullRomSpline supports Array<Color4>");

            printf("passed\n");
        }
    }
}
