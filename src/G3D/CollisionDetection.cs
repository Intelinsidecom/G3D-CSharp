/**
  @file CollisionDetection.h


  Moving collision detection for simple primitives.

  @author Morgan McGuire, matrix@graphics3d.com
  @cite Spherical collision based on Paul Nettle's
  ftp://ftp.3dmaileffects.com/pub/FluidStudios/CollisionDetection/Fluid_Studios_Generic_Collision_Detection_for_Games_Using_Ellipsoids.pdf
  and comments by Max McGuire.  Ray-sphere intersection by Eric Haines.
  Box-Box intersection written by Kevin Egan.
  Thanks to Max McGuire of Iron Lore for various bug fixes.

  @created 2001-11-19
  @edited  2006-01-10

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
      Moving collision detection for simple primitives.

      The C++ class uses a static "ignore" argument as the default value of
      several output references.  Because C# does not support default
      reference arguments, each of those functions is provided with an
      additional overload that omits the corresponding output parameter.
     */
    public static class CollisionDetection
    {
        /**
          Converts an index [0, 15] to the corresponding separating axis.
          Does not return normalized vector in the edge-edge case
          (indices 6 through 15).

         @param separatingAxisIndex	Separating axis.
         @param box1					Box 1.
         @param box2					Box 2.

         @return Axis that separates the two boxes.
       */
        public static Vector3 separatingAxisForSolidBoxSolidBox(
                int separatingAxisIndex,
                Box box1,
                Box box2)
        {
            Debug.debugAssert(separatingAxisIndex >= 0);
            Debug.debugAssert(separatingAxisIndex < 15);
            Vector3 axis;
            if (separatingAxisIndex < 3)
            {
                axis = box1.axis(separatingAxisIndex);
            }
            else if (separatingAxisIndex < 6)
            {
                axis = box2.axis(separatingAxisIndex - 3);
            }
            else
            {
                int box1Index = (separatingAxisIndex - 6) / 3;
                int box2Index = (separatingAxisIndex - 6) % 3;
                axis = box1.axis(box1Index).cross(box2.axis(box2Index));
            }
            return axis;
        }

        /**
         Tests whether two boxes have axes that are parallel to
         each other.  If they are, axis1 and axis2 are set to be
         the parallel axes for both box1 and box2 respectively.

         @param ca			Dot products of each of the boxes axes
         @param epsilon	Fudge factor (small unit by which the dot
        					products may vary and still be considered
        					zero).
         @param axis1		Parallel Axis 1. [Post Condition]
         @param axis2		Parallel Axis 2. [Post Condition]

         @return true  - If boxes have a parallel axis
         @return false - otherwise.
       */
        public static bool parallelAxisForSolidBoxSolidBox(
                double[] ca,
                double epsilon,
                out int axis1,
                out int axis2)
        {
            double parallelDot = 1.0 - epsilon;
            for (int i = 0; i < 9; i++)
            {
                if (ca[i] >= parallelDot)
                {
                    axis1 = i / 3;
                    axis2 = i % 3;
                    return true;
                }
            }
            axis1 = 0;
            axis2 = 0;
            return false;
        }

        /**
          Calculates the projected distance between the two boxes along
          the specified separating axis, negative distances correspond
          to an overlap along that separating axis.  The distance is not
          divided by denominator dot(L, L), see
          penetrationDepthForFixedSphereFixedBox() for more details

          @param separatingAxisIndex
         @param a Box 1's bounding sphere vector
         @param b Box 2's bounding sphere vector
         @param D Vector between Box 1 and Box 2's center points
         @param c Pointer to array of dot products of the axes of Box 1
         @param ca Pointer to array of unsigned dot products of the axes
         @param ad Pointer to array of dot products of Box 1 axes and D.
         @param bd Pointer to array of dot products of Box 2 axes and D.

          @return Projected distance between the two boxes along the
          specified separating axis.
         */
        public static float projectedDistanceForSolidBoxSolidBox(
                int separatingAxisIndex,
                Vector3 a,
                Vector3 b,
                Vector3 D,
                double[] c,
                double[] ca,
                double[] ad,
                double[] bd)
        {
            float R0 = 0.0f;
            float R1 = 0.0f;
            float R = 0.0f;
            switch (separatingAxisIndex)
            {
                case 0:
                    // A0
                    R0 = a[0];
                    R1 = (float)(b[0] * ca[0] + b[1] * ca[1] + b[2] * ca[2]);
                    R = Math.Abs((float)ad[0]);
                    break;
                case 1:
                    // A1
                    R0 = a[1];
                    R1 = (float)(b[0] * ca[3] + b[1] * ca[4] + b[2] * ca[5]);
                    R = Math.Abs((float)ad[1]);
                    break;
                case 2:
                    // A2
                    R0 = a[2];
                    R1 = (float)(b[0] * ca[6] + b[1] * ca[7] + b[2] * ca[8]);
                    R = Math.Abs((float)ad[2]);
                    break;
                case 3:
                    // B0
                    R0 = (float)(a[0] * ca[0] + a[1] * ca[3] + a[2] * ca[6]);
                    R1 = b[0];
                    R = Math.Abs((float)bd[0]);
                    break;
                case 4:
                    // B1
                    R0 = (float)(a[0] * ca[1] + a[1] * ca[4] + a[2] * ca[7]);
                    R1 = b[1];
                    R = Math.Abs((float)bd[1]);
                    break;
                case 5:
                    // B2
                    R0 = (float)(a[0] * ca[2] + a[1] * ca[5] + a[2] * ca[8]);
                    R1 = b[2];
                    R = Math.Abs((float)bd[2]);
                    break;
                case 6:
                    // A0 x B0
                    R0 = (float)(a[1] * ca[6] + a[2] * ca[3]);
                    R1 = (float)(b[1] * ca[2] + b[2] * ca[1]);
                    R = Math.Abs((float)(c[3] * ad[2] - c[6] * ad[1]));
                    break;
                case 7:
                    // A0 x B1
                    R0 = (float)(a[1] * ca[7] + a[2] * ca[4]);
                    R1 = (float)(b[0] * ca[2] + b[2] * ca[0]);
                    R = Math.Abs((float)(c[4] * ad[2] - c[7] * ad[1]));
                    break;
                case 8:
                    // A0 x B2
                    R0 = (float)(a[1] * ca[8] + a[2] * ca[5]);
                    R1 = (float)(b[0] * ca[1] + b[1] * ca[0]);
                    R = Math.Abs((float)(c[5] * ad[2] - c[8] * ad[1]));
                    break;
                case 9:
                    // A1 x B0
                    R0 = (float)(a[0] * ca[6] + a[2] * ca[0]);
                    R1 = (float)(b[1] * ca[5] + b[2] * ca[4]);
                    R = Math.Abs((float)(c[6] * ad[0] - c[0] * ad[2]));
                    break;
                case 10:
                    // A1 x B1
                    R0 = (float)(a[0] * ca[7] + a[2] * ca[1]);
                    R1 = (float)(b[0] * ca[5] + b[2] * ca[3]);
                    R = Math.Abs((float)(c[7] * ad[0] - c[1] * ad[2]));
                    break;
                case 11:
                    // A1 x B2
                    R0 = (float)(a[0] * ca[8] + a[2] * ca[2]);
                    R1 = (float)(b[0] * ca[4] + b[1] * ca[3]);
                    R = Math.Abs((float)(c[8] * ad[0] - c[2] * ad[2]));
                    break;
                case 12:
                    // A2 x B0
                    R0 = (float)(a[0] * ca[3] + a[1] * ca[0]);
                    R1 = (float)(b[1] * ca[8] + b[2] * ca[7]);
                    R = Math.Abs((float)(c[0] * ad[1] - c[3] * ad[0]));
                    break;
                case 13:
                    // A2 x B1
                    R0 = (float)(a[0] * ca[4] + a[1] * ca[1]);
                    R1 = (float)(b[0] * ca[8] + b[2] * ca[6]);
                    R = Math.Abs((float)(c[1] * ad[1] - c[4] * ad[0]));
                    break;
                case 14:
                    // A2 x B2
                    R0 = (float)(a[0] * ca[5] + a[1] * ca[2]);
                    R1 = (float)(b[0] * ca[7] + b[1] * ca[6]);
                    R = Math.Abs((float)(c[2] * ad[1] - c[5] * ad[0]));
                    break;
                default:
                    Debug.debugAssertM(false, "fell through switch statement");
                    break;
            }

            return (R - (R0 + R1));
        }

        /**
         Creates a set of standard information about two boxes in order to
         solve for their collision.  This information includes a vector to
         the radius of the bounding sphere for each box, the vector between
         each boxes' center and a series of dot products between differing
         important vectors.  These dot products include those between the axes
         of both boxes (signed and unsigned values), and the dot products
         between all the axes of box1 and the boxes' center vector and box2
         and the boxes' center vector.

         @pre The following space requirements must be met:
             		- c[]  9 elements
             		- ca[] 9 elements
             		- ad[] 3 elements
             		- bd[] 3 elements

         @cite dobted from David Eberly's papers, variables used in this function
         correspond to variables used in pages 6 and 7 in the pdf
         http://www.magic-software.com/Intersection.html
         http://www.magic-software.com/Documentation/DynamicCollisionDetection.pdf

         @note Links are out-dated. (Kept to preserve origin and authorship)

         @param box1 Box 1
         @param box2 Box 2
         @param a Box 1's bounding sphere vector
         @param b Box 2's bounding sphere vector
         @param D Vector between Box 1 and Box 2's center points
         @param c Pointer to array of dot products of the axes of Box 1
         @param ca Pointer to array of unsigned dot products of the axes
         @param ad Pointer to array of dot products of Box 1 axes and D.
         @param bd Pointer to array of dot products of Box 2 axes and D.
         */
        public static void fillSolidBoxSolidBoxInfo(
                Box box1,
                Box box2,
                out Vector3 a,
                out Vector3 b,
                out Vector3 D,
                double[] c,
                double[] ca,
                double[] ad,
                double[] bd)
        {
            // length between center and each side of box1 and box2
            a = box1.extent() * 0.5f;
            b = box2.extent() * 0.5f;

            // difference between centers of box1 and box2
            D = box2.center() - box1.center();

            // store the value of all possible dot products between the
            // axes of box1 and box2, c_{row, col} in the Eberly paper
            // corresponds to c[row * 3 + col] for this 9 element array.
            //
            // c[] holds signed values, ca[] hold absolute values
            for (int i = 0; i < 9; i++)
            {
                c[i] = box1.axis(i / 3).dot(box2.axis(i % 3));
                ca[i] = Math.Abs(c[i]);
            }

            // store all possible dot products between the axes of box1 and D,
            // as well as the axes of box2 and D
            for (int i = 0; i < 3; i++)
            {
                ad[i] = box1.axis(i).dot(D);
                bd[i] = box2.axis(i).dot(D);
            }
        }

        /**
         Performs a simple bounding sphere check between two boxes to determine
         whether these boxes could <i>possibly</i> intersect.  This is a very
         cheap operation (three dot products, two sqrts and a few others).  If
         it returns true, an intersection is possible, but not necessarily
         guaranteed.

         @param a Vector from box A's center to an outer vertex
         @param b Vector from box B's center to an outer vertex
         @param D Distance between the centers of the two boxes

         @return true - if possible intersection
         @return false - otherwise (This does not guarantee an intersection)
         */
        public static bool conservativeBoxBoxTest(
                Vector3 a,
                Vector3 b,
                Vector3 D)
        {
            // do a quick bounding sphere test because it is relatively
            // cheap, (three dot products, two sqrts, and a few others)
            double boxRadius1 = a.magnitude();
            double boxRadius2 = b.magnitude();
            return (D.squaredMagnitude() < g3dmath.square(boxRadius1 + boxRadius2));
        }

        /**
          Determines whether two fixed solid boxes intersect.

          @note To speed up collision detection, the lastSeparatingAxis from
          the previous time step can be passed in and that plane can be
          checked first.  If the separating axis was not saved, or if the
          two boxes intersected then lastSeparatingAxis should equal -1.

         @cite Adobted from David Eberly's papers, variables used in this function
         correspond to variables used in pages 6 and 7 in the pdf
         http://www.magic-software.com/Intersection.html
         http://www.magic-software.com/Documentation/DynamicCollisionDetection.pdf

         @param box1				Box 1.
         @param box2				Box 2.
         @param lastSeparatingAxis	Last separating axis.
        	  							(optimization - see note)

         @return true  - Intersection.
         @return false - otherwise.
         */
        public static bool fixedSolidBoxIntersectsFixedSolidBox(
            Box box1,
            Box box2,
            int lastSeparatingAxis = -1)
        {
            // for explanations of the variable please refer to the
            // paper and fillSolidBoxSolidBoxInfo()
            Vector3 a;
            Vector3 b;
            Vector3 D;
            double[] c = new double[9];
            double[] ca = new double[9];
            double[] ad = new double[3];
            double[] bd = new double[3];

            fillSolidBoxSolidBoxInfo(box1, box2, out a, out b, out D, c, ca, ad, bd);

            int dummy1, dummy2;
            bool parallelAxes = parallelAxisForSolidBoxSolidBox(ca, 0.00001,
                out dummy1, out dummy2);

            // check the separating axis from the last time step
            if (lastSeparatingAxis != -1 &&
                    (lastSeparatingAxis < 6 || !parallelAxes))
            {
                double projectedDistance = projectedDistanceForSolidBoxSolidBox(
                    lastSeparatingAxis, a, b, D, c, ca, ad, bd);

                // the separating axis from the last time step is still
                // valid, the boxes do not intersect
                if (projectedDistance > 0.0)
                {
                    return false;
                }
            }

            // test if the boxes can be separated by a plane normal to
            // any of the three axes of box1, any of the three axes of box2,
            // or any of the 9 possible cross products of axes from box1
            // and box2
            for (int i = 0; i < 15; i++)
            {
                // do not need to check edge-edge cases if any two of
                // the axes are parallel
                if (parallelAxes && i == 6)
                {
                    return true;
                }

                double projectedDistance =
                    projectedDistanceForSolidBoxSolidBox(i, a, b, D, c, ca, ad, bd);

                // found a separating axis, the boxes do not intersect
                if (projectedDistance > 0.0)
                {
                    return false;
                }
            }

            return true;
        }

        /**
         Calculates the closest points on two lines with each other.   If the
         lines are parallel then using the starting point, else calculate the
         closest point on each line to the other.

         @note This is very similiar to calculating the intersection of two lines.
         Logically then, the two points calculated would be identical if calculated
         with inifinite precision, but with the finite precision of floating point
         calculations, these values could (will) differ as the line slope approaches
         zero or inifinity.

         @cite variables and algorithm based on derivation at the following website:
          	    http://softsurfer.com/Archive/algorithm_0106/algorithm_0106.htm

         @param line1		Line 1.
         @param line2		Line 2.
         @param closest1	Closest point on line 1.
         @param closest2	Closest point on line 2.
  	 */
        public static void closestPointsBetweenLineAndLine(
                Line line1,
                Line line2,
                out Vector3 closest1,
                out Vector3 closest2)
        {
            // TODO make accessors for Line that don't make a copy of data
            Vector3 P0 = line1.point();
            Vector3 u = line1.direction();
            Vector3 Q0 = line2.point();
            Vector3 v = line2.direction();
            Vector3 w0 = P0 - Q0;

            // a = 1.0, c = 1.0
            double b = u.dot(v);
            double d = u.dot(w0);
            double e = v.dot(w0);
            double D = 1.0 - b * b;
            double sc, tc;

            const double epsilon = 0.00001;

            if (D < epsilon)
            {
                // lines are parallel, choose P0 as one point, find the point
                // on line2 that is closest to P0
                sc = 0.0;
                tc = (b > 1.0) ? (d / b) : (e / 1.0);
            }
            else
            {
                // lines are not parallel
                sc = (b * e - 1.0 * d) / D;
                tc = (1.0 * e - b * d) / D;
            }

            closest1 = P0 + (sc * u);
            closest2 = Q0 + (tc * v);
        }

        /**
         Calculates the depth of penetration between two fixed boxes.
         Contact normal faces away from box1 and into box2.  If there is
         contact, only one contact point is returned.  The minimally
         violated separating plane is computed
            - if the separating axis corresponds to a face
                the contact point is half way between the deepest vertex
                and the face
            - if the separating axis corresponds to two edges
                the contact point is the midpoint of the smallest line
                segment between the two edge lines

         @note This is very similiar to calculating the intersection of two lines.
         Logically then, the two points calculated would be identical if calculated
         with inifinite precision, but with the finite precision of floating point
         calculations, these values could (will) differ as the line slope approaches
         zero or inifinity.

         @cite adobted from David Eberly's papers, variables used in this function
         correspond to variables used in pages 6 and 7 in the pdf
         http://www.magic-software.com/Intersection.html
         http://www.magic-software.com/Documentation/DynamicCollisionDetection.pdf

         @param box1				Box 1
         @param box2				Box 2
         @param contactPoints		Contact point between boxes. [Post Condition]
         @param contactNormals		Surface normal at contact point. [Post Condition]
         @param lastSeparatingAxis	Last separating axis. (Used for optimization)

         @return Depth of penetration between the two boxes.  If there is no
  	   intersection between the boxes, then a negative value is returned.
  	 */
        public static float penetrationDepthForFixedBoxFixedBox(
            Box box1,
            Box box2,
            Array<Vector3> contactPoints,
            Array<Vector3> contactNormals,
            int lastSeparatingAxis = -1)
        {
            contactPoints.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);
            contactNormals.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);

            Vector3 a;
            Vector3 b;
            Vector3 D;
            double[] c = new double[9];
            double[] ca = new double[9];
            double[] ad = new double[3];
            double[] bd = new double[3];

            Debug.debugAssert(lastSeparatingAxis >= -1);
            Debug.debugAssert(lastSeparatingAxis < 15);

            fillSolidBoxSolidBoxInfo(box1, box2, out a, out b, out D, c, ca, ad, bd);

            int axis1, axis2;
            bool parallelAxes = parallelAxisForSolidBoxSolidBox(ca, 0.00001,
                out axis1, out axis2);

            // check the separating axis from the last time step
            if (lastSeparatingAxis != -1 &&
                    (lastSeparatingAxis < 6 || !parallelAxes))
            {
                float projectedDistance = projectedDistanceForSolidBoxSolidBox(
                    lastSeparatingAxis, a, b, D, c, ca, ad, bd);

                // the separating axis from the last time step is still
                // valid, the boxes do not intersect
                if (projectedDistance > 0.0)
                {
                    return -projectedDistance;
                }
            }

            // test if the boxes can be separated by a plane normal to
            // any of the three axes of box1, any of the three axes of box2,
            // (test 9 possible cross products later)
            float penetration = -(float)g3dmath.inf();
            int penetrationAxisIndex = -1;

            for (int i = 0; i < 6; i++)
            {
                float projectedDistance =
                    projectedDistanceForSolidBoxSolidBox(i, a, b, D, c, ca, ad, bd);

                // found a separating axis, the boxes do not intersect
                if (projectedDistance > 0.0)
                {
                    return -projectedDistance;
                }

                // keep track of the axis that is least violated
                if (projectedDistance > penetration)
                {
                    penetration = projectedDistance;
                    penetrationAxisIndex = i;
                }
            }

            // for each edge-edge case we have to adjust the magnitude of
            // penetration since we did not include the dot(L, L) denominator
            // that can be smaller than 1.0 for the edge-edge cases.
            if (!parallelAxes)
            {
                double[] edgeDistances = new double[9];

                // run through edge-edge cases to see if we can find a separating axis
                for (int i = 6; i < 15; i++)
                {
                    float projectedDistance =
                        projectedDistanceForSolidBoxSolidBox(i, a, b, D, c, ca, ad, bd);

                    // found a separating axis, the boxes do not intersect,
                    // correct magnitude and return projected distance
                    if (projectedDistance > 0.0)
                    {
                        Vector3 edgeAxis = separatingAxisForSolidBoxSolidBox(i, box1, box2);
                        projectedDistance /= edgeAxis.dot(edgeAxis);
                        return -projectedDistance;
                    }

                    edgeDistances[i - 6] = projectedDistance;
                }

                // no separating axis found, the boxes do intersect,
                // correct the magnitudes of the projectedDistance values
                for (int i = 6; i < 15; i++)
                {
                    // find the negative penetration value with the smallest magnitude,
                    // the adjustment done for the edge-edge cases only increases
                    // magnitude by dividing by a number smaller than 1 and greater than 0
                    float projectedDistance = (float)edgeDistances[i - 6];
                    if (projectedDistance > penetration)
                    {
                        Vector3 edgeAxis = separatingAxisForSolidBoxSolidBox(i, box1, box2);
                        projectedDistance /= edgeAxis.dot(edgeAxis);
                        if (projectedDistance > penetration)
                        {
                            penetration = projectedDistance;
                            penetrationAxisIndex = i;
                        }
                    }
                }
            }

            // get final separating axis vector
            Vector3 L = separatingAxisForSolidBoxSolidBox(penetrationAxisIndex,
                box1, box2);

            // set L to be the normal that faces away from box1
            if (L.dot(D) < 0)
            {
                L = -L;
            }

            Vector3 contactPoint;

            if (penetrationAxisIndex < 6)
            {
                // vertex to face collision, find deepest colliding vertex
                Box vertexBox;
                Vector3 faceNormal = L;

                // L will be the outward facing normal for the faceBox
                if (penetrationAxisIndex < 3)
                {
                    vertexBox = box2;
                    if (L.dot(D) < 0)
                    {
                        faceNormal = -L;
                    }
                }
                else
                {
                    vertexBox = box1;
                    if (L.dot(D) > 0)
                    {
                        faceNormal = -L;
                    }
                }

                // find the vertex that is farthest away in the direction
                // face normal direction
                int deepestPointIndex = 0;
                float deepestPointDot = faceNormal.dot(vertexBox.getCorner(0));
                for (int i = 1; i < 8; i++)
                {
                    float dotProduct = faceNormal.dot(vertexBox.getCorner(i));
                    if (dotProduct < deepestPointDot)
                    {
                        deepestPointDot = dotProduct;
                        deepestPointIndex = i;
                    }
                }

                // return the point half way between the deepest point and the
                // contacting face
                contactPoint = vertexBox.getCorner(deepestPointIndex) +
                    (-penetration * 0.5f * faceNormal);
            }
            else
            {
                // edge-edge case, find the two ege lines
                int edge1 = (penetrationAxisIndex - 6) / 3;
                int edge2 = (penetrationAxisIndex - 6) % 3;
                Vector3 linePoint1 = box1.center();
                Vector3 linePoint2 = box2.center();
                Vector3 lineDir1 = Vector3.zero();
                Vector3 lineDir2 = Vector3.zero();

                // find edge line by finding the edge axis, and the
                // other two axes that are closest to the other box
                for (int i = 0; i < 3; i++)
                {
                    if (i == edge1)
                    {
                        lineDir1 = box1.axis(i);
                    }
                    else
                    {
                        Vector3 axis = box1.axis(i);
                        if (axis.dot(L) < 0)
                        {
                            axis = -axis;
                        }
                        linePoint1 += axis * a[i];
                    }

                    if (i == edge2)
                    {
                        lineDir2 = box2.axis(i);
                    }
                    else
                    {
                        Vector3 axis = box2.axis(i);
                        if (axis.dot(L) > 0)
                        {
                            axis = -axis;
                        }
                        linePoint2 += axis * b[i];
                    }
                }

                // make lines from the two closest edges, and find
                // the points that on each line that are closest to the other
                Line line1 = Line.fromPointAndDirection(linePoint1, lineDir1);
                Line line2 = Line.fromPointAndDirection(linePoint2, lineDir2);
                Vector3 closest1;
                Vector3 closest2;

                closestPointsBetweenLineAndLine(line1, line2, out closest1, out closest2);

                // take the average of the two closest edge points for the final
                // contact point
                contactPoint = (closest1 + closest2) * 0.5f;
            }

            contactPoints.push(contactPoint);
            contactNormals.push(L);

            return -penetration;
        }

        /**
         Calculates the depth of penetration between two fixed boxes as well as
         the contact point and normal.  The contact normals are not computed by
         this overload.
         */
        public static float penetrationDepthForFixedBoxFixedBox(
            Box box1,
            Box box2,
            Array<Vector3> contactPoints,
            int lastSeparatingAxis = -1)
        {
            Array<Vector3> contactNormals = new Array<Vector3>();
            return penetrationDepthForFixedBoxFixedBox(box1, box2, contactPoints, contactNormals, lastSeparatingAxis);
        }

        /**
         Calculates the depth of penetration between two fixed spheres as well
         as the deepest point of Sphere A that penetrates Sphere B. The normal
         returned points <B>away</B> from the object A, although it may
         represent a perpendicular to either the faces of object B or object A
         depending on their relative orientations.

         @param sphereA		Fixed Sphere A.
         @param sphereB		Fixed Sphere B.
         @param contactPoints	Sphere A's deepest point that penetrates Sphere B.
        						[Post Condition]
         @param contactNormals	Normal at penetration point. [Post Condition]

         @return Depth of penetration.  If there is no intersection between the
	 		 objects then the depth will be a negative value.
         */
        public static float penetrationDepthForFixedSphereFixedSphere(
            Sphere sphereA,
            Sphere sphereB,
            Array<Vector3> contactPoints,
            Array<Vector3> contactNormals)
        {
            Vector3 axis = sphereB.center - sphereA.center;
            double radius = sphereA.radius + sphereB.radius;
            double mag = axis.magnitude();
            axis = axis / (float)mag;
            double depth = -(mag - radius);

            contactPoints.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);
            contactNormals.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);

            if (depth >= 0)
            {
                contactPoints.append(sphereA.center + axis * (float)(sphereA.radius - depth / 2));
                contactNormals.append(axis);
            }

            return (float)depth;
        }

        /**
         Calculates the depth of penetration between two fixed spheres.  The
         contact normal is not computed by this overload.
         */
        public static float penetrationDepthForFixedSphereFixedSphere(
            Sphere sphereA,
            Sphere sphereB,
            Array<Vector3> contactPoints)
        {
            Array<Vector3> contactNormals = new Array<Vector3>();
            return penetrationDepthForFixedSphereFixedSphere(sphereA, sphereB, contactPoints, contactNormals);
        }

        /**
         Calculates the depth of penetration between a fixed sphere and a fixed
         box as well as the deepest point of the sphere that penetrates the box
         and the normal at that intersection.

         @note There are three possible intersections between a sphere and box.
         - Sphere completely contained in the box
         - Sphere intersects one edge
         - Sphere intersects one vertex

         The contact point and contact normal vary for each of these situations.
         - Sphere contained in Box:
         	- Normal is based on side of least penetration (as is the depth calculation).
         	- Point is based on center of sphere
         - Sphere intersects one edge
         	- Normal is based on vector from the box center to the point of depth.
         	- Point is closest point to the sphere on the line
         - Sphere intersects one vertex
         	- Normal is based on vector from the box center to the vertex of penetration.
         	- Point is vertex of penetration.

          @cite Adapted from Jim Arvo's method in Graphics Gems
          See also http://www.win.tue.nl/~gino/solid/gdc2001depth.pdf

         @param sphere			Fixed Sphere.
         @param box			Fixed Box.
         @param contactPoints	Sphere point that penetrates the box. [Post Condition]
         @param contactNormals	Normal at the penetration point. [Post Condition]

         @return Depth of penetration.  If there is no intersection between the
	 		 objects then the depth will be a negative value.
  	   */
        public static float penetrationDepthForFixedSphereFixedBox(
            Sphere sphere,
            Box box,
            Array<Vector3> contactPoints,
            Array<Vector3> contactNormals)
        {
            contactPoints.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);
            contactNormals.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);

            // In its local coordinate frame, the box measures
            // 2 * halfExtent[a] along dimesion a.
            Vector3 halfExtent = new Vector3(box.extent(0), box.extent(1), box.extent(2));
            halfExtent = halfExtent * 0.5f;

            CoordinateFrame boxFrame = CoordinateFrame.identity();
            box.getLocalFrame(ref boxFrame);

            // Transform the sphere to the box's coordinate frame.
            Vector3 center = boxFrame.pointToObjectSpace(sphere.center);

            // Find the square of the distance from the sphere to the box

            // Distance along each axis from the closest side of the box
            // to the sphere center.  Negative values are *inside* the box.
            Vector3 distOutsideBox = Vector3.zero();

            // Divide space up into the 27 regions corresponding
            // to {+|-|0}X, {+|-|0}Y, {+|-|0}Z and classify the
            // sphere center into one of them.
            Vector3 centerRegion = Vector3.zero();

            // In the edge collision case, the edge is between vertices
            // (constant + variable) and (constant - variable).
            Vector3 constant = Vector3.zero();
            Vector3 variable = Vector3.zero();

            int numNonZero = 0;

            // Iterate over axes
            for (int a = 0; a < 3; ++a)
            {
                // For each (box side), see which direction the sphere
                // is outside the box (positive or negative).  Add the
                // square of that distance to the total distance from
                // the box.

                float distanceFromLow = -halfExtent[a] - center[a];
                float distanceFromHigh = center[a] - halfExtent[a];

                if (Math.Abs(distanceFromLow) < Math.Abs(distanceFromHigh))
                {
                    distOutsideBox[a] = distanceFromLow;
                }
                else
                {
                    distOutsideBox[a] = distanceFromHigh;
                }

                if (distanceFromLow < 0.0)
                {
                    if (distanceFromHigh < 0.0)
                    {
                        // Inside the box
                        centerRegion[a] = 0.0f;
                        variable[a] = 1.0f;
                    }
                    else
                    {
                        // Off the high side
                        centerRegion[a] = 1.0f;
                        constant[a] = halfExtent[a];
                        ++numNonZero;
                    }
                }
                else if (distanceFromHigh < 0.0)
                {
                    // Off the low side
                    centerRegion[a] = -1.0f;
                    constant[a] = -halfExtent[a];
                    ++numNonZero;
                }
                else
                {
                    Debug.debugAssertM(false,
                        "distanceFromLow and distanceFromHigh cannot both be positive");
                }
            }

            // Squared distance between the outside of the box and the
            // sphere center.
            float d2 = Vector3.zero().max(distOutsideBox).squaredMagnitude();

            if (d2 > g3dmath.square(sphere.radius))
            {
                // There is no penetration because the distance is greater
                // than the radius of the sphere.  This is the common case
                // and we quickly exit.
                return -1;
            }

            // We know there is some penetration but need to classify it.
            //
            // Examine the region that contains the center of the sphere. If
            // there is exactly one non-zero axis, the collision is with a
            // plane.  If there are exactly two non-zero axes, the collision
            // is with an edge.  If all three axes are non-zero, the collision is
            // with a vertex.  If there are no non-zero axes, the center is inside
            // the box.

            double depth = -1;
            switch (numNonZero)
            {
                case 3: // Vertex collision
                    // The collision point is the vertex at constant, the normal
                    // is the vector from there to the sphere center.
                    contactNormals.append(boxFrame.normalToWorldSpace(constant - center));
                    contactPoints.append(boxFrame.pointToWorldSpace(constant));
                    depth = sphere.radius - Math.Sqrt(d2);
                    break;

                case 2: // Edge collision
                    {
                        // TODO: unwrapping the edge constructor and closest point
                        // code will probably make it faster.

                        // Determine the edge
                        Line line = Line.fromPointAndDirection(constant, variable);

                        // Penetration depth:
                        depth = sphere.radius - Math.Sqrt(d2);

                        // The contact point is the closes point to the sphere on the line
                        Vector3 X = line.closestPoint(center);
                        contactNormals.append(boxFrame.normalToWorldSpace(X - center).direction());
                        contactPoints.append(boxFrame.pointToWorldSpace(X));
                    }
                    break;

                case 1: // Plane collision
                    {
                        // The plane normal is the centerRegion vector,
                        // so the sphere normal is the negative.  Take
                        // it to world space from box-space.

                        // Center region doesn't need to be normalized because
                        // it is known to contain only one non-zero value
                        // and that value is +/- 1.
                        Vector3 N = boxFrame.normalToWorldSpace(-centerRegion);
                        contactNormals.append(N);

                        // Penetration depth:
                        depth = sphere.radius - (float)Math.Sqrt(d2);

                        // Compute the contact point from the penetration depth
                        contactPoints.append(sphere.center + N * (float)(sphere.radius - depth));
                    }
                    break;

                case 0: // Volume collision

                    // The sphere center is inside the box.  This is an easy case
                    // to handle.  Note that all axes of distOutsideBox must
                    // be negative.

                    // Arbitratily choose the sphere center as a contact point
                    contactPoints.append(sphere.center);

                    // Find the least-negative penetration axis.
                    //
                    // We could have computed this during the loop over the axes,
                    // but since volume collisions are rare (they only occur with
                    // large time steps), this case will seldom be executed and
                    // should not be optimized at the expense of the others.
                    if (distOutsideBox.x > distOutsideBox.y)
                    {
                        if (distOutsideBox.x > distOutsideBox.z)
                        {
                            // Smallest penetration on x-axis
                            // Chose normal based on which side we're closest to.
                            // Keep in mind that this is a normal to the sphere,
                            // so it is the inverse of the box normal.
                            if (center.x > 0)
                            {
                                contactNormals.append(boxFrame.normalToWorldSpace(-Vector3.unitX()));
                            }
                            else
                            {
                                contactNormals.append(boxFrame.normalToWorldSpace(Vector3.unitX()));
                            }
                            depth = -distOutsideBox.x;
                        }
                        else
                        {
                            // Smallest penetration on z-axis
                            // Chose normal based on which side we're closest to.
                            // Keep in mind that this is a normal to the sphere,
                            // so it is the inverse of the box normal.
                            if (center.z > 0)
                            {
                                contactNormals.append(boxFrame.normalToWorldSpace(-Vector3.unitZ()));
                            }
                            else
                            {
                                contactNormals.append(boxFrame.normalToWorldSpace(Vector3.unitZ()));
                            }
                            depth = -distOutsideBox.z;
                        }
                    }
                    else if (distOutsideBox.y > distOutsideBox.z)
                    {
                        // Smallest penetration on y-axis
                        // Chose normal based on which side we're closest to.
                        // Keep in mind that this is a normal to the sphere,
                        // so it is the inverse of the box normal.
                        if (center.y > 0)
                        {
                            contactNormals.append(boxFrame.normalToWorldSpace(-Vector3.unitY()));
                        }
                        else
                        {
                            contactNormals.append(boxFrame.normalToWorldSpace(Vector3.unitY()));
                        }
                        depth = -distOutsideBox.y;
                    }
                    else
                    {
                        // Smallest on z-axis
                        // Chose normal based on which side we're closest to.
                        // Keep in mind that this is a normal to the sphere,
                        // so it is the inverse of the box normal.
                        if (center.z > 0)
                        {
                            contactNormals.append(boxFrame.normalToWorldSpace(-Vector3.unitZ()));
                        }
                        else
                        {
                            contactNormals.append(boxFrame.normalToWorldSpace(Vector3.unitZ()));
                        }
                        depth = -distOutsideBox.z;
                    }
                    break;

                default:
                    Debug.debugAssertM(false, "Fell through switch");
                    break;
            }

            return (float)depth;
        }

        /**
         Calculates the depth of penetration between a fixed sphere and a fixed
         box.  The contact normal is not computed by this overload.
         */
        public static float penetrationDepthForFixedSphereFixedBox(
            Sphere sphere,
            Box box,
            Array<Vector3> contactPoints)
        {
            Array<Vector3> contactNormals = new Array<Vector3>();
            return penetrationDepthForFixedSphereFixedBox(sphere, box, contactPoints, contactNormals);
        }

        /**
         Calculates the depth of penetration between a Fixed Sphere and a Fixed
         Plane as well as the deepest point of the sphere that penetrates the plane
         and the plane normal at that intersection.

         @param sphere			Fixed Sphere.
         @param plane			Fixed Plane.
         @param contactPoints	Sphere point that penetrates the plane.
        						[Post Condition]
         @param contactNormals	Normal at penetration point. [Post Condition]

  	  @return Depth of penetration.  If there is no intersection between the
	 		 objects then the depth will be a negative value.
  	 */
        public static float penetrationDepthForFixedSphereFixedPlane(
            Sphere sphereA,
            Plane planeB,
            Array<Vector3> contactPoints,
            Array<Vector3> contactNormals)
        {
            Vector3 N;
            double d;

            planeB.getEquation(out N, out d);

            double depth = -(sphereA.center.dot(N) + d - sphereA.radius);

            contactPoints.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);
            contactNormals.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);

            if (depth >= 0)
            {
                contactPoints.append(N * (float)(depth - sphereA.radius) + sphereA.center);
                contactNormals.append(N);
            }

            return (float)depth;
        }

        /**
         Calculates the depth of penetration between a Fixed Sphere and a Fixed
         Plane.  The contact normal is not computed by this overload.
         */
        public static float penetrationDepthForFixedSphereFixedPlane(
            Sphere sphereA,
            Plane planeB,
            Array<Vector3> contactPoints)
        {
            Array<Vector3> contactNormals = new Array<Vector3>();
            return penetrationDepthForFixedSphereFixedPlane(sphereA, planeB, contactPoints, contactNormals);
        }

        /**
         Calculates the depth of penetration between a fixed box and a fixed
         plane as well as the vertexes of the box that penetrate the plane
         and the plane normals at those intersections.

         @param box			Fixed Box.
         @param plane		Fixed Plane.
         @param contactPoints	Box points that penetrate the plane.
        						[Post Condition]
         @param contactNormals	Normals at penetration points [Post Condition]

         @return Depth of penetration.  If there is no intersection between the
	 		 objects then the depth will be a negative value.
  	*/
        public static float penetrationDepthForFixedBoxFixedPlane(
            Box box,
            Plane plane,
            Array<Vector3> contactPoints,
            Array<Vector3> contactNormals)
        {
            Vector3 N;
            double d;

            plane.getEquation(out N, out d);

            contactPoints.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);
            contactNormals.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);

            float lowest = (float)g3dmath.inf();
            for (int i = 0; i < 8; ++i)
            {
                Vector3 vertex = box.corner(i);

                float x = vertex.dot(N) + (float)d;

                if (x <= 0)
                {
                    // All vertices below the plane should be contact points.
                    contactPoints.append(vertex);
                    contactNormals.append(-N);
                }

                lowest = Math.Min(lowest, x);
            }

            // Depth should be a positive number
            return -lowest;
        }

        /**
         Calculates the depth of penetration between a fixed box and a fixed
         plane.  The contact normals are not computed by this overload.
         */
        public static float penetrationDepthForFixedBoxFixedPlane(
            Box box,
            Plane plane,
            Array<Vector3> contactPoints)
        {
            Array<Vector3> contactNormals = new Array<Vector3>();
            return penetrationDepthForFixedBoxFixedPlane(box, plane, contactPoints, contactNormals);
        }

        /**
          Calculates time between the intersection of a moving point and a fixed
          plane.

          @note This is only a one sided collision test.   The side defined by
        	  the plane's surface normal is the only one tested.  For a two sided
        	  collision, call the function once for each side's surface normal.

         @param point		Moving point.
         @param velocity	Point's velocity.
         @param plane		Fixed plane.
         @param location	Location of collision. [Post Condition]
        	 					(Infinite vector on no collision)
         @param outNormal	Plane's surface normal. [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	          value will be inf().
        	*/
        public static float collisionTimeForMovingPointFixedPlane(
            Vector3 point,
            Vector3 velocity,
            Plane plane,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            // Solve for the time at which normal.dot(point + velocity) + d == 0.
            double d;
            Vector3 normal;
            plane.getEquation(out normal, out d);

            outNormal = Vector3.zero();

            float vdotN = velocity.dot(normal);
            float pdotN = point.dot(normal);

            if (g3dmath.fuzzyEq(pdotN + d, 0))
            {
                // The point is *in* the plane.
                outLocation = point;
                outNormal = normal;
                return 0;
            }

            if (vdotN >= 0)
            {
                // no collision will occur
                outLocation = Vector3.inf();
                return (float)g3dmath.inf();
            }

            float t = (float)(-(pdotN + d) / vdotN);
            if (t < 0)
            {
                outLocation = Vector3.inf();
                return (float)g3dmath.inf();
            }
            else
            {
                outLocation = point + velocity * t;
                outNormal = normal;
                return t;
            }
        }

        /**
          Calculates time between the intersection of a moving point and a fixed
          plane.  The plane's surface normal is not computed by this overload.
        	*/
        public static float collisionTimeForMovingPointFixedPlane(
            Vector3 point,
            Vector3 velocity,
            Plane plane,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingPointFixedPlane(point, velocity, plane, out outLocation, out outNormal);
        }

        /**
          Calculates time between the intersection of a moving point and a fixed
          triangle.

          @note This is only a one sided collision test.   The side defined by
        	  the triangle's surface normal is the only one tested.  For a two sided
        	  collision, call the function once for each side's surface normal.

         @param orig		Moving point.
         @param dir		Point's velocity.
    	  @param v0 		Triangle vertex 1.
    	  @param v1 		Triangle vertex 2.
    	  @param v2 		Triangle vertex 3
         @param location	Location of collision. [Post Condition]
        	 					(Infinite vector on no collision)

         @return Time til collision.  If there is no collision then the return
        	          value will be inf().
        	*/
        public static float collisionTimeForMovingPointFixedTriangle(
            Vector3 orig,
            Vector3 dir,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2)
        {
            return Ray.fromOriginAndDirection(orig, dir).intersectionTime(v0, v1, v2);
        }

        /**
          Calculates time between the intersection of a moving point and a fixed
          triangle.

          @note This is only a one sided collision test.   The side defined by
        	  the triangle's surface normal is the only one tested.  For a two sided
        	  collision, call the function once for each side's surface normal.

         @param orig		Moving point.
         @param dir		Point's velocity.
    	  @param v0 		Triangle vertex 1.
    	  @param v1 		Triangle vertex 2.
    	  @param v2 		Triangle vertex 3
         @param location	Location of collision. [Post Condition]
        	 					(Infinite vector on no collision)

         @return Time til collision.  If there is no collision then the return
        	          value will be inf().
        	*/
        public static float collisionTimeForMovingPointFixedTriangle(
            Vector3 orig,
            Vector3 dir,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            out Vector3 location)
        {
            location = Vector3.zero();
            float t = collisionTimeForMovingPointFixedTriangle(orig, dir, v0, v1, v2);
            if (t < g3dmath.inf())
            {
                location = orig + dir * t;
            }
            return t;
        }

        /**
          Calculates time between the intersection of a moving point and a fixed
          triangle.

          @note This is only a one sided collision test.   The side defined by
        	  the triangle's surface normal is the only one tested.  For a two sided
        	  collision, call the function once for each side's surface normal.

         @param orig		Moving point.
         @param dir		Point's velocity.
    	  @param tri		Fixed triangle.
         @param location	Location of collision. [Post Condition]
        	 					(Infinite vector on no collision)
         @param normal		Triangle's surface normal. [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	          value will be inf().
        	*/
        public static float collisionTimeForMovingPointFixedTriangle(
            Vector3 orig,
            Vector3 dir,
            Triangle tri,
            out Vector3 location,
            out Vector3 normal)
        {
            location = Vector3.zero();
            normal = Vector3.zero();

            float t = collisionTimeForMovingPointFixedTriangle(
                orig, dir, tri.vertex(0), tri.vertex(1), tri.vertex(2));

            if (t < g3dmath.inf())
            {
                location = orig + dir * t;
                normal = tri.normal();
            }
            return t;
        }

        /**
          Calculates time between the intersection of a moving point and a fixed
          triangle.  The triangle's surface normal is not computed by this
          overload.
        	*/
        public static float collisionTimeForMovingPointFixedTriangle(
            Vector3 orig,
            Vector3 dir,
            Triangle tri,
            out Vector3 location)
        {
            Vector3 normal;
            return collisionTimeForMovingPointFixedTriangle(orig, dir, tri, out location, out normal);
        }

        /**
          Calculates time between the intersection of a moving point and a fixed
          triangle.

          @note This is only a one sided collision test.   The side defined by
        	  the triangle's surface normal is the only one tested.  For a two sided
        	  collision, call the function once for each side's surface normal.

         @param orig		Moving point.
         @param dir		Point's velocity.
    	  @param v0 		Triangle vertex 1.
    	  @param v1 		Triangle vertex 2.
    	  @param v2 		Triangle vertex 3
         @param location	Location of collision. [Post Condition]
        	 					(Infinite vector on no collision)
         @param normal		Triangle's surface normal. [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	          value will be inf().
        	*/
        public static float collisionTimeForMovingPointFixedTriangle(
            Vector3 orig,
            Vector3 dir,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            out Vector3 location,
            out Vector3 normal)
        {
            location = Vector3.zero();
            normal = Vector3.zero();

            float t = collisionTimeForMovingPointFixedTriangle(orig, dir, v0, v1, v2);
            if (t < g3dmath.inf())
            {
                location = orig + dir * t;
                normal = (v2 - v0).cross(v1 - v0).direction();
            }
            return t;
        }

        /**
         Unlike other methods, does not support an output normal.
         If the ray origin is inside the box, returns inf() but inside
         is set to true.
         <B>Beta API</B>

         @cite Andrew Woo, from "Graphics Gems", Academic Press, 1990
          @cite Optimized code by Pierre Terdiman, 2000 (~20-30% faster on my Celeron 500)
         @cite Epsilon value added by Klaus Hartmann
         @cite http://www.codercorner.com/RayAABB.cpp
         */
        public static float collisionTimeForMovingPointFixedAABox(
            Vector3 point,
            Vector3 velocity,
            AABox box,
            out Vector3 outLocation,
            out bool inside,
            out Vector3 outNormal)
        {
            if (collisionLocationForMovingPointFixedAABox(point, velocity, box, out outLocation, out inside, out outNormal))
            {
                return (outLocation - point).magnitude();
            }
            else
            {
                return (float)g3dmath.inf();
            }
        }

        /**
         Unlike other methods, does not support an output normal.
         */
        public static float collisionTimeForMovingPointFixedAABox(
            Vector3 point,
            Vector3 velocity,
            AABox box,
            out Vector3 outLocation,
            out bool inside)
        {
            Vector3 outNormal;
            return collisionTimeForMovingPointFixedAABox(point, velocity, box, out outLocation, out inside, out outNormal);
        }

        /**
         Unlike other methods, does not support an output normal.
         */
        public static float collisionTimeForMovingPointFixedAABox(
            Vector3 point,
            Vector3 velocity,
            AABox box,
            out Vector3 outLocation)
        {
            bool inside;
            return collisionTimeForMovingPointFixedAABox(point, velocity, box, out outLocation, out inside);
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         Axis-Aligned Box (AABox).

         @note Avoids the sqrt from collisionTimeForMovingPointFixedAABox.

         @param point		Moving point.
         @param velocity	Sphere's velocity.
         @param box			Fixed AAbox.
         @param location	Location of collision. [Post Condition]
         @param Inside		Does the ray originate inside the box? [Post Condition]
         @param normal		Box's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static bool collisionLocationForMovingPointFixedAABox(
            Vector3 point,
            Vector3 velocity,
            AABox box,
            out Vector3 outLocation,
            out bool inside,
            out Vector3 normal)
        {
            inside = true;
            outLocation = Vector3.zero();
            normal = Vector3.zero();
            Vector3 MinB = box.low();
            Vector3 MaxB = box.high();
            Vector3 MaxT = new Vector3(-1.0f, -1.0f, -1.0f);

            // Find candidate planes.
            for (int i = 0; i < 3; ++i)
            {
                if (point[i] < MinB[i])
                {
                    outLocation[i] = MinB[i];
                    inside = false;

                    // Calculate T distances to candidate planes
                    if (hasNonZeroBitPattern(velocity[i]))
                    {
                        MaxT[i] = (MinB[i] - point[i]) / velocity[i];
                    }
                }
                else if (point[i] > MaxB[i])
                {
                    outLocation[i] = MaxB[i];
                    inside = false;

                    // Calculate T distances to candidate planes
                    if (hasNonZeroBitPattern(velocity[i]))
                    {
                        MaxT[i] = (MaxB[i] - point[i]) / velocity[i];
                    }
                }
            }

            if (inside)
            {
                // Ray origin inside bounding box
                return false;
            }

            // Get largest of the maxT's for final choice of intersection
            int WhichPlane = 0;
            if (MaxT[1] > MaxT[WhichPlane])
            {
                WhichPlane = 1;
            }

            if (MaxT[2] > MaxT[WhichPlane])
            {
                WhichPlane = 2;
            }

            // Check final candidate actually inside box
            if (hasNegativeSignBit(MaxT[WhichPlane]))
            {
                // Miss the box
                return false;
            }

            for (int i = 0; i < 3; ++i)
            {
                if (i != WhichPlane)
                {
                    outLocation[i] = point[i] + MaxT[WhichPlane] * velocity[i];
                    if ((outLocation[i] < MinB[i]) ||
                        (outLocation[i] > MaxB[i]))
                    {
                        // On this plane we're outside the box extents, so
                        // we miss the box
                        return false;
                    }
                }
            }

            // Choose the normal to be the plane normal facing into the ray
            normal = Vector3.zero();
            normal[WhichPlane] = (velocity[WhichPlane] > 0) ? -1.0f : 1.0f;

            return true;
        }

        /**
         Calculates the collision location of a moving point and a fixed
         Axis-Aligned Box (AABox).
         */
        public static bool collisionLocationForMovingPointFixedAABox(
            Vector3 point,
            Vector3 velocity,
            AABox box,
            out Vector3 outLocation,
            out bool inside)
        {
            Vector3 normal;
            return collisionLocationForMovingPointFixedAABox(point, velocity, box, out outLocation, out inside, out normal);
        }

        /**
         Calculates the collision location of a moving point and a fixed
         Axis-Aligned Box (AABox).
         */
        public static bool collisionLocationForMovingPointFixedAABox(
            Vector3 point,
            Vector3 velocity,
            AABox box,
            out Vector3 outLocation)
        {
            bool inside;
            return collisionLocationForMovingPointFixedAABox(point, velocity, box, out outLocation, out inside);
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         sphere.

         @note When ray is starts inside the rectangle, the exiting intersection
         is detected.

         @param point		Moving point.
         @param velocity	Point's velocity.
         @param Sphere		Fixed Sphere.
         @param location	Location of collision. [Post Condition]
         @param outNormal	Sphere's surface normal to collision [Post Condition]


         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingPointFixedSphere(
            Vector3 point,
            Vector3 velocity,
            Sphere sphere,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            double speed = velocity.magnitude();
            Vector3 direction = velocity / (float)speed;

            outNormal = Vector3.zero();

            Vector3 L = sphere.center - point;
            double d = L.dot(direction);

            double L2 = L.dot(L);
            double R2 = sphere.radius * sphere.radius;
            double D2 = d * d;

            if ((d < 0) && (L2 > R2))
            {
                outLocation = Vector3.inf();
                return (float)g3dmath.inf();
            }

            double M2 = L2 - D2;

            if (M2 > R2)
            {
                outLocation = Vector3.inf();
                return (float)g3dmath.inf();
            }

            double q = Math.Sqrt(R2 - M2);
            double time;

            if (L2 > R2)
            {
                time = d - q;
            }
            else
            {
                time = d + q;
            }

            time /= speed;

            outLocation = point + velocity * (float)time;
            outNormal = (outLocation - sphere.center).direction();

            return (float)time;
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         sphere.  The sphere's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingPointFixedSphere(
            Vector3 point,
            Vector3 velocity,
            Sphere sphere,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingPointFixedSphere(point, velocity, sphere, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         box.

         @note If the point is already inside the box, no collision: inf is returned.

         @param point		Moving point.
         @param velocity	Sphere's velocity.
         @param box			Fixed box.
         @param location	Position of collision. [Post Condition]
         @param outNormal	Box's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingPointFixedBox(
            Vector3 point,
            Vector3 velocity,
            Box box,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            double bestTime;

            Vector3 normal;
            Vector3 v0, v1, v2, v3;

            // Prime the loop
            int f = 0;
            box.getFaceCorners(f, out v0, out v1, out v2, out v3);
            bestTime = collisionTimeForMovingPointFixedRectangle(point, velocity, v0, v1, v2, v3, out outLocation, out normal);
            outNormal = normal;

            // Check other faces
            for (f = 1; f < 6; ++f)
            {
                Vector3 pos;
                box.getFaceCorners(f, out v0, out v1, out v2, out v3);
                float time = collisionTimeForMovingPointFixedRectangle(point, velocity, v0, v1, v2, v3, out pos, out normal);
                if (time < bestTime)
                {
                    bestTime = time;
                    outNormal = normal;
                    outLocation = pos;
                }
            }

            return (float)bestTime;
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         box.  The box's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingPointFixedBox(
            Vector3 point,
            Vector3 velocity,
            Box box,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingPointFixedBox(point, velocity, box, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         rectangle defined by the points v0, v1, v2, & v3.

         @note This is only a one sided collision test.   The side defined by
         the rectangle's surface normal is the only one tested.  For a two sided
         collision, call the function once for each side's surface normal.

         @param point		Moving point.
         @param velocity	Sphere's velocity.
         @param v0 			Rectangle vertex 1.
         @param v1 			Rectangle vertex 2.
         @param v2 			Rectangle vertex 3
         @param v3 			Rectangle vertex 4.
         @param location	Location of collision [Post Condition]
         @param outNormal	Rectangle's surface normal. [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingPointFixedRectangle(
            Vector3 point,
            Vector3 velocity,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            Plane plane = new Plane(v0, v1, v2);

            float time = collisionTimeForMovingPointFixedPlane(point, velocity, plane, out outLocation, out outNormal);

            if (time == g3dmath.inf())
            {
                // No collision is ever going to happen
                return time;
            }

            if (isPointInsideRectangle(v0, v1, v2, v3, plane.normal(), outLocation))
            {
                // The intersection point is inside the rectangle; that is the location where
                // the point hits the rectangle.
                return time;
            }
            else
            {
                return (float)g3dmath.inf();
            }
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         rectangle.  The rectangle's surface normal is not computed by this
         overload.
         */
        public static float collisionTimeForMovingPointFixedRectangle(
            Vector3 point,
            Vector3 velocity,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingPointFixedRectangle(point, velocity, v0, v1, v2, v3, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         capsule.

         @param point		Moving point.
         @param velocity	Point's velocity.
         @param capsule		Fixed capsule.
         @param location	Location of collision. [Post Condition]
         @param outNormal	Capsule's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingPointFixedCapsule(
            Vector3 point,
            Vector3 velocity,
            Capsule capsule,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            float timeScale = velocity.magnitude();

            if (timeScale == 0.0f)
            {
                timeScale = 1;
            }

            Vector3 direction = velocity / timeScale;
            int numIntersections;
            Vector3[] intersection = new Vector3[2];
            findRayCapsuleIntersection(Ray.fromOriginAndDirection(point, direction), capsule, out numIntersections, intersection);

            outNormal = Vector3.zero();

            if (numIntersections == 2)
            {
                // A collision can only occur if there are two intersections.  If there is one
                // intersection, that one is exiting the capsule.

                // Find the entering intersection (the first one that occurs).
                float d0 = (intersection[0] - point).squaredMagnitude();
                float d1 = (intersection[1] - point).squaredMagnitude();

                // Compute the surface normal
                Vector3 p2 = LineSegment.fromTwoPoints(capsule.getPoint1(), capsule.getPoint2()).closestPoint(point);
                outNormal = (point - p2).direction();

                if (d0 > d1)
                {
                    outLocation = intersection[1];
                    return (float)(Math.Sqrt(d1) / timeScale);
                }
                else
                {
                    outLocation = intersection[0];
                    return (float)(Math.Sqrt(d0) / timeScale);
                }
            }
            else
            {
                // No entering intersection discovered; return no intersection.
                outLocation = Vector3.inf();
                return (float)g3dmath.inf();
            }
        }

        /**
         Calculates time between the intersection of a moving point and a fixed
         capsule.  The capsule's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingPointFixedCapsule(
            Vector3 point,
            Vector3 velocity,
            Capsule capsule,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingPointFixedCapsule(point, velocity, capsule, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         triangle.

         @param sphere		Moving sphere.
         @param velocity	Sphere's velocity.
         @param plane		Fixed Plane.
         @param location	Location of collision -- not center position of sphere
         					at the collision time. [Post Condition]
         @param outNormal	Box's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
         */
        public static float collisionTimeForMovingSphereFixedPlane(
            Sphere sphere,
            Vector3 velocity,
            Plane plane,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            if (sphere.radius == 0)
            {
                // Optimization for zero radius sphere
                return collisionTimeForMovingPointFixedPlane(sphere.center, velocity, plane, out outLocation, out outNormal);
            }

            // The collision point on the sphere will be the point at
            // center - (radius * normal).  Collisions only occur when
            // the sphere is travelling into the plane.

            double d;
            plane.getEquation(out outNormal, out d);

            double vdotN = velocity.dot(outNormal);

            if (g3dmath.fuzzyGt(vdotN, 0))
            {
                // No collision when the sphere is moving towards a backface.
                outLocation = Vector3.inf();
                return (float)g3dmath.inf();
            }

            float cdotN = sphere.center.dot(outNormal);

            // Distance from the center to the plane
            float distance = cdotN + (float)d;

            // Where is the collision on the sphere?
            Vector3 point = sphere.center - (sphere.radius * outNormal);

            if (g3dmath.fuzzyLe(Math.Abs(distance), sphere.radius))
            {
                // Already interpenetrating
                outLocation = sphere.center - distance * outNormal;
                return 0;
            }
            else
            {
                return collisionTimeForMovingPointFixedPlane(point, velocity, plane, out outLocation, out outNormal);
            }
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         plane.  The plane's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingSphereFixedPlane(
            Sphere sphere,
            Vector3 velocity,
            Plane plane,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingSphereFixedPlane(sphere, velocity, plane, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         triangle.

         @param sphere		Moving sphere.
         @param velocity	Sphere's velocity.
         @param triangle	Fixed Triangle.
         @param location	Location of collision -- not center position of sphere
         					at the collision time. [Post Condition]
         @param outNormal	Box's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
        	*/
        public static float collisionTimeForMovingSphereFixedTriangle(
            Sphere sphere,
            Vector3 velocity,
            Triangle triangle,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            Vector3 dummy;

            outNormal = triangle.normal();
            float time = collisionTimeForMovingSphereFixedPlane(sphere, velocity, triangle.plane(), out outLocation, out dummy);

            if (time == g3dmath.inf())
            {
                // No collision is ever going to happen
                return time;
            }

            if (isPointInsideTriangle(triangle.vertex(0), triangle.vertex(1), triangle.vertex(2), triangle.normal(), outLocation, triangle.primaryAxis()))
            {
                // The intersection point is inside the triangle; that is the location where
                // the sphere hits the triangle.
                return time;
            }

            // Switch over to moving the triangle towards a fixed sphere and see at what time
            // they will hit.

            // Closest point on the triangle to the sphere intersection with the plane.
            // Use stack-allocated local variables instead of heap allocations
            Vector3 v0 = triangle.vertex(0);
            Vector3 v1 = triangle.vertex(1);
            Vector3 v2 = triangle.vertex(2);
            Vector3 e0 = triangle.edgeDirection(0);
            Vector3 e1 = triangle.edgeDirection(1);
            Vector3 e2 = triangle.edgeDirection(2);
            double m0 = triangle.edgeMagnitude(0);
            double m1 = triangle.edgeMagnitude(1);
            double m2 = triangle.edgeMagnitude(2);

            // Closest point on each segment
            Vector3 r0 = closestPointOnLineSegment(v0, v1, e0, (float)m0, outLocation);
            Vector3 r1 = closestPointOnLineSegment(v1, v2, e1, (float)m1, outLocation);
            Vector3 r2 = closestPointOnLineSegment(v2, v0, e2, (float)m2, outLocation);

            // Distance squared
            double d0 = (r0 - outLocation).squaredMagnitude();
            double d1 = (r1 - outLocation).squaredMagnitude();
            double d2 = (r2 - outLocation).squaredMagnitude();

            Vector3 point;
            if (d0 < d1)
            {
                if (d0 < d2)
                {
                    point = r0;
                }
                else
                {
                    point = r2;
                }
            }
            else
            {
                if (d1 < d2)
                {
                    point = r1;
                }
                else
                {
                    point = r2;
                }
            }

            double t = collisionTimeForMovingPointFixedSphere(point, -velocity, sphere, out dummy, out dummy);

            // The collision occured at the point, if it occured.  The normal was the plane normal,
            // computed above.

            return (float)t;
        }

        /// <summary>
        /// Optimized overload that accepts pre-computed triangle data to avoid heap allocations.
        /// Call this from performance-critical paths with pre-computed data.
        /// </summary>
        public static float collisionTimeForMovingSphereFixedTriangle(
            Sphere sphere,
            Vector3 velocity,
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 normal,
            Vector3 edgeDir0, Vector3 edgeDir1, Vector3 edgeDir2,
            double edgeMag0, double edgeMag1, double edgeMag2,
            Vector3.Axis primaryAxis,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            Vector3 dummy;

            outNormal = normal;
            Plane plane = new Plane(v0, v1, v2);
            float time = collisionTimeForMovingSphereFixedPlane(sphere, velocity, plane, out outLocation, out dummy);

            if (time == g3dmath.inf())
            {
                return time;
            }

            if (isPointInsideTriangle(v0, v1, v2, normal, outLocation, primaryAxis))
            {
                return time;
            }

            // Closest point on each segment - inline from closestPointToTrianglePerimeter
            Vector3 r0 = closestPointOnLineSegment(v0, v1, edgeDir0, (float)edgeMag0, outLocation);
            Vector3 r1 = closestPointOnLineSegment(v1, v2, edgeDir1, (float)edgeMag1, outLocation);
            Vector3 r2 = closestPointOnLineSegment(v2, v0, edgeDir2, (float)edgeMag2, outLocation);

            // Distance squared
            double d0 = (r0 - outLocation).squaredMagnitude();
            double d1 = (r1 - outLocation).squaredMagnitude();
            double d2 = (r2 - outLocation).squaredMagnitude();

            Vector3 point;
            if (d0 < d1)
            {
                if (d0 < d2)
                {
                    point = r0;
                }
                else
                {
                    point = r2;
                }
            }
            else
            {
                if (d1 < d2)
                {
                    point = r1;
                }
                else
                {
                    point = r2;
                }
            }

            double t = collisionTimeForMovingPointFixedSphere(point, -velocity, sphere, out dummy, out dummy);

            return (float)t;
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         triangle.  The triangle's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingSphereFixedTriangle(
            Sphere sphere,
            Vector3 velocity,
            Triangle triangle,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingSphereFixedTriangle(sphere, velocity, triangle, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         rectangle defined by the points v0, v1, v2, & v3.

         @param sphere		Moving sphere.
         @param velocity	Sphere's velocity.
         @param v0 			Rectangle vertex 1.
         @param v1 			Rectangle vertex 2.
         @param v2 			Rectangle vertex 3
         @param v3 			Rectangle vertex 4.
         @param location	Location of collision -- not center position of sphere
         					at the collision time. [Post Condition]
         @param outNormal	Box's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingSphereFixedRectangle(
            Sphere sphere,
            Vector3 velocity,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            Plane plane = new Plane(v0, v1, v2);

            float time = collisionTimeForMovingSphereFixedPlane(sphere, velocity, plane, out outLocation, out outNormal);

            if (time == g3dmath.inf())
            {
                // No collision is ever going to happen
                return time;
            }

            if (isPointInsideRectangle(v0, v1, v2, v3, plane.normal(), outLocation))
            {
                // The intersection point is inside the rectangle; that is the location where
                // the sphere hits the rectangle.
                return time;
            }

            // Switch over to moving the rectangle towards a fixed sphere and see at what time
            // they will hit.

            Vector3 point = closestPointToRectanglePerimeter(v0, v1, v2, v3, sphere.center);

            Vector3 dummy;
            double t = collisionTimeForMovingPointFixedSphere(point, -velocity, sphere, out outLocation, out dummy);

            // Normal is the plane normal, location is the original location of the point.
            outLocation = point;

            return (float)t;
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         rectangle.  The rectangle's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingSphereFixedRectangle(
            Sphere sphere,
            Vector3 velocity,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingSphereFixedRectangle(sphere, velocity, v0, v1, v2, v3, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         box.

         @note This function will not detect an intersection between a moving object
         that is already interpenetrating the fixed object.

         @param sphere		Moving sphere.
         @param velocity	Sphere's velocity.
         @param box			Fixed box.
         @param location	Location of collision -- not center position of sphere
         					at the collision time. [Post Condition]
         @param outNormal	Box's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingSphereFixedBox(
            Sphere sphere,
            Vector3 velocity,
            Box box,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            // TODO: test if sphere is already inside box

            double bestTime;

            Vector3 v0, v1, v2, v3;
            int f = 0;
            box.getFaceCorners(f, out v0, out v1, out v2, out v3);
            bestTime = collisionTimeForMovingSphereFixedRectangle(sphere, velocity, v0, v1, v2, v3, out outLocation, out outNormal);

            for (f = 1; f < 6; ++f)
            {
                Vector3 pos, normal;
                box.getFaceCorners(f, out v0, out v1, out v2, out v3);
                float time = collisionTimeForMovingSphereFixedRectangle(sphere, velocity, v0, v1, v2, v3, out pos, out normal);
                if (time < bestTime)
                {
                    bestTime = time;
                    outLocation = pos;
                    outNormal = normal;
                }
            }

            return (float)bestTime;
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         box.  The box's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingSphereFixedBox(
            Sphere sphere,
            Vector3 velocity,
            Box box,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingSphereFixedBox(sphere, velocity, box, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         sphere.

         @note This won't detect a collision if the sphere is already interpenetrating
        	       the fixed sphere.

         @param movingSphere	Moving sphere.
         @param velocity		Sphere's velocity.
         @param fixedSphere		Fixed Sphere.
         @param location		Location of collision -- not center position of sphere
        	 						at the collision time. [Post Condition]
         @param outNormal		Sphere's surface normal to collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingSphereFixedSphere(
            Sphere sphere,
            Vector3 velocity,
            Sphere fixedSphere,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            Sphere bigFixed = new Sphere(fixedSphere.center, fixedSphere.radius + sphere.radius);

            double time = collisionTimeForMovingPointFixedSphere(sphere.center, velocity, bigFixed, out outLocation, out outNormal);

            if (time < g3dmath.inf())
            {
                // Location is now the center of the moving sphere at the collision time.
                // Adjust for the size of the moving sphere.  Two spheres always collide
                // along a line between their centers.
                outLocation += (outLocation - fixedSphere.center) * sphere.radius / fixedSphere.radius;
            }

            return (float)time;
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         sphere.  The sphere's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingSphereFixedSphere(
            Sphere sphere,
            Vector3 velocity,
            Sphere fixedSphere,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingSphereFixedSphere(sphere, velocity, fixedSphere, out outLocation, out outNormal);
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         capsule.

         @note This won't detect a collision if the sphere is already
        	       interpenetrating the capsule.

         @param sphere		Moving sphere.
         @param velocity	Sphere's velocity.
         @param capsule		Fixed capsule.
         @param location	Location of collision -- not center position of sphere
         					at the collision time. [Post Condition]
         @param outNormal	Capsule's surface normal to the collision [Post Condition]

         @return Time til collision.  If there is no collision then the return
        	        value will be inf().
  	*/
        public static float collisionTimeForMovingSphereFixedCapsule(
            Sphere sphere,
            Vector3 velocity,
            Capsule capsule,
            out Vector3 outLocation,
            out Vector3 outNormal)
        {
            Capsule _capsule = new Capsule(capsule.getPoint1(), capsule.getPoint2(), capsule.getRadius() + sphere.radius);

            Vector3 normal;
            outNormal = Vector3.zero();
            double time = collisionTimeForMovingPointFixedCapsule(sphere.center, velocity, _capsule, out outLocation, out normal);

            if (time < g3dmath.inf())
            {
                // Location is now the position of the center of the sphere at the time of collision.
                // We have to adjust the collision location for the size of the sphere.
                outLocation -= sphere.radius * normal;
            }

            return (float)time;
        }

        /**
         Calculates time between the intersection of a moving sphere and a fixed
         capsule.  The capsule's surface normal is not computed by this overload.
         */
        public static float collisionTimeForMovingSphereFixedCapsule(
            Sphere sphere,
            Vector3 velocity,
            Capsule capsule,
            out Vector3 outLocation)
        {
            Vector3 outNormal;
            return collisionTimeForMovingSphereFixedCapsule(sphere, velocity, capsule, out outLocation, out outNormal);
        }

        /**
         Finds the direction of bounce that a sphere would have when it
         intersects an object with the  given time of collision, the
         collision location and the collision normal.

         @note This function works like a pong style ball bounce.

         @param sphere				Moving sphere.
         @param velocity			Sphere's velocity.
         @param collisionTime		Time of collision.
         @param collisionLocation	Collision location.
         @param collisionNormal		Surface collision normal.

         @return Direction of bounce.
  	*/
        public static Vector3 bounceDirection(
            Sphere sphere,
            Vector3 velocity,
            float collisionTime,
            Vector3 collisionLocation,
            Vector3 collisionNormal)
        {
            // Location when the collision occurs
            Vector3 sphereLocation = sphere.center + velocity * collisionTime;

            Vector3 normal = (sphereLocation - collisionLocation);
            if (g3dmath.fuzzyEq(normal.squaredMagnitude(), 0))
            {
                normal = collisionNormal;
            }
            else
            {
                normal.unitize(1e-06f);
            }

            Vector3 direction = velocity.direction();

            // Reflect direction about the normal
            return direction - 2.0f * normal * normal.dot(direction);
        }

        /**
         Finds the direction of slide given a moving sphere, its velocity, the
         time of collision and the collision location.  This function works as
         if the sphere intersects the surface and continues to hug it.

         @note The result will work well for calculating the movement of a player
         who collides with an object and continues moving along the object instead
         of just bouncing off it.

         @param sphere				Moving sphere.
         @param velocity			Sphere's velocity.
         @param collisionTime		Time of collision
         @param collisionLocation	Collision location.

         @return Direction of slide.
  	*/
        public static Vector3 slideDirection(
            Sphere sphere,
            Vector3 velocity,
            float collisionTime,
            Vector3 collisionLocation)
        {
            Vector3 sphereLocation = sphere.center + velocity * collisionTime;
            Vector3 normal = (sphereLocation - collisionLocation).direction();
            Vector3 direction = velocity.direction();

            // subtract off the part in the direction away from the normal.
            return direction - normal * normal.dot(direction);
        }

        /**
         Finds the closest point on a line segment to a given point.

         @param v0 line vertex 1.
         @param v1 line vertex 2.
         @param point External point.

         @return Closests point to <code>point</code> on the line segment.
  	*/
        public static Vector3 closestPointOnLineSegment(
            Vector3 v0,
            Vector3 v1,
            Vector3 point)
        {
            Vector3 edge = (v1 - v0);
            double edgeLength = edge.magnitude();

            return closestPointOnLineSegment(v0, v1, edge / (float)edgeLength, (float)edgeLength, point);
        }

        /**
         Finds the closest point on a line segment to a given point.

         @note This is an optimization to closestPointOnLineSegment.  Edge length
         and direction can be used in this function if already pre-calculated.  This
         prevents doing the same work twice.

         @param v0 line vertex 1.
         @param v1 line vertex 2.
         @param edgeDirection The direction of the segment (unit length).
         @param edgeLength The length of the segment.
         @param point External point.

         @return Closests point to <code>point</code> on the line segment.
  	*/
        public static Vector3 closestPointOnLineSegment(
            Vector3 v0,
            Vector3 v1,
            Vector3 edgeDirection,
            float edgeLength,
            Vector3 point)
        {
            Debug.debugAssert((v1 - v0).direction().fuzzyEq(edgeDirection));
            Debug.debugAssert(g3dmath.fuzzyEq((v1 - v0).magnitude(), edgeLength));

            Vector3 c = point - v0;
            float t = edgeDirection.dot(c);

            if (t < 0)
            {
                return v0;
            }
            else if (t > edgeLength)
            {
                return v1;
            }
            else
            {
                return v0 + edgeDirection * t;
            }
        }

        /**
         Finds the closest point on the perimeter of the triangle to an external point;
         given a triangle defined by three points v0, v1, & v2, and the external point.

         @param v0 Triangle vertex 1.
         @param v1 Triangle vertex 2.
         @param v2 Triangle vertex 3.
         @param point External point.

         @return Closests point to <code>point</code> on the perimeter of the
         triangle.
  	*/
        public static Vector3 closestPointToTrianglePerimeter(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 point)
        {
            Vector3[] v = new Vector3[] { v0, v1, v2 };
            Vector3[] edgeDirection = new Vector3[] { (v1 - v0), (v2 - v1), (v0 - v2) };
            double[] edgeLength = new double[3];

            for (int i = 0; i < 3; ++i)
            {
                edgeLength[i] = edgeDirection[i].magnitude();
                edgeDirection[i] = edgeDirection[i] / (float)edgeLength[i];
            }

            return closestPointToTrianglePerimeter(v, edgeDirection, edgeLength, point);
        }

        /**
         Finds the closest point on the perimeter of the triangle to an external point;
         given a triangle defined by the array of points v, its edge directions and
         their lengths, as well as the external point.

         @note This is an optimization to closestPointToTrianglePerimeter.  Edge length
         and direction can be used in this function if already pre-calculated.  This
         prevents doing the same work twice.

         @param v Triangle vertices.
         @param edgeDirection Triangle edge directions.
         @param edgeLength Triangle edge lengths.
         @param point External point.

         @return Closests point to <code>point</code> on the perimeter of the
         triangle.
  	*/
        public static Vector3 closestPointToTrianglePerimeter(
            Vector3[] v,
            Vector3[] edgeDirection,
            double[] edgeLength,
            Vector3 point)
        {
            // Closest point on each segment
            Vector3[] r = new Vector3[3];

            // Distance squared
            double[] d = new double[3];

            // Index of the next point
            int[] next = new int[] { 1, 2, 0 };

            for (int i = 0; i < 3; ++i)
            {
                r[i] = closestPointOnLineSegment(v[i], v[next[i]], edgeDirection[i], (float)edgeLength[i], point);
                d[i] = (r[i] - point).squaredMagnitude();
            }

            if (d[0] < d[1])
            {
                if (d[0] < d[2])
                {
                    return r[0];
                }
                else
                {
                    return r[2];
                }
            }
            else
            {
                if (d[1] < d[2])
                {
                    return r[1];
                }
                else
                {
                    return r[2];
                }
            }
        }

        /**
         Tests whether a point is contained within the triangle defined by
         v0, v1, & v2 and its plane's normal.

         @param v0 Triangle vertex 1.
         @param v1 Triangle vertex 2.
         @param v2 Triangle vertex 3.
         @param normal Normal to triangle's plane.
         @param point The point in question.
         @param primaryAxis Primary axis of triangle.  This will be detected
        	        if not given. This parameter is provided as an optimization.

         @return true  - if point is inside the triangle.
         @return false - otherwise
  	*/
        public static bool isPointInsideTriangle(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 normal,
            Vector3 point,
            Vector3.Axis primaryAxis = Vector3.Axis.DETECT_AXIS)
        {
            if (primaryAxis == Vector3.Axis.DETECT_AXIS)
            {
                primaryAxis = normal.primaryAxis();
            }

            // Check that the point is within the triangle using a Barycentric
            // coordinate test on a two dimensional plane.
            int i, j;

            switch (primaryAxis)
            {
                case Vector3.Axis.X_AXIS:
                    i = 2;
                    j = 1;
                    break;

                case Vector3.Axis.Y_AXIS:
                    i = 2;
                    j = 0;
                    break;

                case Vector3.Axis.Z_AXIS:
                    i = 0;
                    j = 1;
                    break;

                default:
                    // This case is here to supress a warning on Linux
                    i = j = 0;
                    Debug.debugAssertM(false, "Should not get here.");
                    break;
            }

            // Area of the polygon
            double area = area2(v0, v1, v2, i, j);
            if (area == 0)
            {
                return (v0 == point);
            }

            Debug.debugAssert(area != 0);

            double a = area2(point, v1, v2, i, j) / area;

            if (a < 0)
            {
                return false;
            }

            double b = area2(v0, point, v2, i, j) / area;

            if ((b < 0) || ((1 - (a + b)) < 0))
            {
                return false;
            }

            return true;
        }

        /**
          Tests for the intersection of a moving sphere and a fixed box in a
          given time limit.

          @note Returns true if any part of the sphere is inside the box
          		during the time period (inf means "ever").  Useful for
    		    performing bounding-box collision detection.

         @param sphere 		Moving sphere.
         @param velocity 	Velocity of moving sphere.
         @param box 		Fixed box.
         @param timeLimit 	Time limit for intersection test.

         @return true  -  if the two objects will touch.
         @return false - if there is no intersection.
  	*/
        public static bool movingSpherePassesThroughFixedBox(
            Sphere sphere,
            Vector3 velocity,
            Box box)
        {
            return movingSpherePassesThroughFixedBox(sphere, velocity, box, g3dmath.inf());
        }

        /**
          Tests for the intersection of a moving sphere and a fixed box in a
          given time limit.

         @param sphere 		Moving sphere.
         @param velocity 	Velocity of moving sphere.
         @param box 		Fixed box.
         @param timeLimit 	Time limit for intersection test.

         @return true  -  if the two objects will touch.
         @return false - if there is no intersection.
  	*/
        public static bool movingSpherePassesThroughFixedBox(
            Sphere sphere,
            Vector3 velocity,
            Box box,
            double timeLimit)
        {
            // If they intersect originally, they definitely pass through each other.
            if (fixedSolidSphereIntersectsFixedSolidBox(sphere, box))
            {
                return true;
            }

            // See if the sphere hits the box during the time period.
            Vector3 dummy1, dummy2;

            return (collisionTimeForMovingSphereFixedBox(sphere, velocity, box, out dummy1, out dummy2) < timeLimit);
        }

        /**
         Tests for the intersection of a moving sphere and a fixed sphere in a
         given time limit.

         @note This function will not detect an intersection between a moving object
         that is already interpenetrating the fixed object.

         @param sphere 		Moving sphere.
         @param velocity 	Velocity of moving sphere.
         @param fixedSphere Fixed sphere.
         @param timeLimit 	Time limit for intersection test.

         @return true  -  if the two spheres will touch.
         @return false - if there is no intersection.
  	*/
        public static bool movingSpherePassesThroughFixedSphere(
            Sphere sphere,
            Vector3 velocity,
            Sphere fixedSphere)
        {
            return movingSpherePassesThroughFixedSphere(sphere, velocity, fixedSphere, g3dmath.inf());
        }

        /**
         Tests for the intersection of a moving sphere and a fixed sphere in a
         given time limit.

         @param sphere 		Moving sphere.
         @param velocity 	Velocity of moving sphere.
         @param fixedSphere Fixed sphere.
         @param timeLimit 	Time limit for intersection test.

         @return true  -  if the two spheres will touch.
         @return false - if there is no intersection.
  	*/
        public static bool movingSpherePassesThroughFixedSphere(
            Sphere sphere,
            Vector3 velocity,
            Sphere fixedSphere,
            double timeLimit)
        {
            if (fixedSolidSphereIntersectsFixedSolidSphere(sphere, fixedSphere))
            {
                return true;
            }

            // Extend the fixed sphere by the radius of the moving sphere
            Sphere bigFixed = new Sphere(fixedSphere.center, fixedSphere.radius + sphere.radius);
            Vector3 dummy1, dummy2;

            // If the sphere collides with the other sphere during the time limit, it passes through
            return (collisionTimeForMovingPointFixedSphere(sphere.center, velocity, bigFixed, out dummy1, out dummy2) < timeLimit);
        }

        /**
         Tests for the intersection of two fixed spheres.

         @param sphere1 Fixed sphere 1.
         @param sphere2 Fixed sphere 2.

         @return true -  if the two spheres touch.
         @return false - if there is no intersection.
  	*/
        public static bool fixedSolidSphereIntersectsFixedSolidSphere(
            Sphere sphere1,
            Sphere sphere2)
        {
            return (sphere1.center - sphere2.center).squaredMagnitude() < g3dmath.square(sphere1.radius + sphere2.radius);
        }

        /**
         Tests for the intersection of a fixed sphere and a fixed box.

         @param sphere Fixed sphere.
         @param box    Fixed box.

         @return true -  if the two objects touch.
         @return false - if there is no intersection.
  	*/
        public static bool fixedSolidSphereIntersectsFixedSolidBox(
            Sphere sphere,
            Box box)
        {
            // If the center of the sphere is within the box, the whole
            // sphere is within the box.
            if (box.contains(sphere.center))
            {
                return true;
            }

            double r2 = g3dmath.square(sphere.radius);

            // Find the closest point on the surface of the box to the sphere.  If
            // this point is within the sphere's radius, they intersect.
            for (int f = 0; f < 6; ++f)
            {
                Vector3 v0, v1, v2, v3;
                box.getFaceCorners(f, out v0, out v1, out v2, out v3);
                if ((closestPointToRectangle(v0, v1, v2, v3, sphere.center) - sphere.center).squaredMagnitude() <= r2)
                {
                    return true;
                }
            }

            return false;
        }

        /**
         Tests whether a point is inside a rectangle defined by the vertexes
         v0, v1, v2, & v3, and the rectangle's plane normal.

         @param v0 Rectangle vertex 1.
         @param v1 Rectangle vertex 2.
         @param v2 Rectangle vertex 3.
         @param v3 Rectangle vertex 4.
         @param normal Normal to rectangle's plane.
         @param point The point in question.

         @return true  - if point is inside the rectangle.
         @return false - otherwise
  	*/
        public static bool isPointInsideRectangle(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 normal,
            Vector3 point)
        {
            return isPointInsideTriangle(v0, v1, v2, normal, point) ||
                   isPointInsideTriangle(v2, v3, v0, normal, point);
        }

        /**
         Finds the closest point on the perimeter of the rectangle to an
         external point; given a rectangle defined by four points v0, v1,
         v2, & v3, and the external point.

         @param v0 Rectangle vertex 1.
         @param v1 Rectangle vertex 2.
         @param v2 Rectangle vertex 3.
         @param v3 Rectangle vertex 4.
         @param point External point.

         @return Closests point to <code>point</code> on the perimeter of the
  	   rectangle.
  	 */
        public static Vector3 closestPointToRectanglePerimeter(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 point)
        {
            Vector3 r0 = closestPointOnLineSegment(v0, v1, point);
            Vector3 r1 = closestPointOnLineSegment(v1, v2, point);
            Vector3 r2 = closestPointOnLineSegment(v2, v3, point);
            Vector3 r3 = closestPointOnLineSegment(v3, v0, point);

            double d0 = (r0 - point).squaredMagnitude();
            double d1 = (r1 - point).squaredMagnitude();
            double d2 = (r2 - point).squaredMagnitude();
            double d3 = (r3 - point).squaredMagnitude();

            if (d0 < d1)
            {
                if (d0 < d2)
                {
                    if (d0 < d3)
                    {
                        return r0;
                    }
                    else
                    {
                        return r3;
                    }
                }
                else
                {
                    if (d2 < d3)
                    {
                        return r2;
                    }
                    else
                    {
                        return r3;
                    }
                }
            }
            else
            {
                if (d1 < d2)
                {
                    if (d1 < d3)
                    {
                        return r1;
                    }
                    else
                    {
                        return r3;
                    }
                }
                else
                {
                    if (d2 < d3)
                    {
                        return r2;
                    }
                    else
                    {
                        return r3;
                    }
                }
            }
        }

        /**
          Finds the closest point in the rectangle to an external point; Given
          a rectangle defined by four points v0, v1, v2, & v3, and the external
          point.

          @param v0 Rectangle vertex 1.
          @param v1 Rectangle vertex 2.
          @param v2 Rectangle vertex 3
          @param v3 Rectangle vertex 4.
          @param point External point.

          @return Closet point in the rectangle to the external point.
	  */
        public static Vector3 closestPointToRectangle(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 point)
        {
            Plane plane = new Plane(v0, v1, v2);

            // Project the point into the plane
            double a, b, c, d;
            plane.getEquation(out a, out b, out c, out d);

            double distance = a * point.x + b * point.y + c * point.z + d;
            Vector3 planePoint = point - distance * plane.normal();

            if (isPointInsideRectangle(v0, v1, v2, v3, plane.normal(), planePoint))
            {
                return planePoint;
            }
            else
            {
                return closestPointToRectanglePerimeter(v0, v1, v2, v3, planePoint);
            }
        }

        /**
         2D area via crossproduct, matching the AREA2 macro of the original
         implementation.  The axes i and j select the two dimensions of the
         triangle that are used for the calculation.
         */
        private static double area2(Vector3 d, Vector3 e, Vector3 f, int i, int j)
        {
            return ((e[i] - d[i]) * (f[j] - d[j])) - ((f[i] - d[i]) * (e[j] - d[j]));
        }

        /**
         The C++ implementation reinterprets the bits of the floating point
         value to test whether it is zero.  This is different from a comparison
         against zero because -0.0f has a non-zero bit pattern.
         */
        private static bool hasNonZeroBitPattern(float value)
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(value), 0) != 0;
        }

        /**
         The C++ implementation tests the sign bit of the floating point value.
         */
        private static bool hasNegativeSignBit(float value)
        {
            return (BitConverter.ToUInt32(BitConverter.GetBytes(value), 0) & 0x80000000) != 0;
        }

        /** Used by findRayCapsuleIntersection.
            @cite From magic software http://www.magic-software.com/Source/Intersection3D/MgcIntr3DLinCap.cpp */
        private static int findRayCapsuleIntersectionAux(
            Vector3 rkOrigin,
            Vector3 rkDirection,
            Capsule rkCapsule,
            double[] afT)
        {
            Vector3 capsuleDirection = rkCapsule.getPoint2() - rkCapsule.getPoint1();

            // set up quadratic Q(t) = a*t^2 + 2*b*t + c
            Vector3 kU = Vector3.zero();
            Vector3 kV = Vector3.zero();
            Vector3 kW = capsuleDirection;
            float fWLength = kW.unitize(1e-06f);
            Vector3.generateOrthonormalBasis(ref kU, ref kV, ref kW, true);
            Vector3 kD = new Vector3(kU.dot(rkDirection), kV.dot(rkDirection), kW.dot(rkDirection));
            float fDLength = kD.unitize(1e-06f);

            float fEpsilon = 1e-6f;

            float fInvDLength = 1.0f / fDLength;
            Vector3 kDiff = rkOrigin - rkCapsule.getPoint1();
            Vector3 kP = new Vector3(kU.dot(kDiff), kV.dot(kDiff), kW.dot(kDiff));
            float fRadiusSqr = (float)g3dmath.square(rkCapsule.getRadius());

            float fInv, fA, fB, fC, fDiscr, fRoot, fT, fTmp;

            // Is the velocity parallel to the capsule direction? (or zero)
            if ((Math.Abs(kD.z) >= 1.0f - fEpsilon) || (fDLength < fEpsilon))
            {
                float fAxisDir = rkDirection.dot(capsuleDirection);

                fDiscr = fRadiusSqr - kP.x * kP.x - kP.y * kP.y;
                if ((fAxisDir < 0) && (fDiscr >= 0.0f))
                {
                    // Velocity anti-parallel to the capsule direction
                    fRoot = (float)Math.Sqrt(fDiscr);
                    afT[0] = (kP.z + fRoot) * fInvDLength;
                    afT[1] = -(fWLength - kP.z + fRoot) * fInvDLength;
                    return 2;
                }
                else if ((fAxisDir > 0) && (fDiscr >= 0.0f))
                {
                    // Velocity parallel to the capsule direction
                    fRoot = (float)Math.Sqrt(fDiscr);
                    afT[0] = -(kP.z + fRoot) * fInvDLength;
                    afT[1] = (fWLength - kP.z + fRoot) * fInvDLength;
                    return 2;
                }
                else
                {
                    // sphere heading wrong direction, or no velocity at all
                    return 0;
                }
            }

            // test intersection with infinite cylinder
            fA = kD.x * kD.x + kD.y * kD.y;
            fB = kP.x * kD.x + kP.y * kD.y;
            fC = kP.x * kP.x + kP.y * kP.y - fRadiusSqr;
            fDiscr = fB * fB - fA * fC;
            if (fDiscr < 0.0f)
            {
                // line does not intersect infinite cylinder
                return 0;
            }

            int iQuantity = 0;

            if (fDiscr > 0.0f)
            {
                // line intersects infinite cylinder in two places
                fRoot = (float)Math.Sqrt(fDiscr);
                fInv = 1.0f / fA;
                fT = (-fB - fRoot) * fInv;
                fTmp = kP.z + fT * kD.z;
                if ((0.0f <= fTmp) && (fTmp <= fWLength))
                {
                    afT[iQuantity] = fT * fInvDLength;
                    iQuantity++;
                }

                fT = (-fB + fRoot) * fInv;
                fTmp = kP.z + fT * kD.z;

                if ((0.0f <= fTmp) && (fTmp <= fWLength))
                {
                    afT[iQuantity++] = fT * fInvDLength;
                }

                if (iQuantity == 2)
                {
                    // line intersects capsule wall in two places
                    return 2;
                }
            }
            else
            {
                // line is tangent to infinite cylinder
                fT = -fB / fA;
                fTmp = kP.z + fT * kD.z;
                if ((0.0f <= fTmp) && (fTmp <= fWLength))
                {
                    afT[0] = fT * fInvDLength;
                    return 1;
                }
            }

            // test intersection with bottom hemisphere
            // fA = 1
            fB += kP.z * kD.z;
            fC += kP.z * kP.z;
            fDiscr = fB * fB - fC;
            if (fDiscr > 0.0f)
            {
                fRoot = (float)Math.Sqrt(fDiscr);
                fT = -fB - fRoot;
                fTmp = kP.z + fT * kD.z;
                if (fTmp <= 0.0f)
                {
                    afT[iQuantity++] = fT * fInvDLength;
                    if (iQuantity == 2)
                    {
                        return 2;
                    }
                }

                fT = -fB + fRoot;
                fTmp = kP.z + fT * kD.z;
                if (fTmp <= 0.0f)
                {
                    afT[iQuantity++] = fT * fInvDLength;
                    if (iQuantity == 2)
                    {
                        return 2;
                    }
                }
            }
            else if (fDiscr == 0.0f)
            {
                fT = -fB;
                fTmp = kP.z + fT * kD.z;
                if (fTmp <= 0.0f)
                {
                    afT[iQuantity++] = fT * fInvDLength;
                    if (iQuantity == 2)
                    {
                        return 2;
                    }
                }
            }

            // test intersection with top hemisphere
            // fA = 1
            fB -= kD.z * fWLength;
            fC += fWLength * (fWLength - 2.0f * kP.z);

            fDiscr = fB * fB - fC;
            if (fDiscr > 0.0f)
            {
                fRoot = (float)Math.Sqrt(fDiscr);
                fT = -fB - fRoot;
                fTmp = kP.z + fT * kD.z;
                if (fTmp >= fWLength)
                {
                    afT[iQuantity++] = fT * fInvDLength;
                    if (iQuantity == 2)
                    {
                        return 2;
                    }
                }

                fT = -fB + fRoot;
                fTmp = kP.z + fT * kD.z;
                if (fTmp >= fWLength)
                {
                    afT[iQuantity++] = fT * fInvDLength;
                    if (iQuantity == 2)
                    {
                        return 2;
                    }
                }
            }
            else if (fDiscr == 0.0f)
            {
                fT = -fB;
                fTmp = kP.z + fT * kD.z;
                if (fTmp >= fWLength)
                {
                    afT[iQuantity++] = fT * fInvDLength;
                    if (iQuantity == 2)
                    {
                        return 2;
                    }
                }
            }

            return iQuantity;
        }

        /** Used by collisionTimeForMovingPointFixedCapsule.
            @cite From magic software http://www.magic-software.com/Source/Intersection3D/MgcIntr3DLinCap.cpp

        	@param rkRay      The ray
        	@param rkCapsule  The capsule
        	@param riQuantity The number of intersections found
        	@param akPoint    The intersections found
        	@return           True if there is at least one intersection
        	*/
        private static bool findRayCapsuleIntersection(
            Ray rkRay,
            Capsule rkCapsule,
            out int riQuantity,
            Vector3[] akPoint)
        {
            double[] afT = new double[2];
            riQuantity = findRayCapsuleIntersectionAux(rkRay.origin, rkRay.direction, rkCapsule, afT);

            // Only return intersections that occur in the future
            int iClipQuantity = 0;
            int i;
            for (i = 0; i < riQuantity; i++)
            {
                if (afT[i] >= 0.0f)
                {
                    akPoint[iClipQuantity] = rkRay.origin + (float)afT[i] * rkRay.direction;
                    iClipQuantity++;
                }
            }

            riQuantity = iClipQuantity;
            return (riQuantity > 0);
        }
    }
}
