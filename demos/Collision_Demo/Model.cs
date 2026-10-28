using System;
using G3D;

namespace Collision_Demo
{
    public class Model
    {
        public static App app;

        protected Array<Vector3> vertex = new Array<Vector3>();
        protected Array<Vector3> normal = new Array<Vector3>();
        protected Array<uint> index = new Array<uint>();
        protected Array<Triangle> collisionTriangle = new Array<Triangle>();

        private VAR vertexVAR;
        private VAR normalVAR;
        private VAR indexVAR;

        public Sphere boundingSphere;
        public Box boundingBox;

        private static Table<string, Model> table = new Table<string, Model>();

        public static void freeModels()
        {
            table.deleteValues();
            table.clear();
        }

        public static Model getModel(string filename)
        {
            Model result;
            if (!table.get(filename, out result))
            {
                result = new Model(filename);
                table.set(filename, result);
            }
            return result;
        }

        private Model(string filename)
        {
            app.debugLog.println("Loading " + app.dataDir + "ifs/" + filename);

            BinaryInput b = new BinaryInput(app.dataDir + "ifs/" + filename, G3DEndian.G3D_LITTLE_ENDIAN);

            Debug.debugAssertM(b.getLength() > 0, "File not found: \"" + app.dataDir + "ifs/" + filename + "\"");

            double radius = 0.0;

            Vector3 min = Vector3.inf();
            Vector3 max = -Vector3.inf();

            string fmt = b.readString32();
            float version = b.readFloat32();
            string name = b.readString32();

            Debug.debugAssert(fmt == "IFS");
            Debug.debugAssert(version == 1.0f);

            string vertexHeader = b.readString32();

            int numVertices = (int)b.readUInt32();
            vertex.resize(numVertices);

            for (int v = 0; v < vertex.size(); ++v)
            {
                vertex[v] = b.readVector3() * 5.0f;

                double r = vertex[v].squaredLength();
                if (r > radius * radius)
                {
                    radius = Math.Sqrt(r);
                }

                min = min.min(vertex[v]);
                max = max.max(vertex[v]);
            }

            boundingSphere = new Sphere(Vector3.zero(), (float)radius);
            boundingBox = new Box(min, max);

            normal.resize(vertex.size());

            string triHeader = b.readString32();
            int numTris = (int)b.readUInt32();

            for (int t = 0; t < numTris; ++t)
            {
                int v0 = (int)b.readUInt32();
                int v1 = (int)b.readUInt32();
                int v2 = (int)b.readUInt32();

                Vector3 faceNormal = (vertex[v1] - vertex[v0]).cross(vertex[v2] - vertex[v0]);

                normal[v0] += faceNormal;
                normal[v1] += faceNormal;
                normal[v2] += faceNormal;

                index.append((uint)v0);
                index.append((uint)v1);
                index.append((uint)v2);
            }

            for (int n = 0; n < normal.size(); ++n)
            {
                normal[n] = normal[n].direction();
            }

            for (int i = 0; i < index.size(); i += 3)
            {
                collisionTriangle.append(new Triangle(vertex[(int)index[i]], vertex[(int)index[i + 1]], vertex[(int)index[i + 2]]));
            }

            vertexVAR = VAR.create(VAR.Type.VERTEX, vertex.size(), VAR.Usage.STATIC);
            normalVAR = VAR.create(VAR.Type.NORMAL, normal.size(), VAR.Usage.STATIC);
            indexVAR = VAR.create(VAR.Type.INDEX, index.size(), VAR.Usage.STATIC);

            Vector3[] vertexArray = new Vector3[vertex.size()];
            for (int i = 0; i < vertexArray.Length; ++i)
            {
                vertexArray[i] = vertex[i];
            }
            Vector3[] normalArray = new Vector3[normal.size()];
            for (int i = 0; i < normalArray.Length; ++i)
            {
                normalArray[i] = normal[i];
            }
            uint[] indexArray = new uint[index.size()];
            for (int i = 0; i < indexArray.Length; ++i)
            {
                indexArray[i] = index[i];
            }

            vertexVAR.setData(vertexArray);
            normalVAR.setData(normalArray);
            indexVAR.setData(indexArray);
        }

        public void render(RenderDevice rd)
        {
            rd.beginIndexedPrimitives();
            rd.setVertexArray(vertexVAR);
            rd.setNormalArray(normalVAR);
            rd.setIndexArray(indexVAR);
            rd.sendIndices(RenderDevice.Primitive.TRIANGLES, index.size());
            rd.endIndexedPrimitives();
        }

        public GameTime timeUntilCollisionWithMovingSphere(
            Sphere sphere,
            Vector3 velocity,
            GameTime timeLimit,
            ref Vector3 outLocation,
            ref Vector3 outNormal)
        {
            outLocation = Vector3.inf();

            if (!new Capsule(sphere.center, sphere.center + velocity * (float)timeLimit, sphere.radius + boundingSphere.radius).contains(boundingSphere.center))
            {
                return g3dmath.inf();
            }

            GameTime outTime = g3dmath.inf();

            for (int i = 0; i < collisionTriangle.size(); ++i)
            {
                Vector3 testLocation = Vector3.zero();
                Vector3 testNormal = Vector3.zero();
                GameTime testTime = CollisionDetection.collisionTimeForMovingSphereFixedTriangle(sphere, velocity, collisionTriangle[i], out testLocation, out testNormal);

                if (testTime < outTime)
                {
                    outTime = testTime;
                    outNormal = testNormal;
                    outLocation = testLocation;
                }
            }

            if (outTime > timeLimit)
            {
                return g3dmath.inf();
            }
            return outTime;
        }
    }
}