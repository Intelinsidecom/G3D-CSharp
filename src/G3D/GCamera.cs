/**
  @file GCamera.h

  Camera abstraction for 3D viewing.

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2001-05-29
  @edited  2006-03-04
*/

using System;
using System.Collections.Generic;
using G3D;

namespace G3D
{
    public sealed class GCamera
    {
        public sealed class Frustum
        {
            public sealed class Face
            {
                public int[] vertexIndex = new int[4];
                public Plane plane;
            }

            public Array<Vector4> vertexPos = new Array<Vector4>();
            public Array<Face> faceArray = new Array<Face>();
        }

        private Matrix4 projectionMatrix;
        private CoordinateFrame coordinateFrame;
        private float fieldOfView = (float)g3dmath.toRadians(55);
        private float imagePlaneDepth = 1.0f;
        private float nearPlane = 0.1f;
        private float farPlane = 1000.0f;
        private CoordinateFrame cframe = CoordinateFrame.identity();

        public GCamera()
        {
            projectionMatrix = Matrix4.identity();
            coordinateFrame = CoordinateFrame.identity();
        }

        public Matrix4 getProjectionMatrix()
        {
            return projectionMatrix;
        }

        public CoordinateFrame getCoordinateFrame()
        {
            return coordinateFrame;
        }

        public void setProjectionMatrix(Matrix4 m)
        {
            projectionMatrix = m;
        }

        public void setCoordinateFrame(CoordinateFrame c)
        {
            coordinateFrame = c;
            cframe = c;
        }

        public void setPosition(Vector3 pos)
        {
            coordinateFrame.translation = pos;
            cframe.translation = pos;
        }

        public void lookAt(Vector3 target)
        {
            coordinateFrame.lookAt(target);
            cframe.lookAt(target);
        }

        public void getFrustum(Rect2D viewport, Frustum f)
        {
            // Stub implementation
        }

        public Frustum frustum(Rect2D viewport)
        {
            return new Frustum();
        }

        public double getFieldOfView()
        {
            return fieldOfView;
        }

        public float getNearPlaneZ()
        {
            return -nearPlane;
        }

        public float getFarPlaneZ()
        {
            return -farPlane;
        }

        public void setFieldOfView(float angle)
        {
            fieldOfView = angle;
        }

        public void setImagePlaneDepth(float depth, Rect2D viewport)
        {
            imagePlaneDepth = depth;
        }

        public void getClipPlanes(Rect2D viewport, Array<Plane> outClip)
        {
            Matrix4 mvp = projectionMatrix * coordinateFrame.inverse().toMatrix4();
            
            // Extract planes from MVP matrix
            // Left:   column 3 + column 0
            // Right:  column 3 - column 0
            // Bottom: column 3 + column 1
            // Top:    column 3 - column 1
            // Near:   column 3 + column 2
            // Far:    column 3 - column 2
            
            Vector4 c0 = new Vector4(mvp.elt(0, 0), mvp.elt(1, 0), mvp.elt(2, 0), mvp.elt(3, 0));
            Vector4 c1 = new Vector4(mvp.elt(0, 1), mvp.elt(1, 1), mvp.elt(2, 1), mvp.elt(3, 1));
            Vector4 c2 = new Vector4(mvp.elt(0, 2), mvp.elt(1, 2), mvp.elt(2, 2), mvp.elt(3, 2));
            Vector4 c3 = new Vector4(mvp.elt(0, 3), mvp.elt(1, 3), mvp.elt(2, 3), mvp.elt(3, 3));
            
            Vector4 left = c3 + c0;
            Vector4 right = c3 - c0;
            Vector4 bottom = c3 + c1;
            Vector4 top = c3 - c1;
            Vector4 near = c3 + c2;
            Vector4 far = c3 - c2;
            
            outClip.clear();
            outClip.append(Plane.fromEquation(near.x, near.y, near.z, near.w));
            outClip.append(Plane.fromEquation(far.x, far.y, far.z, far.w));
            outClip.append(Plane.fromEquation(left.x, left.y, left.z, left.w));
            outClip.append(Plane.fromEquation(right.x, right.y, right.z, right.w));
            outClip.append(Plane.fromEquation(bottom.x, bottom.y, bottom.z, bottom.w));
            outClip.append(Plane.fromEquation(top.x, top.y, top.z, top.w));
        }
    }
}