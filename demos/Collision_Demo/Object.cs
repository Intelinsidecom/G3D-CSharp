using System;
using System.Collections.Generic;
using G3D;

namespace Collision_Demo
{
    public class Object
    {
        protected Color3 color;

        public Object(Color3 _color)
        {
            color = _color;
        }

        public virtual void render(RenderDevice rd)
        {
        }

        public virtual GameTime timeUntilCollisionWithMovingSphere(
            Sphere sphere,
            Vector3 velocity,
            GameTime timeLimit,
            ref Vector3 outLocation,
            ref Vector3 outNormal)
        {
            return g3dmath.inf();
        }

        protected static void glSpecular(RenderDevice rd)
        {
            rd.setSpecularCoefficient(Color3.white());
            rd.setShininess(16.0f);
        }

        protected static void glDiffuse(RenderDevice rd)
        {
            rd.setSpecularCoefficient(Color3.black());
            rd.setShininess(0.0f);
        }
    }

    public class GeneralObject : Object
    {
        private Model model;
        private CoordinateFrame cframe;

        public GeneralObject(Model _model, CoordinateFrame _cframe, Color3 _color)
            : base(_color)
        {
            model = _model;
            cframe = _cframe;
        }

        public override void render(RenderDevice rd)
        {
            rd.pushState();
            glSpecular(rd);
            rd.setColor(color);
            rd.setObjectToWorldMatrix(cframe);
            model.render(rd);
            rd.popState();
        }

        public override GameTime timeUntilCollisionWithMovingSphere(
            Sphere sphere,
            Vector3 velocity,
            GameTime timeLimit,
            ref Vector3 outLocation,
            ref Vector3 outNormal)
        {
            Sphere s = cframe.toObjectSpace(sphere);
            Vector3 v = cframe.vectorToObjectSpace(velocity);

            GameTime t = model.timeUntilCollisionWithMovingSphere(s, v, timeLimit, ref outLocation, ref outNormal);

            if (t < g3dmath.inf())
            {
                outLocation = cframe.pointToWorldSpace(outLocation);
                outNormal = cframe.vectorToWorldSpace(outNormal);
            }

            return t;
        }
    }

    public class SphereObject : Object
    {
        public Sphere sphere;

        public SphereObject(Sphere s, Color3 _color)
            : base(_color)
        {
            sphere = s;
        }

        public override void render(RenderDevice rd)
        {
            glSpecular(rd);
            rd.setColor(color);
            DrawHelper.DrawSphere(rd, sphere.center, (float)sphere.radius, 40, 20);
        }

        public override GameTime timeUntilCollisionWithMovingSphere(
            Sphere movingSphere,
            Vector3 velocity,
            GameTime timeLimit,
            ref Vector3 outLocation,
            ref Vector3 outNormal)
        {
            GameTime t = CollisionDetection.collisionTimeForMovingSphereFixedSphere(movingSphere, velocity, sphere, out outLocation, out outNormal);
            if (t > timeLimit)
            {
                return g3dmath.inf();
            }
            return t;
        }
    }

    public class CapsuleObject : Object
    {
        protected Capsule capsule;

        public CapsuleObject(Capsule c, Color3 _color)
            : base(_color)
        {
            capsule = c;
        }

        public override void render(RenderDevice rd)
        {
            rd.setColor(color);
            rd.beginPrimitive(RenderDevice.Primitive.LINES);
            Vector3 cp1 = capsule.getPoint1();
            Vector3 cp2 = capsule.getPoint2();
            rd.sendVertex(cp1);
            rd.sendVertex(cp2);
            rd.endPrimitive();
        }

        public override GameTime timeUntilCollisionWithMovingSphere(
            Sphere movingSphere,
            Vector3 velocity,
            GameTime timeLimit,
            ref Vector3 outLocation,
            ref Vector3 outNormal)
        {
            GameTime t = CollisionDetection.collisionTimeForMovingSphereFixedCapsule(movingSphere, velocity, capsule, out outLocation, out outNormal);
            if (t > timeLimit)
            {
                return g3dmath.inf();
            }
            return t;
        }
    }

    public class BoxObject : Object
    {
        private Box box;

        public BoxObject(Box b, Color3 _color)
            : base(_color)
        {
            box = b;
        }

        public override void render(RenderDevice rd)
        {
            glDiffuse(rd);
            rd.setColor(color);
            rd.setObjectToWorldMatrix(CoordinateFrame.identity());
            Vector3 v0, v1, v2, v3;
            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            for (int f = 0; f < 6; ++f)
            {
                box.getFaceCorners(f, out v0, out v1, out v2, out v3);
                rd.setNormal((v1 - v0).cross(v3 - v0).direction());
                rd.sendVertex(v0);
                rd.sendVertex(v1);
                rd.sendVertex(v2);
                rd.sendVertex(v3);
            }
            rd.endPrimitive();
        }

        public override GameTime timeUntilCollisionWithMovingSphere(
            Sphere sphere,
            Vector3 velocity,
            GameTime timeLimit,
            ref Vector3 outLocation,
            ref Vector3 outNormal)
        {
            GameTime t = CollisionDetection.collisionTimeForMovingSphereFixedBox(sphere, velocity, box, out outLocation, out outNormal);
            if (t <= timeLimit)
            {
                return t;
            }
            return g3dmath.inf();
        }
    }

    public class SimSphere : SphereObject
    {
        public Vector3 velocity;

        public SimSphere(Sphere s, Vector3 _velocity, Color3 _color)
            : base(s, _color)
        {
            velocity = _velocity;
        }

        public override void render(RenderDevice rd)
        {
            base.render(rd);
        }
    }

    public static class DrawHelper
    {
        private class SphereMesh
        {
            public VAR vertexVAR;
            public VAR normalVAR;
            public VAR indexVAR;
            public int indexCount;
        }

        private static readonly Dictionary<float, SphereMesh> sphereMeshCache = new Dictionary<float, SphereMesh>();

        private static SphereMesh getSphereMesh(float radius, int slices, int stacks)
        {
            SphereMesh mesh;
            if (sphereMeshCache.TryGetValue(radius, out mesh))
            {
                return mesh;
            }

            int ringCount = stacks + 1;
            int ringSize = slices + 1;
            int vertexCount = ringCount * ringSize;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];

            for (int i = 0; i < ringCount; ++i)
            {
                float lat = (float)Math.PI * (-0.5f + (float)i / stacks);
                float z = (float)Math.Sin(lat);
                float zr = (float)Math.Cos(lat);

                for (int j = 0; j < ringSize; ++j)
                {
                    float lng = 2 * (float)Math.PI * (float)j / slices;
                    float x = (float)Math.Cos(lng);
                    float y = (float)Math.Sin(lng);

                    Vector3 normal = new Vector3(x * zr, y * zr, z);
                    int at = i * ringSize + j;
                    normals[at] = normal;
                    vertices[at] = normal * radius;
                }
            }

            List<uint> indices = new List<uint>(stacks * slices * 6);
            for (int i = 0; i < stacks; ++i)
            {
                for (int j = 0; j < slices; ++j)
                {
                    uint lower = (uint)(i * ringSize + j);
                    uint lowerNext = (uint)(i * ringSize + j + 1);
                    uint upper = (uint)((i + 1) * ringSize + j);
                    uint upperNext = (uint)((i + 1) * ringSize + j + 1);

                    indices.Add(upper);
                    indices.Add(lower);
                    indices.Add(upperNext);

                    indices.Add(lower);
                    indices.Add(lowerNext);
                    indices.Add(upperNext);
                }
            }

            mesh = new SphereMesh();
            mesh.vertexVAR = VAR.create(VAR.Type.VERTEX, vertexCount, VAR.Usage.STATIC);
            mesh.normalVAR = VAR.create(VAR.Type.NORMAL, vertexCount, VAR.Usage.STATIC);
            mesh.indexVAR = VAR.create(VAR.Type.INDEX, indices.Count, VAR.Usage.STATIC);
            mesh.vertexVAR.setData(vertices);
            mesh.normalVAR.setData(normals);
            mesh.indexVAR.setData(indices.ToArray());
            mesh.indexCount = indices.Count;

            sphereMeshCache.Add(radius, mesh);
            return mesh;
        }

        public static void DrawSphere(RenderDevice rd, Vector3 center, float radius, int slices, int stacks)
        {
            SphereMesh mesh = getSphereMesh(radius, slices, stacks);

            rd.pushState();
            rd.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
            rd.setCullFace(RenderDevice.CullFace.CULL_BACK);
            rd.setObjectToWorldMatrix(rd.getObjectToWorldMatrix() * new CoordinateFrame(center));

            rd.beginIndexedPrimitives();
            rd.setVertexArray(mesh.vertexVAR);
            rd.setNormalArray(mesh.normalVAR);
            rd.setIndexArray(mesh.indexVAR);
            rd.sendIndices(RenderDevice.Primitive.TRIANGLES, mesh.indexCount);
            rd.endIndexedPrimitives();

            rd.popState();
        }
    }
}