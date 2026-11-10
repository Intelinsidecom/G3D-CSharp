/**
  @file GCamera.h

  Camera abstraction for 3D viewing.

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2001-05-29
  @edited  2006-03-04
 */

using System;

namespace G3D
{
    /**
      There is a viewport of width x height size in world space that corresponds to
      a screenWidth x screenHeight pixel grid on a
      renderDevice->getWidth() x renderDevice->getHeight()
      window.

      All viewport arguments are the pixel bounds of the viewport-- e.g.,
      RenderDevice::getViewport().
     */
    public sealed class GCamera
    {
        public sealed class Frustum
        {
            public sealed class Face
            {
                /** Counter clockwise indices into vertexPos */
                public int[] vertexIndex = new int[4];

                /** The plane containing the face. */
                public Plane plane;
            }

            /** The vertices, in homogeneous space.  If w == 0,
                a vertex is at infinity. */
            public Array<Vector4> vertexPos = new Array<Vector4>();

            /** The faces in the frustum.  When the
                far plane is at infinity, there are 5 faces,
                otherwise there are 6.  The faces are in the order
                N,R,L,B,T,[F].
             */
            public Array<Face> faceArray = new Array<Face>();
        }

        /**
         Vertical field of view (in radians)
         */
        private float fieldOfView;

        /**
         The image plane depth corresponding to a vertical field of
         view, where the film size is 1x1.
         */
        private float imagePlaneDepth;

        /**
         Clipping plane, *not* imaging plane.  Positive numbers.
         */
        private float nearPlane;

        /**
         Positive
         */
        private float farPlane;

        private CoordinateFrame cframe = CoordinateFrame.identity();

        public GCamera()
        {
            nearPlane = 0.1f;
            farPlane = (float)g3dmath.inf();
            setFieldOfView((float)g3dmath.toRadians(55.0f));
        }

        public CoordinateFrame getCoordinateFrame()
        {
            return cframe;
        }

        public void getCoordinateFrame(out CoordinateFrame c)
        {
            c = cframe;
        }

        public CoordinateFrame coordinateFrame()
        {
            return cframe;
        }

        public void setCoordinateFrame(CoordinateFrame c)
        {
            cframe = c;
        }

        public Vector3 getPosition()
        {
            return cframe.translation;
        }

        public void setPosition(Vector3 t)
        {
            cframe.translation = t;
        }

        /**
         Sets the horizontal field of view, in radians.  The
         initial angle is toRadians(55).
         <UL>
          <LI> toRadians(50) - Telephoto
          <LI> toRadians(110) - Normal
          <LI> toRadians(140) - Wide angle
         </UL>
        */
        public void setFieldOfView(float angle)
        {
            Debug.debugAssert((angle < g3dmath.G3D_PI) && (angle > 0));

            fieldOfView = angle;

            // Solve for the corresponding image plane depth, as if the extent
            // of the film was 1x1.
            imagePlaneDepth = 1.0f / (2.0f * (float)Math.Tan(angle / 2.0f));
        }

        /**
         Sets the field of view based on a desired image plane depth
         (<I>s'</I>) and film dimensions in world space.  Depth must be positive.  Width,
         depth, and height are measured in the same units (meters are
         recommended).  The field of view will span the diagonal to the
         image.

         <I>Note</I>: to simulate a 35mm GCamera, set width =
         0.36 mm and height = 0.24 mm.  The width and height used are
         generally not the pixel dimensions of the image.
        */
        public void setImagePlaneDepth(float depth, Rect2D viewport)
        {
            Debug.debugAssert(depth > 0);
            setFieldOfView(2.0f * (float)Math.Atan(viewport.height() / (2.0f * depth)));
        }

        public float getFieldOfView()
        {
            return fieldOfView;
        }

        public float getImagePlaneDepth(Rect2D viewport)
        {
            // The image plane depth has been pre-computed for
            // a 1x1 image.  Now that the image is width x height,
            // we need to scale appropriately.

            return imagePlaneDepth * viewport.height();
        }

        /**
         Returns the GCamera space width of the viewport.
         */
        public float getViewportWidth(Rect2D viewport)
        {
            return getViewportHeight(viewport) * viewport.width() / viewport.height();
        }

        /**
         Returns the GCamera space height of the viewport.
         */
        public float getViewportHeight(Rect2D viewport)
        {
            return nearPlane / imagePlaneDepth;
        }

        /**
         Projects a world space point onto a width x height screen.  The
         returned coordinate uses pixmap addressing: x = right and y =
         down.  The resulting z value is <I>rhw</I>.

         If the point is behind the camera, Vector3::inf() is returned.
        */
        public Vector3 project(Vector3 point, Rect2D viewport)
        {
            int screenWidth = (int)viewport.width();
            int screenHeight = (int)viewport.height();

            Vector3 outp = cframe.pointToObjectSpace(point);
            float w = outp.z * CoordinateFrame.zLookDirection;

            if (w <= 0)
            {
                return Vector3.inf();
            }
            Debug.debugAssert(w > 0);

            // Find where it hits an image plane of these dimensions
            float zImagePlane = getImagePlaneDepth(viewport);

            // Recover the distance
            float rhw = zImagePlane / w;

            // Add the image center, flip the y axis
            outp.x = screenWidth / 2.0f - (rhw * outp.x * CoordinateFrame.zLookDirection);
            outp.y = screenHeight / 2.0f - (rhw * outp.y);
            outp.z = rhw;

            return outp;
        }

        /**
         Returns the pixel area covered by a shape of the given
         world space area at the given z value (z must be negative).
        */
        public float worldToScreenSpaceArea(float area, float z, Rect2D viewport)
        {
            if (z >= 0)
            {
                return (float)g3dmath.inf();
            }

            float zImagePlane = getImagePlaneDepth(viewport);

            return area * (float)g3dmath.square((double)(zImagePlane / z));
        }

        /**
         Returns the world space ray passing through the center of pixel
         (x, y) on the image plane.  The pixel x and y axes are opposite
         the 3D object space axes: (0,0) is the upper left corner of the screen.
         They are in viewport coordinates, not screen coordinates.

         Integer (x, y) values correspond to
         the upper left corners of pixels.  If you want to cast rays
         through pixel centers, add 0.5 to x and y.
        */
        public Ray worldRay(float x, float y, Rect2D viewport)
        {
            int screenWidth = g3dmath.iFloor(viewport.width());
            int screenHeight = g3dmath.iFloor(viewport.height());

            Ray outp;
            // Set the origin to 0
            outp.origin = Vector3.zero();

            float cx = screenWidth / 2.0f;
            float cy = screenHeight / 2.0f;

            outp.direction =
                new Vector3((x - cx) * -CoordinateFrame.zLookDirection,
                          -(y - cy),
                           getImagePlaneDepth(viewport) * CoordinateFrame.zLookDirection);

            outp = cframe.toWorldSpace(outp);

            // Normalize the direction (we didn't do it before)
            outp.direction = outp.direction.direction();

            return outp;
        }

        /**
         Returns the world space 3D viewport corners.  These
         are at the near clipping plane.  The corners are constructed
         from the nearPlaneZ, getViewportWidth, and getViewportHeight.
         "left" and "right" are from the GCamera's perspective.
        */
        public void get3DViewportCorners(
            Rect2D viewport,
            out Vector3 outUR,
            out Vector3 outUL,
            out Vector3 outLL,
            out Vector3 outLR)
        {
            // Must be kept in sync with getFrustum()

            float sign = CoordinateFrame.zLookDirection;
            float w = -sign * getViewportWidth(viewport) / 2.0f;
            float h = getViewportHeight(viewport) / 2.0f;
            float z = -sign * getNearPlaneZ();

            // Compute the points
            outUR = new Vector3(w, h, z);
            outUL = new Vector3(-w, h, z);
            outLL = new Vector3(-w, -h, z);
            outLR = new Vector3(w, -h, z);

            // Take to world space
            outUR = cframe.pointToWorldSpace(outUR);
            outUL = cframe.pointToWorldSpace(outUL);
            outLR = cframe.pointToWorldSpace(outLR);
            outLL = cframe.pointToWorldSpace(outLL);
        }

        /**
          Returns a negative z-value.
         */
        public float getNearPlaneZ()
        {
            return -nearPlane;
        }

        /**
          Returns a negative z-value.
         */
        public float getFarPlaneZ()
        {
            return -farPlane;
        }

        public void setFarPlaneZ(float z)
        {
            Debug.debugAssert(z < 0);
            farPlane = -z;
        }

        public void setNearPlaneZ(float z)
        {
            Debug.debugAssert(z < 0);
            nearPlane = -z;
        }

        public void lookAt(Vector3 position)
        {
            lookAt(position, Vector3.unitY());
        }

        public void lookAt(Vector3 position, Vector3 up)
        {
            cframe.lookAt(position, up);
        }

        /**
         Returns the clipping planes of the frustum, in world space.
         The planes have normals facing <B>into</B> the view frustum.

         The plane order is guaranteed to be:
           Near, Right, Left, Top, Bottom, [Far]

         If the far plane is at infinity, the resulting array will have
         5 planes, otherwise there will be 6.

         The viewport is used only to determine the aspect ratio of the screen; the
         absolute dimensions and xy values don't matter.
        */
        public void getClipPlanes(Rect2D viewport, Array<Plane> clip)
        {
            Frustum fr;
            getFrustum(viewport, out fr);
            clip.resize(fr.faceArray.size(), false);
            for (int f = 0; f < clip.size(); ++f)
            {
                clip[f] = fr.faceArray[f].plane;
            }
        }

        /**
         Returns the world space view frustum, which is a truncated pyramid describing
         the volume of space seen by this camera.
        */
        public void getFrustum(Rect2D viewport, out Frustum fr)
        {
            fr = new Frustum();

            float x = getViewportWidth(viewport) / 2;
            float y = getViewportHeight(viewport) / 2;
            float z = getNearPlaneZ();
            float w = z / getFarPlaneZ();
            float fovx = x * fieldOfView / y;

            // Near face (ccw from UR)
            fr.vertexPos.append(
                new Vector4(x, y, z, 1),
                new Vector4(-x, y, z, 1),
                new Vector4(-x, -y, z, 1),
                new Vector4(x, -y, z, 1));

            // Far face (ccw from UR, from origin)
            fr.vertexPos.append(
                new Vector4(x, y, z, w),
                new Vector4(-x, y, z, w),
                new Vector4(-x, -y, z, w),
                new Vector4(x, -y, z, w));

            Frustum.Face face = new Frustum.Face();

            // Near plane (wind backwards so normal faces into frustum)
            // Recall that nearPlane, farPlane are positive numbers, so
            // we need to negate them to produce actual z values.
            face.plane = new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, -nearPlane));
            face.vertexIndex[0] = 3;
            face.vertexIndex[1] = 2;
            face.vertexIndex[2] = 1;
            face.vertexIndex[3] = 0;
            fr.faceArray.append(face);

            // Right plane
            face = new Frustum.Face();
            face.plane = new Plane(new Vector3(-(float)Math.Cos(fovx / 2), 0, -(float)Math.Sin(fovx / 2)), Vector3.zero());
            face.vertexIndex[0] = 0;
            face.vertexIndex[1] = 4;
            face.vertexIndex[2] = 7;
            face.vertexIndex[3] = 3;
            fr.faceArray.append(face);

            // Left plane
            face = new Frustum.Face();
            face.plane = new Plane(new Vector3(-fr.faceArray.last().plane.normal().x, 0, fr.faceArray.last().plane.normal().z), Vector3.zero());
            face.vertexIndex[0] = 5;
            face.vertexIndex[1] = 1;
            face.vertexIndex[2] = 2;
            face.vertexIndex[3] = 6;
            fr.faceArray.append(face);

            // Top plane
            face = new Frustum.Face();
            face.plane = new Plane(new Vector3(0, -(float)Math.Cos(fieldOfView / 2.0f), -(float)Math.Sin(fieldOfView / 2.0f)), Vector3.zero());
            face.vertexIndex[0] = 1;
            face.vertexIndex[1] = 5;
            face.vertexIndex[2] = 4;
            face.vertexIndex[3] = 0;
            fr.faceArray.append(face);

            // Bottom plane
            face = new Frustum.Face();
            face.plane = new Plane(new Vector3(0, -fr.faceArray.last().plane.normal().y, fr.faceArray.last().plane.normal().z), Vector3.zero());
            face.vertexIndex[0] = 2;
            face.vertexIndex[1] = 3;
            face.vertexIndex[2] = 7;
            face.vertexIndex[3] = 6;
            fr.faceArray.append(face);

            // Far plane
            if (farPlane < g3dmath.inf())
            {
                face = new Frustum.Face();
                face.plane = new Plane(new Vector3(0, 0, 1), new Vector3(0, 0, -farPlane));
                face.vertexIndex[0] = 4;
                face.vertexIndex[1] = 5;
                face.vertexIndex[2] = 6;
                face.vertexIndex[3] = 7;
                fr.faceArray.append(face);
            }

            // Transform vertices to world space
            for (int v = 0; v < fr.vertexPos.size(); ++v)
            {
                fr.vertexPos[v] = cframe.toWorldSpace(fr.vertexPos[v]);
            }

            // Transform planes to world space
            for (int p = 0; p < fr.faceArray.size(); ++p)
            {
                // Since there is no scale factor, we don't have to
                // worry about the inverse transpose of the normal.
                Vector3 normal;
                float d;

                fr.faceArray[p].plane.getEquation(out normal, out d);

                Vector3 newNormal = cframe.rotation * normal;

                if (g3dmath.isFinite(d))
                {
                    d = (newNormal * -d + cframe.translation).dot(newNormal);
                    fr.faceArray[p].plane = new Plane(newNormal, newNormal * d);
                }
                else
                {
                    // When d is infinite, we can't multiply 0's by it without
                    // generating NaNs.
                    fr.faceArray[p].plane = Plane.fromEquation(newNormal.x, newNormal.y, newNormal.z, d);
                }
            }
        }

        public Frustum frustum(Rect2D viewport)
        {
            Frustum f;
            getFrustum(viewport, out f);
            return f;
        }

        /** Read and Write camera parameters */
        public void serialize(BinaryOutput bo)
        {
            bo.writeFloat64(fieldOfView);
            bo.writeFloat64(imagePlaneDepth);
            Debug.debugAssert(nearPlane > 0.0f);
            bo.writeFloat64(nearPlane);
            Debug.debugAssert(farPlane > 0.0f);
            bo.writeFloat64(farPlane);
            cframe.serialize(bo);
        }

        public void deserialize(BinaryInput bi)
        {
            fieldOfView = (float)bi.readFloat64();
            imagePlaneDepth = (float)bi.readFloat64();
            nearPlane = (float)bi.readFloat64();
            Debug.debugAssert(nearPlane > 0.0f);
            farPlane = (float)bi.readFloat64();
            Debug.debugAssert(farPlane > 0.0f);
            cframe.deserialize(bi);
        }
    }
}
