/**
  @file Draw.h
   
  @maintainer Morgan McGuire, matrix@graphics3d.com
  
  @created 2003-10-29
  @edited  2005-08-06
*/

using System;
using G3D;

namespace G3D
{
    /**
      Useful routines for rendering primitives when debugging.  Compared
      to the optimized RenderDevice::beginIndexedPrimitives calls used
      by IFSModel, these routines are slow.

      When rendering translucent bounding objects, depth write is automatically
      disabled.  Render from back to front for proper transparency.
    */
    public static class Draw
    {
        private const int WIRE_SPHERE_SECTIONS = 26;
        private const int SPHERE_SECTIONS = 40;
        private const int SPHERE_PITCH_SECTIONS = 20;
        private const int SPHERE_YAW_SECTIONS = 40;

        /** Called from wireSphere, wireCapsule */
        private static void wireSphereSection(
            Sphere sphere,
            RenderDevice renderDevice,
            Color4 color,
            bool top,
            bool bottom)
        {
            int sections = WIRE_SPHERE_SECTIONS;
            int start = top ? 0 : (sections / 2);
            int stop = bottom ? sections : (sections / 2);

            renderDevice.pushState();
                renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
                renderDevice.setColor(color);
                renderDevice.setLineWidth(2);
                renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                renderDevice.setCullFace(RenderDevice.CullFace.CULL_BACK);
                renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                float radius = sphere.radius;
                Vector3 center = sphere.center;

                // Wire frame
                for (int y = 0; y < 8; ++y)
                {
                    float yaw = y * (float)g3dmath.G3D_PI / 4;
                    Vector3 x = new Vector3((float)Math.Cos(yaw) * radius, 0, (float)Math.Sin(yaw) * radius);

                    renderDevice.beginPrimitive(RenderDevice.Primitive.LINE_STRIP);
                        for (int p = start; p <= stop; ++p)
                        {
                            float pitch0 = p * (float)g3dmath.G3D_PI / (sections * 0.5f);

                            Vector3 v0 = (float)Math.Cos(pitch0) * x + Vector3.unitY() * radius * (float)Math.Sin(pitch0);
                            renderDevice.setNormal(v0.direction());
                            renderDevice.sendVertex(v0 + center);
                        }
                    renderDevice.endPrimitive();
                }

                int a = bottom ? -1 : 0;
                int b = top ? 1 : 0;
                for (int p = a; p <= b; ++p)
                {
                    float pitch = p * (float)g3dmath.G3D_PI / 6;

                    renderDevice.beginPrimitive(RenderDevice.Primitive.LINE_STRIP);
                        for (int y = 0; y <= sections; ++y)
                        {
                            float yaw0 = y * (float)g3dmath.G3D_PI / 13;
                            Vector3 v0 = new Vector3((float)Math.Cos(yaw0) * (float)Math.Cos(pitch), (float)Math.Sin(pitch), (float)Math.Sin(yaw0) * (float)Math.Cos(pitch)) * radius;
                            renderDevice.setNormal(v0.direction());
                            renderDevice.sendVertex(v0 + center);
                        }
                    renderDevice.endPrimitive();
                }

            renderDevice.popState();
        }

        private static void sphereSection(
            Sphere sphere,
            RenderDevice renderDevice,
            Color4 color,
            bool top,
            bool bottom)
        {
            CoordinateFrame cframe = renderDevice.getObjectToWorldMatrix();

            // Auto-normalization will take care of normal length
            cframe.translation += cframe.rotation * sphere.center;
            cframe.rotation = cframe.rotation * sphere.radius;

            renderDevice.pushState();
                renderDevice.setObjectToWorldMatrix(cframe);

                renderDevice.setColor(color);
                renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);

                int firstPitch = top ? 0 : SPHERE_PITCH_SECTIONS / 2;
                int lastPitch = bottom ? SPHERE_PITCH_SECTIONS : SPHERE_PITCH_SECTIONS / 2;

                for (int p = firstPitch; p < lastPitch; ++p)
                {
                    float pitch0 = p * (float)g3dmath.G3D_PI / SPHERE_PITCH_SECTIONS;
                    float pitch1 = (p + 1) * (float)g3dmath.G3D_PI / SPHERE_PITCH_SECTIONS;

                    float sp0 = (float)Math.Sin(pitch0);
                    float sp1 = (float)Math.Sin(pitch1);
                    float cp0 = (float)Math.Cos(pitch0);
                    float cp1 = (float)Math.Cos(pitch1);

                    renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);
                        for (int y = 0; y < SPHERE_YAW_SECTIONS; ++y)
                        {
                            float yaw0 = -y * (float)g3dmath.G3D_PI * 2.0f / SPHERE_YAW_SECTIONS;
                            float yaw1 = -(y + 1) * (float)g3dmath.G3D_PI * 2.0f / SPHERE_YAW_SECTIONS;

                            float cy0 = (float)Math.Cos(yaw0);
                            float sy0 = (float)Math.Sin(yaw0);
                            float cy1 = (float)Math.Cos(yaw1);
                            float sy1 = (float)Math.Sin(yaw1);

                            Vector3 v00 = new Vector3(cy0 * sp0, cp0, sy0 * sp0);
                            Vector3 v01 = new Vector3(cy1 * sp0, cp0, sy1 * sp0);
                            Vector3 v10 = new Vector3(cy0 * sp1, cp1, sy0 * sp1);
                            Vector3 v11 = new Vector3(cy1 * sp1, cp1, sy1 * sp1);

                            renderDevice.setNormal(v00);
                            renderDevice.sendVertex(v00);

                            renderDevice.setNormal(v01);
                            renderDevice.sendVertex(v01);

                            renderDevice.setNormal(v11);
                            renderDevice.sendVertex(v11);

                            renderDevice.setNormal(v10);
                            renderDevice.sendVertex(v10);
                        }
                    renderDevice.endPrimitive();
                }

            renderDevice.popState();
        }

        /**
         Returns the scale due to perspective at
         a point for a line.
        */
        private static float perspectiveLineThickness(
            RenderDevice rd,
            Vector3 pt)
        {
            Vector4 projected = screenProject(rd, new Vector4(pt, 1.0f));
            if (projected.w > 0)
            {
                return Math.Max(15.0f * projected.w, 10.0f);
            }
            return 0.0f;
        }

        private static Vector4 screenProject(RenderDevice rd, Vector4 v)
        {
            Vector4 clip = rd.project(v);
            Rect2D viewport = rd.getViewport();
            double rhw = 1.0 / clip.w;
            return new Vector4(
                (float)((1 + clip.x * rhw) * viewport.width() / 2 + viewport.x0()),
                (float)((1 - clip.y * rhw) * viewport.height() / 2 + viewport.y0()),
                (float)(clip.z * rhw),
                (float)rhw);
        }

        /**
        Renders exact corners of a 2D polygon using lines.
        Assumes you already called push2D(). 
         */
        public static void poly2DOutline(Array<Vector2> polygon, RenderDevice renderDevice, Color4 color = default(Color4))
        {
            if (color == default(Color4)) color = new Color4(Color3.yellow(), 1);

            if (polygon.size() == 0)
            {
                return;
            }

            renderDevice.beginPrimitive(RenderDevice.Primitive.LINE_STRIP);
                renderDevice.setColor(color);
                for (int i = 0; i < polygon.size(); ++i)
                {
                    renderDevice.sendVertex(polygon[i]);
                }
                renderDevice.sendVertex(polygon[0]);
            renderDevice.endPrimitive();
        }

        public static void poly2D(Array<Vector2> polygon, RenderDevice renderDevice, Color4 color = default(Color4))
        {
            if (color == default(Color4)) color = new Color4(Color3.yellow(), 1);

            if (polygon.size() == 0)
            {
                return;
            }

            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLE_FAN);
                renderDevice.setColor(color);
                for (int i = 0; i < polygon.size(); ++i)
                {
                    renderDevice.sendVertex(polygon[i]);
                }
            renderDevice.endPrimitive();
        }

        /**
         Set the solid color or wire color to Color4::clear() to
         prevent rendering of surfaces or lines.
         */
        public static void box(
            Box box,
            RenderDevice rd,
            Color4 solidColor = default(Color4),
            Color4 wireColor = default(Color4))
        {
            if (solidColor == default(Color4)) solidColor = new Color4(1, .2f, .2f, .5f);
            if (wireColor == default(Color4)) wireColor = new Color4(Color3.black(), 1);

            rd.pushState();
                rd.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);

                if (solidColor.a > 0)
                {
                    int numPasses = 1;

                    if (solidColor.a < 1)
                    {
                        // Multiple rendering passes to get front/back blending correct
                        rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                        numPasses = 2;
                        rd.setCullFace(RenderDevice.CullFace.CULL_FRONT);
                        rd.setDepthWrite(false);
                    }
                    else
                    {
                        rd.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    }

                    rd.setColor(solidColor);
                    for (int k = 0; k < numPasses; ++k)
                    {
                        rd.beginPrimitive(RenderDevice.Primitive.QUADS);
                            for (int i = 0; i < 6; ++i)
                            {
                                Vector3 v0, v1, v2, v3;
                                box.getFaceCorners(i, out v0, out v1, out v2, out v3);

                                Vector3 n = (v1 - v0).cross(v3 - v0);
                                rd.setNormal(n.direction());
                                rd.sendVertex(v0);
                                rd.sendVertex(v1);
                                rd.sendVertex(v2);
                                rd.sendVertex(v3);
                            }
                        rd.endPrimitive();
                        rd.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    }
                }

                if (wireColor.a > 0)
                {
                    rd.setDepthWrite(true);
                    rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                    rd.setColor(wireColor);
                    rd.setLineWidth(2);

                    Vector3 c = box.getCenter();
                    Vector3 v;

                    // Wire frame
                    rd.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                    rd.beginPrimitive(RenderDevice.Primitive.LINES);

                        // Front and back
                        for (int i = 0; i < 8; i += 4)
                        {
                            for (int j = 0; j < 4; ++j)
                            {
                                v = box.getCorner(i + j);
                                rd.setNormal((v - c).direction());
                                rd.sendVertex(v);

                                v = box.getCorner(i + ((j + 1) % 4));
                                rd.setNormal((v - c).direction());
                                rd.sendVertex(v);
                            }
                        }

                        // Sides
                        for (int i = 0; i < 4; ++i)
                        {
                            v = box.getCorner(i);
                            rd.setNormal((v - c).direction());
                            rd.sendVertex(v);

                            v = box.getCorner(i + 4);
                            rd.setNormal((v - c).direction());
                            rd.sendVertex(v);
                        }

                    rd.endPrimitive();
                }
            rd.popState();
        }

        public static void box(
            AABox box,
            RenderDevice rd,
            Color4 solidColor = default(Color4),
            Color4 wireColor = default(Color4))
        {
            Draw.box(box.toBox(), rd, solidColor, wireColor);
        }

        public static void sphere(
            Sphere sphere,
            RenderDevice rd,
            Color4 solidColor = default(Color4),
            Color4 wireColor = default(Color4))
        {
            if (solidColor == default(Color4)) solidColor = new Color4(1, 1, 0, .5f);
            if (wireColor == default(Color4)) wireColor = new Color4(Color3.black(), 1);

            if (solidColor.a > 0)
            {
                rd.pushState();

                    int numPasses = 1;

                    if (solidColor.a < 1)
                    {
                        numPasses = 2;
                        rd.setCullFace(RenderDevice.CullFace.CULL_FRONT);
                        rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                        rd.setDepthWrite(false);
                    }
                    else
                    {
                        rd.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    }

                    if (wireColor.a > 0)
                    {
                        rd.setPolygonOffset(3);
                    }

                    for (int k = 0; k < numPasses; ++k)
                    {
                        sphereSection(sphere, rd, solidColor, true, true);
                        rd.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    }
                rd.popState();
            }

            if (wireColor.a > 0)
            {
                wireSphereSection(sphere, rd, wireColor, true, true);
            }
        }

        public static void plane(
            Plane plane,
            RenderDevice rd,
            Color4 solidColor = default(Color4),
            Color4 wireColor = default(Color4))
        {
            if (solidColor == default(Color4)) solidColor = new Color4(.2f, .2f, 1, .5f);
            if (wireColor == default(Color4)) wireColor = new Color4(Color3.black(), 1);

            rd.pushState();
                CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();

                Vector3 planeNormal;
                double d;

                plane.getEquation(out planeNormal, out d);
                Vector3 P = planeNormal * (float)d;

                CoordinateFrame cframe1 = new CoordinateFrame(P);
                cframe1.lookAt(P + planeNormal);

                rd.setObjectToWorldMatrix(cframe0 * cframe1);

                rd.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
                rd.enableTwoSidedLighting();

                if (solidColor.a > 0.0f)
                {
                    // Draw concentric circles around the origin.  We break them up to get good depth
                    // interpolation and reasonable shading.

                    rd.setPolygonOffset(0.7);

                    if (solidColor.a < 1.0f)
                    {
                        rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                    }

                    rd.setNormal(Vector3.unitZ());
                    rd.setColor(solidColor);

                    rd.setCullFace(RenderDevice.CullFace.CULL_NONE);
                    // Infinite strip
                    const int N = 12;
                    float r1 = 100;
                    rd.beginPrimitive(RenderDevice.Primitive.QUAD_STRIP);
                        for (int i = 0; i <= N; ++i)
                        {
                            float a = i * (float)g3dmath.G3D_TWO_PI / N;
                            float c = (float)Math.Cos(a);
                            float s = (float)Math.Sin(a);

                            rd.sendVertex(new Vector3(c * r1, s * r1, 0));
                            rd.sendVertex(new Vector4(c, s, 0, 0));
                        }
                    rd.endPrimitive();

                    // Finite strips.
                    float r2 = 0;
                    const int M = 4;
                    for (int j = 0; j < M; ++j)
                    {
                        r2 = r1;
                        r1 = r1 / 3;
                        if (j == M - 1)
                        {
                            // last pass
                            r1 = 0;
                        }

                        rd.beginPrimitive(RenderDevice.Primitive.QUAD_STRIP);
                            for (int i = 0; i <= N; ++i)
                            {
                                float a = i * (float)g3dmath.G3D_TWO_PI / N;
                                float c = (float)Math.Cos(a);
                                float s = (float)Math.Sin(a);

                                rd.sendVertex(new Vector3(c * r1, s * r1, 0));
                                rd.sendVertex(new Vector3(c * r2, s * r2, 0));
                            }
                        rd.endPrimitive();
                    }
                }

                if (wireColor.a > 0)
                {
                    rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                    rd.setLineWidth(1.5);

                    rd.beginPrimitive(RenderDevice.Primitive.LINES);
                        rd.setColor(wireColor);
                        rd.setNormal(Vector3.unitZ());

                        // Lines to infinity
                        rd.sendVertex(new Vector4(1, 0, 0, 0));
                        rd.sendVertex(new Vector4(0, 0, 0, 1));

                        rd.sendVertex(new Vector4(-1, 0, 0, 0));
                        rd.sendVertex(new Vector4(0, 0, 0, 1));

                        rd.sendVertex(new Vector4(0, -1, 0, 0));
                        rd.sendVertex(new Vector4(0, 0, 0, 1));

                        rd.sendVertex(new Vector4(0, 1, 0, 0));
                        rd.sendVertex(new Vector4(0, 0, 0, 1));
                    rd.endPrimitive();

                    rd.setLineWidth(0.5);

                    rd.beginPrimitive(RenderDevice.Primitive.LINES);

                        // Horizontal and vertical lines
                        const int N = 10;
                        const float space = 1;
                        const float Ns = N * space;
                        for (int x = -N; x <= N; ++x)
                        {
                            float sx = x * space;
                            rd.sendVertex(new Vector3(Ns, sx, 0));
                            rd.sendVertex(new Vector3(-Ns, sx, 0));

                            rd.sendVertex(new Vector3(sx, Ns, 0));
                            rd.sendVertex(new Vector3(sx, -Ns, 0));
                        }

                    rd.endPrimitive();
                }

            rd.popState();
        }

        public static void line(
            Line line,
            RenderDevice rd,
            Color4 color = default(Color4))
        {
            if (color == default(Color4)) color = new Color4(Color3.black(), 1);

            rd.pushState();
                rd.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
                rd.setColor(color);
                rd.setLineWidth(2);
                rd.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                Vector3 v0 = line.point();
                Vector3 d = line.direction();
                rd.beginPrimitive(RenderDevice.Primitive.LINE_STRIP);
                    // Off to infinity
                    rd.sendVertex(new Vector4(-d, 0));

                    for (int i = -10; i <= 10; i += 2)
                    {
                        rd.sendVertex(v0 + d * (float)i * 100.0f);
                    }

                    // Off to infinity
                    rd.sendVertex(new Vector4(d, 0));
                rd.endPrimitive();
            rd.popState();
        }

        public static void lineSegment(
            LineSegment lineSegment,
            RenderDevice rd,
            Color4 color = default(Color4),
            float scale = 1)
        {
            if (color == default(Color4)) color = new Color4(Color3.black(), 1);

            rd.pushState();

                rd.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
                rd.setColor(color);

                // Compute perspective line width
                Vector3 v0 = lineSegment.endPoint(0);
                Vector3 v1 = lineSegment.endPoint(1);

                Vector4 s0 = screenProject(rd, new Vector4(v0, 1.0f));
                Vector4 s1 = screenProject(rd, new Vector4(v1, 1.0f));

                float L = 2 * scale;
                if ((s0.w > 0) && (s1.w > 0))
                {
                    L = 15.0f * (s0.w + s1.w) / 2.0f;
                }
                else if (s0.w > 0)
                {
                    L = Math.Max(15 * s0.w, 10.0f);
                }
                else if (s1.w > 0)
                {
                    L = Math.Max(15.0f * s1.w, 10.0f);
                }

                rd.setLineWidth(L);

                // Find the object space vector perpendicular to the line
                // that points closest to the eye.
                Vector3 eye = rd.getObjectToWorldMatrix().pointToObjectSpace(rd.getCameraToWorldMatrix().translation);
                Vector3 E = eye - v0;
                Vector3 V = v1 - v0;
                Vector3 U = E.cross(V);
                Vector3 N = V.cross(U).direction();

                rd.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                rd.beginPrimitive(RenderDevice.Primitive.LINES);
                    rd.setNormal(N);
                    rd.sendVertex(v0);
                    rd.sendVertex(v1);
                rd.endPrimitive();
            rd.popState();
        }

        /**
         Renders per-vertex normals as thin arrows.  The length
         of the normals is scaled inversely to the number of normals
         rendered.
         */
        public static void vertexNormals(
            MeshAlg.Geometry geometry,
            RenderDevice renderDevice,
            Color4 color = default(Color4),
            float scale = 1)
        {
            if (color == default(Color4)) color = new Color4(Color3.green() * .5f, 1);

            renderDevice.pushState();
                renderDevice.setColor(color);
                renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                Array<Vector3> vertexArray = geometry.vertexArray;
                Array<Vector3> normalArray = geometry.normalArray;

                float D = g3dmath.clamp(5.0f / (float)Math.Pow((float)vertexArray.size(), 0.25f), 0.1f, .8f) * scale;

                renderDevice.setLineWidth(1);
                renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                    for (int v = 0; v < vertexArray.size(); ++v)
                    {
                        renderDevice.sendVertex(vertexArray[v] + normalArray[v] * D);
                        renderDevice.sendVertex(vertexArray[v]);
                    }
                renderDevice.endPrimitive();

                renderDevice.setLineWidth(2);
                renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                    for (int v = 0; v < vertexArray.size(); ++v)
                    {
                        renderDevice.sendVertex(vertexArray[v] + normalArray[v] * D * .96f);
                        renderDevice.sendVertex(vertexArray[v] + normalArray[v] * D * .84f);
                    }
                renderDevice.endPrimitive();

                renderDevice.setLineWidth(3);
                renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                    for (int v = 0; v < vertexArray.size(); ++v)
                    {
                        renderDevice.sendVertex(vertexArray[v] + normalArray[v] * D * .92f);
                        renderDevice.sendVertex(vertexArray[v] + normalArray[v] * D * .84f);
                    }
                renderDevice.endPrimitive();
            renderDevice.popState();
        }

        /**
         Convenient for rendering tangent space basis vectors.
         */
        public static void vertexVectors(
            Array<Vector3> vertexArray,
            Array<Vector3> directionArray,
            RenderDevice renderDevice,
            Color4 color = default(Color4),
            float scale = 1)
        {
            if (color == default(Color4)) color = new Color4(Color3.red() * 0.5f, 1);

            renderDevice.pushState();
                renderDevice.setColor(color);
                renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                float D = g3dmath.clamp(5.0f / (float)Math.Pow((float)vertexArray.size(), 0.25f), 0.1f, 0.8f) * scale;

                renderDevice.setLineWidth(1);
                renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                    for (int v = 0; v < vertexArray.size(); ++v)
                    {
                        renderDevice.sendVertex(vertexArray[v] + directionArray[v] * D);
                        renderDevice.sendVertex(vertexArray[v]);
                    }
                renderDevice.endPrimitive();

                renderDevice.setLineWidth(2);
                renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                    for (int v = 0; v < vertexArray.size(); ++v)
                    {
                        renderDevice.sendVertex(vertexArray[v] + directionArray[v] * D * .96f);
                        renderDevice.sendVertex(vertexArray[v] + directionArray[v] * D * .84f);
                    }
                renderDevice.endPrimitive();

                renderDevice.setLineWidth(3);
                renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                    for (int v = 0; v < vertexArray.size(); ++v)
                    {
                        renderDevice.sendVertex(vertexArray[v] + directionArray[v] * D * .92f);
                        renderDevice.sendVertex(vertexArray[v] + directionArray[v] * D * .84f);
                    }
                renderDevice.endPrimitive();
            renderDevice.popState();
        }

        public static void capsule(
           Capsule capsule, 
           RenderDevice renderDevice,
           Color4 solidColor = default(Color4),
           Color4 wireColor = default(Color4))
        {
            if (solidColor == default(Color4)) solidColor = new Color4(1, 0, 1, .5f);
            if (wireColor == default(Color4)) wireColor = new Color4(Color3.black(), 1);

            CoordinateFrame cframe = new CoordinateFrame(capsule.getPoint1());

            Vector3 Y = (capsule.getPoint2() - capsule.getPoint1()).direction();
            Vector3 X = (Math.Abs(Y.dot(Vector3.unitX())) > 0.9) ? Vector3.unitY() : Vector3.unitX();
            Vector3 Z = X.cross(Y).direction();
            X = Y.cross(Z);
            cframe.rotation.setColumn(0, X);
            cframe.rotation.setColumn(1, Y);
            cframe.rotation.setColumn(2, Z);

            float radius = capsule.getRadius();
            float height = (capsule.getPoint2() - capsule.getPoint1()).magnitude();

            // Always render upright in object space
            Sphere sphere1 = new Sphere(Vector3.zero(), radius);
            Sphere sphere2 = new Sphere(new Vector3(0, height, 0), radius);

            Vector3 top = new Vector3(0, height, 0);

            renderDevice.pushState();

                renderDevice.setObjectToWorldMatrix(renderDevice.getObjectToWorldMatrix() * cframe);
                renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);

                if (solidColor.a > 0)
                {
                    int numPasses = 1;

                    if (solidColor.a < 1)
                    {
                        // Multiple rendering passes to get front/back blending correct
                        renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                        numPasses = 2;
                        renderDevice.setCullFace(RenderDevice.CullFace.CULL_FRONT);
                        renderDevice.setDepthWrite(false);
                    }

                    renderDevice.setColor(solidColor);
                    for (int k = 0; k < numPasses; ++k)
                    {
                        sphereSection(sphere1, renderDevice, solidColor, false, true);
                        sphereSection(sphere2, renderDevice, solidColor, true, false);

                        // Cylinder faces
                        renderDevice.beginPrimitive(RenderDevice.Primitive.QUAD_STRIP);
                            for (int y = 0; y <= SPHERE_SECTIONS; ++y)
                            {
                                float yaw0 = y * (float)g3dmath.G3D_PI * 2.0f / SPHERE_SECTIONS;
                                Vector3 v0 = new Vector3((float)Math.Cos(yaw0), 0, (float)Math.Sin(yaw0));

                                renderDevice.setNormal(v0);
                                renderDevice.sendVertex(v0 * radius);
                                renderDevice.sendVertex(v0 * radius + top);
                            }
                        renderDevice.endPrimitive();

                        renderDevice.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    }
                }

                if (wireColor.a > 0)
                {
                    renderDevice.setDepthWrite(true);
                    renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                    wireSphereSection(sphere1, renderDevice, wireColor, false, true);
                    wireSphereSection(sphere2, renderDevice, wireColor, true, false);

                    // Line around center
                    renderDevice.setColor(wireColor);
                    Vector3 center = new Vector3(0, height / 2, 0);
                    renderDevice.setLineWidth(2);
                    renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                        for (int y = 0; y < WIRE_SPHERE_SECTIONS; ++y)
                        {
                            float yaw0 = y * (float)g3dmath.G3D_PI * 2.0f / WIRE_SPHERE_SECTIONS;
                            float yaw1 = (y + 1) * (float)g3dmath.G3D_PI * 2.0f / WIRE_SPHERE_SECTIONS;

                            Vector3 v0 = new Vector3((float)Math.Cos(yaw0), 0, (float)Math.Sin(yaw0));
                            Vector3 v1 = new Vector3((float)Math.Cos(yaw1), 0, (float)Math.Sin(yaw1));

                            renderDevice.setNormal(v0);
                            renderDevice.sendVertex(v0 * radius + center);
                            renderDevice.setNormal(v1);
                            renderDevice.sendVertex(v1 * radius + center);
                        }

                        // Edge lines
                        for (int y = 0; y < 8; ++y)
                        {
                            float yaw = y * (float)g3dmath.G3D_PI / 4;
                            Vector3 x = new Vector3((float)Math.Cos(yaw), 0, (float)Math.Sin(yaw));

                            renderDevice.setNormal(x);
                            renderDevice.sendVertex(x * radius);
                            renderDevice.sendVertex(x * radius + top);
                        }
                    renderDevice.endPrimitive();
                }

            renderDevice.popState();
        }

        public static void cylinder(
           Cylinder cylinder, 
           RenderDevice renderDevice,
           Color4 solidColor = default(Color4),
           Color4 wireColor = default(Color4))
        {
            if (solidColor == default(Color4)) solidColor = new Color4(1, 1, 0, .5f);
            if (wireColor == default(Color4)) wireColor = new Color4(Color3.black(), 1);

            CoordinateFrame cframe = CoordinateFrame.identity();

            cylinder.getReferenceFrame(ref cframe);

            float radius = cylinder.radius();
            float height = cylinder.height();

            // Always render upright in object space
            Vector3 bot = new Vector3(0, -height / 2, 0);
            Vector3 top = new Vector3(0, height / 2, 0);

            renderDevice.pushState();

                renderDevice.setObjectToWorldMatrix(renderDevice.getObjectToWorldMatrix() * cframe);
                renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);

                if (solidColor.a > 0)
                {
                    int numPasses = 1;

                    if (solidColor.a < 1)
                    {
                        // Multiple rendering passes to get front/back blending correct
                        renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                        numPasses = 2;
                        renderDevice.setCullFace(RenderDevice.CullFace.CULL_FRONT);
                        renderDevice.setDepthWrite(false);
                    }

                    renderDevice.setColor(solidColor);
                    for (int k = 0; k < numPasses; ++k)
                    {
                        // Top
                        renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLE_FAN);
                            renderDevice.setNormal(Vector3.unitY());
                            renderDevice.sendVertex(top);
                            for (int y = 0; y <= SPHERE_SECTIONS; ++y)
                            {
                                float yaw0 = -y * (float)g3dmath.G3D_PI * 2.0f / SPHERE_SECTIONS;
                                Vector3 v0 = new Vector3((float)Math.Cos(yaw0), 0, (float)Math.Sin(yaw0));

                                renderDevice.sendVertex(v0 * radius + top);
                            }
                        renderDevice.endPrimitive();

                        // Bottom
                        renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLE_FAN);
                            renderDevice.setNormal(-Vector3.unitY());
                            renderDevice.sendVertex(bot);
                            for (int y = 0; y <= SPHERE_SECTIONS; ++y)
                            {
                                float yaw0 = y * (float)g3dmath.G3D_PI * 2.0f / SPHERE_SECTIONS;
                                Vector3 v0 = new Vector3((float)Math.Cos(yaw0), 0, (float)Math.Sin(yaw0));

                                renderDevice.sendVertex(v0 * radius + bot);
                            }
                        renderDevice.endPrimitive();

                        // Cylinder faces
                        renderDevice.beginPrimitive(RenderDevice.Primitive.QUAD_STRIP);
                            for (int y = 0; y <= SPHERE_SECTIONS; ++y)
                            {
                                float yaw0 = y * (float)g3dmath.G3D_PI * 2.0f / SPHERE_SECTIONS;
                                Vector3 v0 = new Vector3((float)Math.Cos(yaw0), 0, (float)Math.Sin(yaw0));

                                renderDevice.setNormal(v0);
                                renderDevice.sendVertex(v0 * radius + bot);
                                renderDevice.sendVertex(v0 * radius + top);
                            }
                        renderDevice.endPrimitive();

                        renderDevice.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    }
                }

                if (wireColor.a > 0)
                {
                    renderDevice.setDepthWrite(true);
                    renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);

                    // Line around center
                    renderDevice.setColor(wireColor);
                    renderDevice.setLineWidth(2);
                    renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                        for (int z = 0; z < 3; ++z)
                        {
                            Vector3 center = new Vector3(0, 0.0f, 0);
                            for (int y = 0; y < WIRE_SPHERE_SECTIONS; ++y)
                            {
                                float yaw0 = y * (float)g3dmath.G3D_PI * 2.0f / WIRE_SPHERE_SECTIONS;
                                float yaw1 = (y + 1) * (float)g3dmath.G3D_PI * 2.0f / WIRE_SPHERE_SECTIONS;

                                Vector3 v0 = new Vector3((float)Math.Cos(yaw0), 0, (float)Math.Sin(yaw0));
                                Vector3 v1 = new Vector3((float)Math.Cos(yaw1), 0, (float)Math.Sin(yaw1));

                                renderDevice.setNormal(v0);
                                renderDevice.sendVertex(v0 * radius + center);
                                renderDevice.setNormal(v1);
                                renderDevice.sendVertex(v1 * radius + center);
                            }
                        }

                        // Edge lines
                        for (int y = 0; y < 8; ++y)
                        {
                            float yaw = y * (float)g3dmath.G3D_PI / 4;
                            Vector3 x = new Vector3((float)Math.Cos(yaw), 0, (float)Math.Sin(yaw));
                            Vector3 xr = x * radius;

                            // Side
                            renderDevice.setNormal(x);
                            renderDevice.sendVertex(xr + bot);
                            renderDevice.sendVertex(xr + top);

                            // Top
                            renderDevice.setNormal(Vector3.unitY());
                            renderDevice.sendVertex(top);
                            renderDevice.sendVertex(xr + top);

                            // Bottom
                            renderDevice.setNormal(Vector3.unitY());
                            renderDevice.sendVertex(bot);
                            renderDevice.sendVertex(xr + bot);
                        }
                    renderDevice.endPrimitive();
                }

            renderDevice.popState();
        }

        public static void cone(
           Cone cone, 
           RenderDevice renderDevice,
           Color4 solidColor = default(Color4),
           Color4 wireColor = default(Color4))
        {
            throw new NotSupportedException(
                "Draw.cone is not part of G3D 6.09 and cannot be rendered because G3D.Cone does not expose its tip, direction, or angle.");
        }

        public static void ray(
            Ray ray,
            RenderDevice renderDevice,
            Color4 color = default(Color4),
            float scale = 1)
        {
            if (color == default(Color4)) color = new Color4(Color3.orange(), 1);

            Vector3 tip = ray.origin + ray.direction;
            // Create a coordinate frame at the tip
            Vector3 u = ray.direction.direction();
            Vector3 v;
            if (u.x == 0)
            {
                v = Vector3.unitX();
            }
            else
            {
                v = Vector3.unitY();
            }
            Vector3 w = u.cross(v).direction();
            v = w.cross(u).direction();
            Vector3 back = tip - u * 0.3f * scale;

            RenderDevice.ShadeMode oldShadeMode = renderDevice.shadeMode();
            Color4 oldColor = renderDevice.color();

            renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
            renderDevice.setColor(color);

            float r = scale * .1f;
            // Arrow head.  Need this beginprimitive call to sync up G3D and OpenGL
            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
                renderDevice.setNormal(u);
                for (int a = 0; a < SPHERE_SECTIONS; ++a)
                {
                    float angle0 = a * (float)g3dmath.G3D_PI * 2.0f / SPHERE_SECTIONS;
                    float angle1 = (a + 1) * (float)g3dmath.G3D_PI * 2.0f / SPHERE_SECTIONS;
                    Vector3 dir0 = (float)Math.Cos(angle0) * v + (float)Math.Sin(angle0) * w;
                    Vector3 dir1 = (float)Math.Cos(angle1) * v + (float)Math.Sin(angle1) * w;
                    renderDevice.setNormal(dir0);
                    renderDevice.sendVertex(tip);

                    renderDevice.sendVertex(back + dir0 * r);

                    renderDevice.setNormal(dir1);
                    renderDevice.sendVertex(back + dir1 * r);
                }
            renderDevice.endPrimitive();

            // Back of arrow head
            renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLE_FAN);
                renderDevice.setNormal(-u);
                for (int a = 0; a <= SPHERE_SECTIONS; ++a)
                {
                    float angle = a * (float)g3dmath.G3D_PI * 2.0f / SPHERE_SECTIONS;
                    Vector3 dir = (float)Math.Sin(angle) * v + (float)Math.Cos(angle) * w;
                    renderDevice.sendVertex(back + dir * r);
                }
            renderDevice.endPrimitive();

            renderDevice.setColor(oldColor);
            renderDevice.setShadeMode(oldShadeMode);
            lineSegment(LineSegment.fromTwoPoints(ray.origin, back), renderDevice, color, scale);
        }

        public static void arrow(
            Vector3 start,
            Vector3 direction,
            RenderDevice renderDevice,
            Color4 color = default(Color4),
            float scale = 1.0f)
        {
            if (color == default(Color4)) color = new Color4(Color3.orange(), 1);

            ray(Ray.fromOriginAndDirection(start, direction), renderDevice, color, scale);
        }

        public static void axes(
            CoordinateFrame cframe,
            RenderDevice renderDevice,
            Color4 xColor = default(Color4),
            Color4 yColor = default(Color4),
            Color4 zColor = default(Color4),
            float scale = 1.0f)
        {
            if (xColor == default(Color4)) xColor = new Color4(Color3.red(), 1);
            if (yColor == default(Color4)) yColor = new Color4(Color3.green(), 1);
            if (zColor == default(Color4)) zColor = new Color4(Color3.blue(), 1);

            Vector3 c = cframe.translation;
            Vector3 x = cframe.rotation.getColumnVector(0).direction() * 2 * scale;
            Vector3 y = cframe.rotation.getColumnVector(1).direction() * 2 * scale;
            Vector3 z = cframe.rotation.getColumnVector(2).direction() * 2 * scale;

            Draw.arrow(c, x, renderDevice, xColor, scale);
            Draw.arrow(c, y, renderDevice, yColor, scale);
            Draw.arrow(c, z, renderDevice, zColor, scale);

            // Text label scale
            const float xx = -3;
            const float yy = xx * 1.4f;

            // Project the 3D locations of the labels
            Vector4 xc2D = screenProject(renderDevice, new Vector4(c + x * 1.1f, 1.0f));
            Vector4 yc2D = screenProject(renderDevice, new Vector4(c + y * 1.1f, 1.0f));
            Vector4 zc2D = screenProject(renderDevice, new Vector4(c + z * 1.1f, 1.0f));

            // If coordinates are behind the viewer, transform off screen
            Vector2 x2D = (xc2D.w > 0) ? xc2D.xy() : new Vector2(-2000, -2000);
            Vector2 y2D = (yc2D.w > 0) ? yc2D.xy() : new Vector2(-2000, -2000);
            Vector2 z2D = (zc2D.w > 0) ? zc2D.xy() : new Vector2(-2000, -2000);

            // Compute the size of the labels
            float xS = (xc2D.w > 0) ? g3dmath.clamp(10 * xc2D.w * scale, .1f, 5) : 0;
            float yS = (yc2D.w > 0) ? g3dmath.clamp(10 * yc2D.w * scale, .1f, 5) : 0;
            float zS = (zc2D.w > 0) ? g3dmath.clamp(10 * zc2D.w * scale, .1f, 5) : 0;

            renderDevice.push2D();
                renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                renderDevice.setLineWidth(2);

                renderDevice.beginPrimitive(RenderDevice.Primitive.LINES);
                    // X
                    renderDevice.setColor(xColor * 0.8f);
                    renderDevice.sendVertex(new Vector2(-xx, yy) * xS + x2D);
                    renderDevice.sendVertex(new Vector2(xx, -yy) * xS + x2D);
                    renderDevice.sendVertex(new Vector2(xx, yy) * xS + x2D);
                    renderDevice.sendVertex(new Vector2(-xx, -yy) * xS + x2D);

                    // Y
                    renderDevice.setColor(yColor * 0.8f);
                    renderDevice.sendVertex(new Vector2(-xx, yy) * yS + y2D);
                    renderDevice.sendVertex(new Vector2(0, 0) * yS + y2D);
                    renderDevice.sendVertex(new Vector2(0, 0) * yS + y2D);
                    renderDevice.sendVertex(new Vector2(0, -yy) * yS + y2D);
                    renderDevice.sendVertex(new Vector2(xx, yy) * yS + y2D);
                    renderDevice.sendVertex(new Vector2(0, 0) * yS + y2D);
                renderDevice.endPrimitive();

                renderDevice.beginPrimitive(RenderDevice.Primitive.LINE_STRIP);
                    // Z
                    renderDevice.setColor(zColor * 0.8f);
                    renderDevice.sendVertex(new Vector2(xx, yy) * zS + z2D);
                    renderDevice.sendVertex(new Vector2(-xx, yy) * zS + z2D);
                    renderDevice.sendVertex(new Vector2(xx, -yy) * zS + z2D);
                    renderDevice.sendVertex(new Vector2(-xx, -yy) * zS + z2D);
                renderDevice.endPrimitive();
            renderDevice.pop2D();
        }

        public static void axes(
            RenderDevice renderDevice,
            Color4 xColor = default(Color4),
            Color4 yColor = default(Color4),
            Color4 zColor = default(Color4),
            float scale = 1.0f)
        {
            if (xColor == default(Color4)) xColor = new Color4(Color3.red(), 1);
            if (yColor == default(Color4)) yColor = new Color4(Color3.green(), 1);
            if (zColor == default(Color4)) zColor = new Color4(Color3.blue(), 1);

            axes(CoordinateFrame.identity(), renderDevice, xColor, yColor, zColor, scale);
        }

        /**
        Provided texture coordinates are upper bounds; tex
        coords will vary from (0,0) to those bounds.
        */
        public static void rect2D(
            Rect2D rect,
            RenderDevice rd,
            Color4 color = default(Color4),
            Vector2 texCoord0 = default(Vector2),
            Vector2 texCoord1 = default(Vector2),
            Vector2 texCoord2 = default(Vector2),
            Vector2 texCoord3 = default(Vector2))
        {
            if (color == default(Color4)) color = new Color4(Color3.white(), 1);
            if (texCoord0 == default(Vector2)) texCoord0 = new Vector2(1, 1);
            if (texCoord1 == default(Vector2)) texCoord1 = new Vector2(1, 1);
            if (texCoord2 == default(Vector2)) texCoord2 = new Vector2(1, 1);
            if (texCoord3 == default(Vector2)) texCoord3 = new Vector2(1, 1);

            rect2D(rect, rd, color,
                Rect2D.xywh(0, 0, texCoord0.x, texCoord0.y),
                Rect2D.xywh(0, 0, texCoord1.x, texCoord1.y),
                Rect2D.xywh(0, 0, texCoord2.x, texCoord2.y),
                Rect2D.xywh(0, 0, texCoord3.x, texCoord3.y));
        }

        /** Leaves the renderDevice color and texture coordinates modified. Does not allow
            custom texture coordinates and only sets texture coordinate 0.*/
        public static void fastRect2D(
            Rect2D rect,
            RenderDevice rd,
            Color4 color = default(Color4))
        {
            if (color == default(Color4)) color = new Color4(Color3.white(), 1);

            rd.setColor(color);
            // Use begin primitive in case there are any 
            // lazy state changes pending.
            rd.beginPrimitive(RenderDevice.Primitive.QUADS);

                rd.setTexCoord(0, new Vector2(0, 0));
                rd.sendVertex(rect.x0y0());

                rd.setTexCoord(0, new Vector2(0, 1));
                rd.sendVertex(rect.x0y1());

                rd.setTexCoord(0, new Vector2(1, 1));
                rd.sendVertex(rect.x1y1());

                rd.setTexCoord(0, new Vector2(1, 0));
                rd.sendVertex(rect.x1y0());

            rd.endPrimitive();
        }

        /** Draws the specified rectangle, setting coordinates for
          the first four texture units as specified. */
        public static void rect2D(
            Rect2D rect,
            RenderDevice rd,
            Color4 color,
            Rect2D texCoord0,
            Rect2D texCoord1 = default(Rect2D),
            Rect2D texCoord2 = default(Rect2D),
            Rect2D texCoord3 = default(Rect2D))
        {
            if (texCoord1 == default(Rect2D)) texCoord1 = Rect2D.xywh(0, 0, 1, 1);
            if (texCoord2 == default(Rect2D)) texCoord2 = Rect2D.xywh(0, 0, 1, 1);
            if (texCoord3 == default(Rect2D)) texCoord3 = Rect2D.xywh(0, 0, 1, 1);

            Rect2D[] tx = new Rect2D[] { texCoord0, texCoord1, texCoord2, texCoord3 };

            int N = Math.Min(4, (int)rd.numTextureCoords());

            rd.pushState();
                rd.setColor(color);
                rd.beginPrimitive(RenderDevice.Primitive.QUADS);
                    for (int i = 0; i < 4; ++i)
                    {
                        for (int t = 0; t < N; ++t)
                        {
                            rd.setTexCoord((uint)t, tx[t].corner(i));
                        }
                        rd.sendVertex(rect.corner(i));
                    }
                rd.endPrimitive();
            rd.popState();
        }

        /** Draws a border of size width about the rectangle
            using polygons (since RenderDevice::LINE_STRIP doesn't 
            guarantee pixel widths). */
        public static void rect2DBorder(
            Rect2D rect,
            RenderDevice rd,
            Color4 color = default(Color4),
            float innerBorder = 0,
            float outerBorder = 1)
        {
            if (color == default(Color4)) color = new Color4(Color3.black(), 1);

            //
            //
            //   **************************************
            //   **                                  **
            //   * **                              ** *
            //   *   ******************************   *
            //   *   *                            *   *
            //
            //
            Rect2D outer = rect.border(innerBorder);
            Rect2D inner = rect.border(-outerBorder);

            rd.pushState();
                rd.setColor(color);
                rd.beginPrimitive(RenderDevice.Primitive.QUAD_STRIP);

                    for (int i = 0; i < 5; ++i)
                    {
                        int j = i % 4;
                        rd.sendVertex(outer.corner(j));
                        rd.sendVertex(inner.corner(j));
                    }

                rd.endPrimitive();
            rd.popState();
        }

        public static void frustum(
            GCamera.Frustum frustum,
            RenderDevice rd,
            Color4 color = default(Color4),
            Color4 wire = default(Color4))
        {
            if (color == default(Color4)) color = new Color4(1, .4f, .4f, 0.2f);
            if (wire == default(Color4)) wire = new Color4(Color3.black(), 1);

            // Draw edges
            rd.pushState();

                if (wire.a > 0)
                {
                    rd.setColor(wire);
                    for (int f = 0; f < frustum.faceArray.size(); ++f)
                    {
                        rd.beginPrimitive(RenderDevice.Primitive.LINE_STRIP);
                            for (int v = 0; v < 5; ++v)
                            {
                                rd.sendVertex(frustum.vertexPos[frustum.faceArray[f].vertexIndex[v % 4]]);
                            }
                        rd.endPrimitive();
                    }
                }

                rd.setDepthWrite(false);
                rd.setCullFace(RenderDevice.CullFace.CULL_NONE);
                rd.enableTwoSidedLighting();
                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                rd.setColor(color);
                rd.beginPrimitive(RenderDevice.Primitive.QUADS);
                    for (int f = 0; f < frustum.faceArray.size(); ++f)
                    {
                        rd.setNormal(frustum.faceArray[f].plane.normal());
                        for (int v = 0; v < 4; ++v)
                        {
                            rd.sendVertex(frustum.vertexPos[frustum.faceArray[f].vertexIndex[v]]);
                        }
                    }
                rd.endPrimitive();

            rd.popState();
        }

        /**
         This method is as hideously slow as it is convenient.  If you care
         about performance, make a rectangular texture (without MIPMaps) 
         and draw using that.
        */
        public static void fullScreenImage(
            GImage im,
            RenderDevice renderDevice)
        {
            Debug.debugAssert(im.channels == 3 || im.channels == 4);

            Texture texture = Texture.fromGImage("fullScreenImage", im,
                TextureFormat.AUTO, Texture.WrapMode.CLAMP,
                Texture.InterpolateMode.BILINEAR_NO_MIPMAP,
                Texture.Dimension.DIM_2D, Texture.DepthReadMode.DEPTH_NORMAL, 1.0f);

            Rect2D viewport = renderDevice.getViewport();

            renderDevice.push2D();
                renderDevice.setTexture(0, texture);
                renderDevice.setColor(new Color4(1, 1, 1, 1));
                renderDevice.beginPrimitive(RenderDevice.Primitive.QUADS);

                    renderDevice.setTexCoord(0, new Vector2(0, 1));
                    renderDevice.sendVertex(new Vector2(viewport.x0(), viewport.y0()));

                    renderDevice.setTexCoord(0, new Vector2(1, 1));
                    renderDevice.sendVertex(new Vector2(viewport.x1(), viewport.y0()));

                    renderDevice.setTexCoord(0, new Vector2(1, 0));
                    renderDevice.sendVertex(new Vector2(viewport.x1(), viewport.y1()));

                    renderDevice.setTexCoord(0, new Vector2(0, 0));
                    renderDevice.sendVertex(new Vector2(viewport.x0(), viewport.y1()));

                renderDevice.endPrimitive();
            renderDevice.pop2D();
        }
    }
}
