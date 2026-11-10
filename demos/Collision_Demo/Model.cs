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

        private VAR vertexVAR;
        private VAR normalVAR;
        private VAR indexVAR;

        public Sphere boundingSphere;
        public Box boundingBox;

        // Optimized collision data - flat arrays for cache efficiency
        private Vector3[] colV0;
        private Vector3[] colV1;
        private Vector3[] colV2;
        private Vector3[] colNormal;
        private Vector3[] colEdgeDir0;
        private Vector3[] colEdgeDir1;
        private Vector3[] colEdgeDir2;
        private double[] colEdgeMag0;
        private double[] colEdgeMag1;
        private double[] colEdgeMag2;
        private Vector3.Axis[] colPrimaryAxis;
        private AABox[] colBounds;  // Pre-computed triangle bounds for quick rejection

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

            // Pre-compute optimized collision data
            int triCount = index.size() / 3;
            colV0 = new Vector3[triCount];
            colV1 = new Vector3[triCount];
            colV2 = new Vector3[triCount];
            colNormal = new Vector3[triCount];
            colEdgeDir0 = new Vector3[triCount];
            colEdgeDir1 = new Vector3[triCount];
            colEdgeDir2 = new Vector3[triCount];
            colEdgeMag0 = new double[triCount];
            colEdgeMag1 = new double[triCount];
            colEdgeMag2 = new double[triCount];
            colPrimaryAxis = new Vector3.Axis[triCount];
            colBounds = new AABox[triCount];

            // Reusable candidate array (int[] + count) to avoid allocations
            int[] candidates = new int[triCount];

            for (int i = 0; i < index.size(); i += 3)
            {
                int triIdx = i / 3;
                uint i0 = index[i];
                uint i1 = index[i + 1];
                uint i2 = index[i + 2];

                Vector3 v0 = vertex[(int)i0];
                Vector3 v1 = vertex[(int)i1];
                Vector3 v2 = vertex[(int)i2];

                // Compute all collision data inline (Triangle methods are internal)
                colV0[triIdx] = v0;
                colV1[triIdx] = v1;
                colV2[triIdx] = v2;

                Vector3 e0 = v1 - v0;
                Vector3 e1 = v2 - v1;
                Vector3 e2 = v0 - v2;

                double m0 = e0.magnitude();
                double m1 = e1.magnitude();
                double m2 = e2.magnitude();

                colEdgeMag0[triIdx] = m0;
                colEdgeMag1[triIdx] = m1;
                colEdgeMag2[triIdx] = m2;

                Vector3 n0, n1, n2;
                if (m0 == 0) n0 = Vector3.zero(); else n0 = e0 / (float)m0;
                if (m1 == 0) n1 = Vector3.zero(); else n1 = e1 / (float)m1;
                if (m2 == 0) n2 = Vector3.zero(); else n2 = e2 / (float)m2;

                colEdgeDir0[triIdx] = n0;
                colEdgeDir1[triIdx] = n1;
                colEdgeDir2[triIdx] = n2;

                Vector3 faceNormal = e0.cross(e2);
                faceNormal = faceNormal.direction();
                colNormal[triIdx] = faceNormal;

                // Primary axis from plane normal
                colPrimaryAxis[triIdx] = faceNormal.primaryAxis();

                // Pre-compute triangle bounds
                Vector3 lo = v0;
                Vector3 hi = v0;
                lo = lo.min(v1); hi = hi.max(v1);
                lo = lo.min(v2); hi = hi.max(v2);
                colBounds[triIdx] = new AABox(lo, hi);
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

            // Quick bounding sphere test
            if (!new Capsule(sphere.center, sphere.center + velocity * (float)timeLimit, sphere.radius + boundingSphere.radius).contains(boundingSphere.center))
            {
                return g3dmath.inf();
            }

            // Query: swept capsule bounds intersect triangle bounds
            Capsule sweptCapsule = new Capsule(sphere.center, sphere.center + velocity * (float)timeLimit, sphere.radius);
            AABox queryBounds;
            sweptCapsule.getBounds(out queryBounds);

            // Find candidate triangles by simple bounds test (reusing array)
            int candidateCount = 0;
            int[] candidates = new int[colBounds.Length];

            for (int i = 0; i < colBounds.Length; ++i)
            {
                if (queryBounds.intersects(colBounds[i]))
                {
                    candidates[candidateCount++] = i;
                }
            }

            GameTime outTime = g3dmath.inf();

            // Test only candidate triangles using pre-computed data
            for (int ci = 0; ci < candidateCount; ++ci)
            {
                int triIdx = candidates[ci];

                Vector3 testLocation = Vector3.zero();
                Vector3 testNormal = Vector3.zero();

                GameTime testTime = CollisionDetection.collisionTimeForMovingSphereFixedTriangle(
                    sphere, velocity,
                    colV0[triIdx], colV1[triIdx], colV2[triIdx],
                    colNormal[triIdx],
                    colEdgeDir0[triIdx], colEdgeDir1[triIdx], colEdgeDir2[triIdx],
                    colEdgeMag0[triIdx], colEdgeMag1[triIdx], colEdgeMag2[triIdx],
                    colPrimaryAxis[triIdx],
                    out testLocation, out testNormal);

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