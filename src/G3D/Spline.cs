/**
 @file spline.h
  
 @maintainer Morgan McGuire, matrix@graphics3d.com
  
 @created 2004-07-25
 @edited 2004-07-25
*/

using System;

namespace G3D
{
    /**
 Interpolates a property according to a piecewise linear spline.  This provides
 C0 continuity but the derivatives are not smooth.
 <P>
 Example:
 <CODE>
    const double times[] = {MIDNIGHT,               SUNRISE - HOUR,         SUNRISE,              SUNRISE + sunRiseAndSetTime / 4, SUNRISE + sunRiseAndSetTime, SUNSET - sunRiseAndSetTime, SUNSET - sunRiseAndSetTime / 2, SUNSET,               SUNSET + HOUR/2,     DAY};
    const Color3 color[] = {Color3(0, .0, .1),      Color3(0, .0, .1),      Color3::black(),        Color3::black(),                   Color3::white() * .25,         Color3::white() * .25,        Color3(.5, .2, .2),             Color3(.05, .05, .1),   Color3(0, .0, .1), Color3(0, .0, .1)};
    ambient = linearSpline(time, times, color, 10);
 </CODE>

  @param x         The spline is a function of x; this is the sample to choose.
  @param controlX  controlX[i], controlY[i] is a control points.  It is assumed
                   that controlX are strictly increasing.  XType must support
                   the "<" operator and a subtraction operator that returns
                   a number.
  @param controlY  YType must support multiplication and addition.
  @param numControl The number of control points.
 */
    public static class Spline
    {
        private delegate bool LessThan<T>(double x, T controlX);
        private delegate double DifferenceFromX<T>(T controlX, double x);
        private delegate double Difference<T>(T left, T right);
        private delegate T LinearCombination<T>(T current, T previous, double currentWeight, double previousWeight);
        private delegate T CatmullRom<T>(T p0, T p1, T p2, T p3, double t);

        private static bool lessThanDouble(double x, double controlX)
        {
            return x < controlX;
        }

        private static bool lessThanFloat(double x, float controlX)
        {
            return x < controlX;
        }

        private static bool lessThanInt(double x, int controlX)
        {
            return x < controlX;
        }

        private static double differenceFromXDouble(double controlX, double x)
        {
            return controlX - x;
        }

        private static double differenceFromXFloat(float controlX, double x)
        {
            return controlX - x;
        }

        private static double differenceFromXInt(int controlX, double x)
        {
            return controlX - x;
        }

        private static double differenceDouble(double left, double right)
        {
            return left - right;
        }

        private static double differenceFloat(float left, float right)
        {
            return left - right;
        }

        private static double differenceInt(int left, int right)
        {
            return left - right;
        }

        private static double linearCombinationDouble(double current, double previous, double currentWeight, double previousWeight)
        {
            return current * currentWeight + previous * previousWeight;
        }

        private static float linearCombinationFloat(float current, float previous, double currentWeight, double previousWeight)
        {
            return (float)(((double)current * currentWeight) + ((double)previous * previousWeight));
        }

        private static int linearCombinationInt(int current, int previous, double currentWeight, double previousWeight)
        {
            return (int)(((double)current * currentWeight) + ((double)previous * previousWeight));
        }

        private static Vector2 linearCombinationVector2(Vector2 current, Vector2 previous, double currentWeight, double previousWeight)
        {
            return current * (float)currentWeight + previous * (float)previousWeight;
        }

        private static Vector3 linearCombinationVector3(Vector3 current, Vector3 previous, double currentWeight, double previousWeight)
        {
            return current * (float)currentWeight + previous * (float)previousWeight;
        }

        private static Vector4 linearCombinationVector4(Vector4 current, Vector4 previous, double currentWeight, double previousWeight)
        {
            return current * (float)currentWeight + previous * (float)previousWeight;
        }

        private static Color3 linearCombinationColor3(Color3 current, Color3 previous, double currentWeight, double previousWeight)
        {
            return current * (float)currentWeight + previous * (float)previousWeight;
        }

        private static Color4 linearCombinationColor4(Color4 current, Color4 previous, double currentWeight, double previousWeight)
        {
            return current * (float)currentWeight + previous * (float)previousWeight;
        }

        private static TY linearSplineCore<TX, TY>(
            double x,
            TX[] controlX,
            TY[] controlY,
            int numControl,
            LessThan<TX> lessThan,
            DifferenceFromX<TX> differenceFromX,
            Difference<TX> difference,
            LinearCombination<TY> linearCombination)
        {
            Debug.debugAssert(numControl >= 1);
            if (numControl < 1)
            {
                throw new ArgumentOutOfRangeException("numControl");
            }
            if (controlX == null)
            {
                throw new ArgumentNullException("controlX");
            }
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            if (controlX.Length < numControl || controlY.Length < numControl)
            {
                throw new ArgumentOutOfRangeException("numControl");
            }

            // Off the beginning
            if ((numControl == 1) || lessThan(x, controlX[0]))
            {
                return controlY[0];
            }

            for (int i = 1; i < numControl; ++i)
            {
                if (lessThan(x, controlX[i]))
                {
                    double alpha = differenceFromX(controlX[i], x) / difference(controlX[i], controlX[i - 1]);
                    return linearCombination(controlY[i], controlY[i - 1], 1 - alpha, alpha);
                }
            }

            // Off the end
            return controlY[numControl - 1];
        }

        public static double linearSpline(double x, double[] controlX, double[] controlY, int numControl)
        {
            return linearSplineCore<double, double>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationDouble);
        }

        public static double linearSpline(double x, float[] controlX, double[] controlY, int numControl)
        {
            return linearSplineCore<float, double>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationDouble);
        }

        public static double linearSpline(double x, int[] controlX, double[] controlY, int numControl)
        {
            return linearSplineCore<int, double>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationDouble);
        }

        public static float linearSpline(double x, double[] controlX, float[] controlY, int numControl)
        {
            return linearSplineCore<double, float>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationFloat);
        }

        public static float linearSpline(double x, float[] controlX, float[] controlY, int numControl)
        {
            return linearSplineCore<float, float>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationFloat);
        }

        public static float linearSpline(double x, int[] controlX, float[] controlY, int numControl)
        {
            return linearSplineCore<int, float>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationFloat);
        }

        public static int linearSpline(double x, double[] controlX, int[] controlY, int numControl)
        {
            return linearSplineCore<double, int>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationInt);
        }

        public static int linearSpline(double x, float[] controlX, int[] controlY, int numControl)
        {
            return linearSplineCore<float, int>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationInt);
        }

        public static int linearSpline(double x, int[] controlX, int[] controlY, int numControl)
        {
            return linearSplineCore<int, int>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationInt);
        }

        public static Vector2 linearSpline(double x, double[] controlX, Vector2[] controlY, int numControl)
        {
            return linearSplineCore<double, Vector2>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationVector2);
        }

        public static Vector2 linearSpline(double x, float[] controlX, Vector2[] controlY, int numControl)
        {
            return linearSplineCore<float, Vector2>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationVector2);
        }

        public static Vector2 linearSpline(double x, int[] controlX, Vector2[] controlY, int numControl)
        {
            return linearSplineCore<int, Vector2>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationVector2);
        }

        public static Vector3 linearSpline(double x, double[] controlX, Vector3[] controlY, int numControl)
        {
            return linearSplineCore<double, Vector3>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationVector3);
        }

        public static Vector3 linearSpline(double x, float[] controlX, Vector3[] controlY, int numControl)
        {
            return linearSplineCore<float, Vector3>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationVector3);
        }

        public static Vector3 linearSpline(double x, int[] controlX, Vector3[] controlY, int numControl)
        {
            return linearSplineCore<int, Vector3>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationVector3);
        }

        public static Vector4 linearSpline(double x, double[] controlX, Vector4[] controlY, int numControl)
        {
            return linearSplineCore<double, Vector4>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationVector4);
        }

        public static Vector4 linearSpline(double x, float[] controlX, Vector4[] controlY, int numControl)
        {
            return linearSplineCore<float, Vector4>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationVector4);
        }

        public static Vector4 linearSpline(double x, int[] controlX, Vector4[] controlY, int numControl)
        {
            return linearSplineCore<int, Vector4>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationVector4);
        }

        public static Color3 linearSpline(double x, double[] controlX, Color3[] controlY, int numControl)
        {
            return linearSplineCore<double, Color3>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationColor3);
        }

        public static Color3 linearSpline(double x, float[] controlX, Color3[] controlY, int numControl)
        {
            return linearSplineCore<float, Color3>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationColor3);
        }

        public static Color3 linearSpline(double x, int[] controlX, Color3[] controlY, int numControl)
        {
            return linearSplineCore<int, Color3>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationColor3);
        }

        public static Color4 linearSpline(double x, double[] controlX, Color4[] controlY, int numControl)
        {
            return linearSplineCore<double, Color4>(x, controlX, controlY, numControl, lessThanDouble, differenceFromXDouble, differenceDouble, linearCombinationColor4);
        }

        public static Color4 linearSpline(double x, float[] controlX, Color4[] controlY, int numControl)
        {
            return linearSplineCore<float, Color4>(x, controlX, controlY, numControl, lessThanFloat, differenceFromXFloat, differenceFloat, linearCombinationColor4);
        }

        public static Color4 linearSpline(double x, int[] controlX, Color4[] controlY, int numControl)
        {
            return linearSplineCore<int, Color4>(x, controlX, controlY, numControl, lessThanInt, differenceFromXInt, differenceInt, linearCombinationColor4);
        }

        private static double catmullRomDouble(double p0, double p1, double p2, double p3, double t)
        {
            double term0 = 2.0 * p1;
            double term1 = (-p0 + p2) * t;
            double term2 = (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t * t;
            double term3 = (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t * t * t;
            return 0.5 * (term0 + term1 + term2 + term3);
        }

        private static float catmullRomFloat(float p0, float p1, float p2, float p3, double t)
        {
            float term0 = 2.0f * p1;
            double term1 = (-p0 + p2) * t;
            double term2 = (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t * t;
            double term3 = (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t * t * t;
            return (float)(0.5 * (term0 + term1 + term2 + term3));
        }

        private static int catmullRomInt(int p0, int p1, int p2, int p3, double t)
        {
            int term0 = 2 * p1;
            double term1 = (-p0 + p2) * t;
            double term2 = (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t;
            double term3 = (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t;
            return (int)(0.5 * (term0 + term1 + term2 + term3));
        }

        private static Vector2 catmullRomVector2(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, double t)
        {
            float f = (float)t;
            Vector2 term0 = 2.0f * p1;
            Vector2 term1 = (-p0 + p2) * f;
            Vector2 term2 = (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * f * f;
            Vector2 term3 = (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * f * f * f;
            return 0.5f * (term0 + term1 + term2 + term3);
        }

        private static Vector3 catmullRomVector3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, double t)
        {
            float f = (float)t;
            Vector3 term0 = 2.0f * p1;
            Vector3 term1 = (-p0 + p2) * f;
            Vector3 term2 = (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * f * f;
            Vector3 term3 = (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * f * f * f;
            return 0.5f * (term0 + term1 + term2 + term3);
        }

        private static Vector4 catmullRomVector4(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, double t)
        {
            float f = (float)t;
            Vector4 term0 = 2.0f * p1;
            Vector4 term1 = (-p0 + p2) * f;
            Vector4 term2 = (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * f * f;
            Vector4 term3 = (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * f * f * f;
            return 0.5f * (term0 + term1 + term2 + term3);
        }

        private static Color3 catmullRomColor3(Color3 p0, Color3 p1, Color3 p2, Color3 p3, double t)
        {
            float f = (float)t;
            Color3 term0 = 2.0f * p1;
            Color3 term1 = (-p0 + p2) * f;
            Color3 term2 = (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * f * f;
            Color3 term3 = (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * f * f * f;
            return 0.5f * (term0 + term1 + term2 + term3);
        }

        private static Color4 catmullRomColor4(Color4 p0, Color4 p1, Color4 p2, Color4 p3, double t)
        {
            float f = (float)t;
            Color4 term0 = 2.0f * p1;
            Color4 term1 = (-p0 + p2) * f;
            Color4 term2 = (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * f * f;
            Color4 term3 = (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * f * f * f;
            return 0.5f * (term0 + term1 + term2 + term3);
        }

        private static T cyclicCatmullRomSplineCore<T>(double t, T[] controlY, int numPoints, CatmullRom<T> catmullRom)
        {
            Debug.debugAssert(numPoints >= 3);
            if (numPoints < 3)
            {
                throw new ArgumentOutOfRangeException("numPoints");
            }
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            if (controlY.Length < numPoints)
            {
                throw new ArgumentOutOfRangeException("numPoints");
            }

            t = g3dmath.wrap(t, numPoints);

            // Find the indices of adjacent control points    
            int i = g3dmath.iFloor(t);

            // Compute the distance from the control point
            t = t - i;

            // Shift back one point for correct indexing
            i += numPoints - 1;

            // Pick up four control points
            T p0 = controlY[(i + 0) % numPoints];
            T p1 = controlY[(i + 1) % numPoints];
            T p2 = controlY[(i + 2) % numPoints];
            T p3 = controlY[(i + 3) % numPoints];

            return catmullRom(p0, p1, p2, p3, t);
        }

        public static double cyclicCatmullRomSpline(double t, double[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<double>(t, controlY, numPoints, catmullRomDouble);
        }

        public static float cyclicCatmullRomSpline(double t, float[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<float>(t, controlY, numPoints, catmullRomFloat);
        }

        public static int cyclicCatmullRomSpline(double t, int[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<int>(t, controlY, numPoints, catmullRomInt);
        }

        public static Vector2 cyclicCatmullRomSpline(double t, Vector2[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<Vector2>(t, controlY, numPoints, catmullRomVector2);
        }

        public static Vector3 cyclicCatmullRomSpline(double t, Vector3[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<Vector3>(t, controlY, numPoints, catmullRomVector3);
        }

        public static Vector4 cyclicCatmullRomSpline(double t, Vector4[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<Vector4>(t, controlY, numPoints, catmullRomVector4);
        }

        public static Color3 cyclicCatmullRomSpline(double t, Color3[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<Color3>(t, controlY, numPoints, catmullRomColor3);
        }

        public static Color4 cyclicCatmullRomSpline(double t, Color4[] controlY, int numPoints)
        {
            return cyclicCatmullRomSplineCore<Color4>(t, controlY, numPoints, catmullRomColor4);
        }

        /**
 A cubic spline with regularly spaced 
 control points.  The spline interpolates
 the control points.  The spline
 will wrap from the last point back to the first.

 The t parameter is on the range [0, controlY.size()],
 where integers correspond to control points exactly.

 @cite http://www.mvps.org/directx/articles/catmull/
 */
        public static double cyclicCatmullRomSpline(double t, Array<double> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }

        public static float cyclicCatmullRomSpline(double t, Array<float> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }

        public static int cyclicCatmullRomSpline(double t, Array<int> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }

        public static Vector2 cyclicCatmullRomSpline(double t, Array<Vector2> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }

        public static Vector3 cyclicCatmullRomSpline(double t, Array<Vector3> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }

        public static Vector4 cyclicCatmullRomSpline(double t, Array<Vector4> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }

        public static Color3 cyclicCatmullRomSpline(double t, Array<Color3> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }

        public static Color4 cyclicCatmullRomSpline(double t, Array<Color4> controlY)
        {
            if (controlY == null)
            {
                throw new ArgumentNullException("controlY");
            }
            return cyclicCatmullRomSpline(t, controlY.getCArray(), controlY.size());
        }
    }
}
