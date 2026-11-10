/**
 @file MeshAlg.h

 Indexed Mesh algorithms.

 @maintainer Morgan McGuire, matrix@graphics3d.com

 @created 2003-09-14
 @edited  2004-12-26
*/

using System;

namespace G3D
{
    /**
      Indexed <B>mesh alg</B>orithms.  You have to build your own mesh class.
      <P>
      No mesh class is provided with G3D because there isn't an "ideal" 
      mesh format-- one application needs keyframed animation, another
      skeletal animation, a third texture coordinates, a fourth
      cannot precompute information, etc.  Instead of compromising, this
      class implements the hard parts of mesh computation and you can write
      your own ideal mesh class on top of it.
      */
    public partial class MeshAlg
    {
        public enum Primitive
        {
            LINES,
            LINE_STRIP,
            TRIANGLES,
            TRIANGLE_STRIP,
            TRIANGLE_FAN,
            QUADS,
            QUAD_STRIP,
            POINTS
        }

        /**
            Adjacency information for a vertex.
            Does not contain the vertex position or normal,
            which are stored in the MeshAlg::Geometry object.
            <CODE>Vertex</CODE>s must be stored in an array
            parallel to (indexed in the same way as) 
            MeshAlg::Geometry::vertexArray.
            */
        public class Vertex
        {
            public Vertex() { }

            /**
             Array of edges adjacent to this vertex.
             Let e = edgeIndex[i].  
             edge[(e >= 0) ? e : ~e].vertexIndex[0] == this
             vertex index.
             
             Edges may be listed multiple times if they are
             degenerate.
             */
            public Array<int> edgeIndex = new Array<int>();

            /**
             Returns true if e or ~e is in the edgeIndex list.
             */
            public bool inEdge(int e)
            {
                return edgeIndex.contains(~e) || edgeIndex.contains(e);
            }

            /**
             Array of faces containing this vertex.  Faces
             may be listed multiple times if they are degenerate.
            */
            public Array<int> faceIndex = new Array<int>();

            public bool inFace(int f)
            {
                Debug.debugAssert(f >= 0);
                return faceIndex.contains(f);
            }
        }

        /**
         Oriented, indexed triangle.
         */
        public class Face
        {
            public Face()
            {
                for (int i = 0; i < 3; ++i)
                {
                    edgeIndex[i] = 0;
                    vertexIndex[i] = 0;
                }
            }

            /**
             Used by Edge::faceIndex to indicate a missing face.
             This is a large negative value.
             */
            public const int NONE = int.MinValue;

            /**
             Vertices in the face in counter-clockwise order.
             Degenerate faces may include the same vertex multiple times.
             */
            public int[] vertexIndex = new int[3];

            public bool containsVertex(int v)
            {
                return contains(vertexIndex, 3, v);
            }

            /**
             Edge indices in counter-clockwise order.  Edges are
             undirected, so it is important to know which way 
             each edge is pointing in a face.  This is encoded
             using negative indices.
             
             If <CODE>edgeIndex[i] >= 0</CODE> then this face
             contains the directed edge
             between vertex indices  
             <CODE>edgeArray[face.edgeIndex[i]].vertexIndex[0]</CODE>
             and
             <CODE>edgeArray[face.edgeIndex[i]].vertexIndex[1]</CODE>.
             
             If <CODE>edgeIndex[i] < 0</CODE> then 
             <CODE>~edgeIndex[i]</CODE> (i.e. the two's
             complement of) is used and this face contains the directed
             edge between vertex indices
             <CODE>edgeArray[~face.edgeIndex[i]].vertexIndex[0]</CODE>
             and
             <CODE>edgeArray[~face.edgeIndex[i]].vertexIndex[1]</CODE>.

             Degenerate faces may include the same edge multiple times.
             */
            // Temporarily takes on the value Face::NONE during adjacency
            // computation to indicate an edge that has not yet been assigned.
            public int[] edgeIndex = new int[3];

            public bool containsEdge(int e)
            {
                if (e < 0)
                {
                    e = ~e;
                }
                return contains(edgeIndex, 3, e) || contains(edgeIndex, 3, ~e);
            }

            /** Contains the forward edge e if e >= 0 and the backward edge 
                ~e otherwise. */
            public bool containsDirectedEdge(int e)
            {
                return contains(edgeIndex, 3, e);
            }
        }

        /** Oriented, indexed edge */
        public class Edge
        {
            public Edge()
            {
                for (int i = 0; i < 2; ++i)
                {
                    vertexIndex[i] = 0;
                    // Negative face indices are faces that don't exist
                    faceIndex[i] = -1;
                }
            }

            /** Degenerate edges may include the same vertex times. */
            public int[] vertexIndex = new int[2];

            public bool containsVertex(int v)
            {
                return contains(vertexIndex, 2, v);
            }

            /**
             The edge is directed <B>forward</B> in face 0
             <B>backward</B> in face 1. Face index of MeshAlg::Face::NONE
             indicates a boundary (a.k.a. crack, broken) edge.
             */
            public int[] faceIndex = new int[2];

            /** Returns true if f is contained in the faceIndex array in either slot.
                To see if it is forward in that face, just check edge.faceIndex[0] == f.*/
            public bool inFace(int f)
            {
                return contains(faceIndex, 2, f);
            }

            /**
             Returns true if either faceIndex is NONE.
             */
            public bool boundary()
            {
                return (faceIndex[0] == Face.NONE) ||
                       (faceIndex[1] == Face.NONE);
            }

            /** @deprecated Use Edge::boundary */
            public bool broken()
            {
                return boundary();
            }

            /**
             Returns the reversed edge.
             */
            public Edge reverse()
            {
                Edge e = new Edge();
                e.vertexIndex[0] = vertexIndex[1];
                e.vertexIndex[1] = vertexIndex[0];
                e.faceIndex[0] = faceIndex[1];
                e.faceIndex[1] = faceIndex[0];
                return e;
            }
        }

        /**
         Convenient for passing around the data that changes under
         animation. The faces and edges are needed to interpret 
         these values.
         */
        public class Geometry
        {
            public Array<Vector3> vertexArray = new Array<Vector3>();
            public Array<Vector3> normalArray = new Array<Vector3>();

            /**
             Assignment is optimized using SSE.
             */
            public Geometry copyFrom(Geometry src)
            {
                if (object.ReferenceEquals(src, this))
                {
                    return this;
                }

                vertexArray.resize(src.vertexArray.size());
                normalArray.resize(src.normalArray.size());

                for (int i = 0; i < vertexArray.size(); ++i)
                {
                    vertexArray[i] = src.vertexArray[i];
                }

                for (int i = 0; i < normalArray.size(); ++i)
                {
                    normalArray[i] = src.normalArray[i];
                }

                return this;
            }


            public void clear()
            {
                vertexArray.clear();
                normalArray.clear();
            }
        }

        private static bool contains(int[] array, int size, int value)
        {
            for (int i = 0; i < size; ++i)
            {
                if (array[i] == value)
                {
                    return true;
                }
            }
            return false;
        }

        /**
         Computes some basic mesh statistics including: min, max mean and median, 
         edge lengths; and min, mean, median, and max face area.

         @param vertexGeometry      %Vertex positions to use when deciding colocation.
         @param indexArray          Order to traverse vertices to make triangles
         @param minEdgeLength       Minimum edge length
         @param meanEdgeLength      Mean edge length
         @param medianEdgeLength    Median edge length
         @param maxEdgeLength       Max edge length
         @param minFaceArea         Minimum face area
         @param meanFaceArea        Mean face area
         @param medianFaceArea      Median face area
         @param maxFaceArea         Max face area
         */
        public static void computeAreaStatistics(
            Array<Vector3> vertexArray,
            Array<int> indexArray,
            out double minEdgeLength,
            out double meanEdgeLength,
            out double medianEdgeLength,
            out double maxEdgeLength,
            out double minFaceArea,
            out double meanFaceArea,
            out double medianFaceArea,
            out double maxFaceArea)
        {
            Debug.debugAssert(indexArray.size() % 3 == 0);

            Array<double> area = new Array<double>(indexArray.size() / 3);
            Array<double> magnitude = new Array<double>(indexArray.size());

            for (int i = 0; i < indexArray.size(); i += 3)
            {
                Vector3 v0 = vertexArray[indexArray[i]];
                Vector3 v1 = vertexArray[indexArray[i + 1]];
                Vector3 v2 = vertexArray[indexArray[i + 2]];

                area[i / 3] = (v1 - v0).cross(v2 - v0).magnitude() / 2.0;
                magnitude[i] = (v1 - v0).magnitude();
                magnitude[i + 1] = (v2 - v1).magnitude();
                magnitude[i + 2] = (v0 - v2).magnitude();
            }

            area.sort();
            magnitude.sort();

            minEdgeLength = Math.Max(0.0, magnitude[0]);
            maxEdgeLength = Math.Max(0.0, magnitude.last());
            medianEdgeLength = Math.Max(0.0, magnitude[magnitude.size() / 2]);
            meanEdgeLength = 0;
            for (int i = 0; i < magnitude.size(); ++i)
            {
                meanEdgeLength += magnitude[i];
            }
            meanEdgeLength /= magnitude.size();

            minFaceArea = Math.Max(0.0, area[0]);
            maxFaceArea = Math.Max(0.0, area.last());
            medianFaceArea = Math.Max(0.0, area[area.size() / 2]);
            meanFaceArea = 0;
            for (int i = 0; i < area.size(); ++i)
            {
                meanFaceArea += area[i];
            }
            meanFaceArea /= area.size();

            // Make sure round-off hasn't pushed values less than zero
            meanFaceArea = Math.Max(0.0, meanFaceArea);
            meanEdgeLength = Math.Max(0.0, meanEdgeLength);
        }

        /**
         Computes tangent and binormal vectors,
         which provide a (mostly) consistent
         parameterization over the surface for
         effects like bump mapping.  In the resulting coordinate frame, 
         T = x (varies with texture s coordinate), B = y (varies with negative texture t coordinate), 
         and N = z for a right-handed coordinate frame.  If a billboard is vertical on the screen
         in view of the camera, the tangent space matches the camera's coordinate frame.

         The vertex, texCoord, tangent, and binormal
         arrays are parallel arrays.

         The resulting tangent and binormal might not be exactly
         perpendicular to each other.  They are guaranteed to
         be perpendicular to the normal.

         @cite Max McGuire
        */
        public static void computeTangentSpaceBasis(
            Array<Vector3> vertexArray,
            Array<Vector2> texCoordArray,
            Array<Vector3> vertexNormalArray,
            Array<Face> faceArray,
            Array<Vector3> tangent,
            Array<Vector3> binormal)
        {
            Debug.debugAssertM(faceArray.size() != 0, "Unable to calculate valid tangent space without faces.");

            // The three vertices and texCoords of each face
            Vector3[] position = new Vector3[3];
            Vector2[] texCoord = new Vector2[3];
            Vector3 t, b;

            tangent.resize(vertexArray.size());
            binormal.resize(vertexArray.size());

            // Zero the output arrays.
            for (int i = 0; i < tangent.size(); ++i)
            {
                tangent[i] = Vector3.zero();
            }
            for (int i = 0; i < binormal.size(); ++i)
            {
                binormal[i] = Vector3.zero();
            }

            // Iterate over faces, computing the tangent vectors for each 
            // vertex.  Accumulate those into the tangent and binormal arrays
            // and then orthonormalize at the end.

            for (int f = 0; f < faceArray.size(); ++f)
            {
                Face face = faceArray[f];

                for (int v = 0; v < 3; ++v)
                {
                    int i = face.vertexIndex[v];
                    position[v] = vertexArray[i];
                    texCoord[v] = texCoordArray[i];
                }

                Vector3 faceNormal = (position[1] - position[0]).cross(position[2] - position[0]).direction();
                computeTangentVectors(faceNormal, position, texCoord, out t, out b);

                for (int v = 0; v < 3; ++v)
                {
                    int i = face.vertexIndex[v];
                    tangent[i] += t;
                    binormal[i] += b;
                }
            }

            // Normalize the basis vectors
            for (int v = 0; v < vertexArray.size(); ++v)
            {
                // Remove the component parallel to the normal
                Vector3 N = vertexNormalArray[v];
                Debug.debugAssertM(N.isUnit() || N.isZero(), "Input normals must have unit length");

                tangent[v] -= tangent[v].dot(N) * N;
                binormal[v] -= binormal[v].dot(N) * N;

                // Normalize
                tangent[v] = tangent[v].directionOrZero();
                binormal[v] = binormal[v].directionOrZero();

                // Note that the tangent and binormal might not be perpendicular anymore
            }
        }

        protected static void computeTangentVectors(
            Vector3 normal,
            Vector3[] position,
            Vector2[] texCoord,
            out Vector3 tangent,
            out Vector3 binormal)
        {
            Vector3[] v = new Vector3[3];
            Vector2[] t = new Vector2[3];

            // TODO: don't need the copy
            // Make a copy so that we can sort
            for (int i = 0; i < 3; ++i)
            {
                v[i] = position[i];
                t[i] = texCoord[i];
            }

            /////////////////////////////////////////////////
            // Begin by computing the tangent

            // Bubble sort the vertices by decreasing texture coordinate y.
            if (t[0].y < t[1].y)
            {
                swap(v, 0, 1);
                swap(t, 0, 1);
            }

            // t0 >= t1

            if (t[0].y < t[2].y)
            {
                swap(v, 0, 2);
                swap(t, 0, 2);
            }

            // t0 >= t2, t0 >= t1

            if (t[1].y < t[2].y)
            {
                swap(v, 1, 2);
                swap(t, 1, 2);
            }

            // t0 >= t1 >= t2

            float amount;

            // Compute the direction of constant y.
            if (g3dmath.fuzzyEq(t[2].y, t[0].y))
            {
                // Degenerate case-- the texture coordinates do not vary across this 
                // triangle.
                amount = 1.0f;
            }
            else
            {
                // Solve lerp(t[0].y, t[2].y, amount) = t[1].y for amount:
                //
                // t0 + (t2 - t0) * a = t1
                // a = (t1 - t0) / (t2 - t0)

                amount = (t[1].y - t[0].y) / (t[2].y - t[0].y);
            }

            tangent = v[0].lerp(v[2], amount) - v[1];

            // Make sure the tangent points in the right direction and is 
            // perpendicular to the normal.
            if (g3dmath.lerp(t[0].x, t[2].x, amount) < t[1].x)
            {
                tangent = -tangent;
            }

            // TODO: do we need this?  We take this component off
            // at the end anyway
            tangent -= tangent.dot(normal) * normal;

            // Normalize the tangent so it contributes
            // equally at the vertex (TODO: do we need this?)
            if (g3dmath.fuzzyEq(tangent.magnitude(), 0.0))
            {
                tangent = Vector3.unitX();
            }
            else
            {
                tangent = tangent.direction();
            }

            //////////////////////////////////////////////////
            // Now compute the binormal (same code, but in x)

            // Sort the vertices by texture coordinate x.
            if (t[0].x < t[1].x)
            {
                swap(v, 0, 1);
                swap(t, 0, 1);
            }

            if (t[0].x < t[2].x)
            {
                swap(v, 0, 2);
                swap(t, 0, 2);
            }

            if (t[1].x < t[2].x)
            {
                swap(v, 1, 2);
                swap(t, 1, 2);
            }

            // Compute the direction of constant x.
            if (g3dmath.fuzzyEq(t[2].x, t[0].x))
            {
                amount = 1.0f;
            }
            else
            {
                amount = (t[1].x - t[0].x) / (t[2].x - t[0].x);
            }

            binormal = v[0].lerp(v[2], amount) - v[1];

            // Make sure the binormal points in the right direction and is 
            // perpendicular to the normal.
            if (g3dmath.lerp(t[0].y, t[2].y, amount) < t[1].y)
            {
                binormal = -binormal;
            }

            binormal -= binormal.dot(normal) * normal;

            // Normalize the binormal so that it contributes
            // an equal amount to the per-vertex value (TODO: do we need this? 
            // Nelson Max showed that we don't for computing per-vertex normals)
            if (g3dmath.fuzzyEq(binormal.magnitude(), 0.0))
            {
                binormal = Vector3.unitZ();
            }
            else
            {
                binormal = binormal.direction();
            }

            // This computation gives the opposite convention of what we want.
            binormal = -binormal;
        }

        private static void swap(Vector3[] v, int i, int j)
        {
            Vector3 temp = v[i];
            v[i] = v[j];
            v[j] = temp;
        }

        private static void swap(Vector2[] v, int i, int j)
        {
            Vector2 temp = v[i];
            v[i] = v[j];
            v[j] = temp;
        }

        /** @deprecated */
        public static void computeNormals(
            Array<Vector3> vertexArray,
            Array<Face> faceArray,
            Array<Array<int>> adjacentFaceArray,
            Array<Vector3> vertexNormalArray,
            Array<Vector3> faceNormalArray)
        {
            // Construct a fake vertex array for backwards compatibility
            Array<Vertex> fakeVertexArray = new Array<Vertex>(adjacentFaceArray.size());
            for (int v = 0; v < fakeVertexArray.size(); ++v)
            {
                fakeVertexArray[v] = new Vertex();
            }

            for (int v = 0; v < adjacentFaceArray.size(); ++v)
            {
                fakeVertexArray[v].faceIndex = new Array<int>(adjacentFaceArray[v]);
                // We leave out the edges because they aren't used to compute normals
            }

            computeNormals(vertexArray, faceArray, fakeVertexArray,
                vertexNormalArray, faceNormalArray);
        }

        /**
         Vertex normals are weighted by the area of adjacent faces.
         Nelson Max showed this is superior to uniform weighting for
         general meshes in jgt.

         @param vertexNormalArray Output. Unit length
         @param faceNormalArray Output.   Degenerate faces produce zero magnitude normals. Unit length
         */
        public static void computeNormals(
            Array<Vector3> vertexGeometry,
            Array<Face> faceArray,
            Array<Vertex> vertexArray,
            Array<Vector3> vertexNormalArray,
            Array<Vector3> faceNormalArray)
        {
            // Face normals (not unit length)
            faceNormalArray.resize(faceArray.size());
            for (int f = 0; f < faceArray.size(); ++f)
            {
                Face face = faceArray[f];

                Vector3[] vertex = new Vector3[3];
                for (int j = 0; j < 3; ++j)
                {
                    vertex[j] = vertexGeometry[face.vertexIndex[j]];
                }

                faceNormalArray[f] = (vertex[1] - vertex[0]).cross(vertex[2] - vertex[0]);
            }

            // Per-vertex normals, computed by averaging
            vertexNormalArray.resize(vertexGeometry.size());
            for (int v = 0; v < vertexNormalArray.size(); ++v)
            {
                Vector3 sum = Vector3.zero();
                for (int k = 0; k < vertexArray[v].faceIndex.size(); ++k)
                {
                    int f = vertexArray[v].faceIndex[k];
                    sum += faceNormalArray[f];
                }
                vertexNormalArray[v] = sum.directionOrZero();
            }

            for (int f = 0; f < faceArray.size(); ++f)
            {
                faceNormalArray[f] = faceNormalArray[f].directionOrZero();
            }
        }

        /** Computes unit length normals in place using the other computeNormals methods.
        If you already have a face array use another method; it will be faster. */
        public static void computeNormals(
            Geometry geometry,
            Array<int> indexArray)
        {
            Array<Face> faceArray = new Array<Face>();
            Array<Vertex> vertexArray = new Array<Vertex>();
            Array<Edge> edgeArray = new Array<Edge>();
            Array<Vector3> faceNormalArray = new Array<Vector3>();

            computeAdjacency(geometry.vertexArray, indexArray, faceArray, edgeArray, vertexArray);

            computeNormals(geometry.vertexArray, faceArray, vertexArray,
                           geometry.normalArray, faceNormalArray);
        }

        /**
         Computes face normals only.  Significantly faster (especially if
         normalize is false) than computeNormals.
         */
        public static void computeFaceNormals(
            Array<Vector3> vertexArray,
            Array<Face> faceArray,
            Array<Vector3> faceNormals,
            bool normalize = true)
        {
            faceNormals.resize(faceArray.size());

            for (int f = 0; f < faceArray.size(); ++f)
            {
                Face face = faceArray[f];

                Vector3 v0 = vertexArray[face.vertexIndex[0]];
                Vector3 v1 = vertexArray[face.vertexIndex[1]];
                Vector3 v2 = vertexArray[face.vertexIndex[2]];

                faceNormals[f] = (v1 - v0).cross(v2 - v0);
            }

            if (normalize)
            {
                for (int f = 0; f < faceArray.size(); ++f)
                {
                    faceNormals[f] = faceNormals[f].direction();
                }
            }
        }

        /**
         Classifies each face as a backface or a front face relative
         to the observer point P (which is at infinity when P.w = 0).
         A face with normal exactly perpendicular to the observer vector
         may be classified as either a front or a back face arbitrarily.
         */
        public static void identifyBackfaces(
            Array<Vector3> vertexArray,
            Array<Face> faceArray,
            Vector4 HP,
            Array<bool> backface)
        {
            Vector3 P = HP.xyz();

            backface.resize(faceArray.size());

            if (g3dmath.fuzzyEq(HP.w, 0.0))
            {
                // Infinite case
                for (int f = faceArray.size() - 1; f >= 0; --f)
                {
                    Face face = faceArray[f];

                    Vector3 v0 = vertexArray[face.vertexIndex[0]];
                    Vector3 v1 = vertexArray[face.vertexIndex[1]];
                    Vector3 v2 = vertexArray[face.vertexIndex[2]];

                    Vector3 N = (v1 - v0).cross(v2 - v0);

                    backface[f] = N.dot(P) < 0;
                }
            }
            else
            {
                // Finite case
                for (int f = faceArray.size() - 1; f >= 0; --f)
                {
                    Face face = faceArray[f];

                    Vector3 v0 = vertexArray[face.vertexIndex[0]];
                    Vector3 v1 = vertexArray[face.vertexIndex[1]];
                    Vector3 v2 = vertexArray[face.vertexIndex[2]];

                    Vector3 N = (v1 - v0).cross(v2 - v0);

                    backface[f] = N.dot(P - v0) < 0;
                }
            }
        }

        /** A faster version of identifyBackfaces for the case where
            face normals have already been computed */
        public static void identifyBackfaces(
            Array<Vector3> vertexArray,
            Array<Face> faceArray,
            Vector4 HP,
            Array<bool> backface,
            Array<Vector3> faceNormals)
        {
            Vector3 P = HP.xyz();

            backface.resize(faceArray.size());

            if (g3dmath.fuzzyEq(HP.w, 0.0))
            {
                // Infinite case
                for (int f = faceArray.size() - 1; f >= 0; --f)
                {
                    Vector3 N = faceNormals[f];
                    backface[f] = N.dot(P) < 0;
                }
            }
            else
            {
                // Finite case
                for (int f = faceArray.size() - 1; f >= 0; --f)
                {
                    Face face = faceArray[f];
                    Vector3 v0 = vertexArray[face.vertexIndex[0]];
                    Vector3 N = faceNormals[f];

                    backface[f] = N.dot(P - v0) < 0;
                }
            }
        }

        /**
         Counts the number of edges (in an edge array returned from 
         MeshAlg::computeAdjacency) that have only one adjacent face.
         */
        public static int countBoundaryEdges(Array<Edge> edgeArray)
        {
            int b = 0;

            for (int i = 0; i < edgeArray.size(); ++i)
            {
                if ((edgeArray[i].faceIndex[0] == Face.NONE) !=
                    (edgeArray[i].faceIndex[1] == Face.NONE))
                {
                    ++b;
                }
            }

            return b;
        }

        /** @deprecated Use countBoundaryEdges */
        public static int countBrokenEdges(Array<Edge> edgeArray)
        {
            return countBoundaryEdges(edgeArray);
        }

        /**
         Generates an array of integers from start to start + n - 1 that have run numbers
         in series then omit the next skip before the next run.  Useful for turning
         a triangle list into an indexed face set.

         Example: 
         <PRE>
           createIndexArray(10, x);
           // x = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9] 

           createIndexArray(5, x, 2);
           // x = [2, 3, 4, 5, 6, 7] 

           createIndexArray(6, x, 0, 2, 1);
           // x = [0, 1, 3, 4, 6, 7]
         </PRE>
         */
        public static void createIndexArray(
            int n,
            Array<int> array,
            int start = 0,
            int run = 1,
            int skip = 0)
        {
            Debug.debugAssert(skip >= 0);
            Debug.debugAssert(run >= 0);

            array.resize(n);
            if (skip == 0)
            {
                for (int i = 0; i < n; ++i)
                {
                    array[i] = start + i;
                }
            }
            else
            {
                int rcount = 0;
                int j = start;
                for (int i = 0; i < n; ++i)
                {
                    array[i] = j;

                    ++j;
                    ++rcount;

                    if (rcount == run)
                    {
                        rcount = 0;
                        j += skip;
                    }
                }
            }
        }

        /**
         Computes a conservative, near-optimal axis aligned bounding box and sphere. 

          @cite The bounding sphere uses the method from J. Ritter. An effcient bounding sphere. In Andrew S. Glassner, editor, Graphics Gems. Academic Press, Boston, MA, 1990.

        */
        public static void computeBounds(Array<Vector3> vertex, ref Box box, ref Sphere sphere)
        {
            Vector3 xmin, xmax, ymin, ymax, zmin, zmax;

            // FIRST PASS: find 6 minima/maxima points
            xmin = new Vector3();
            xmax = new Vector3();
            ymin = new Vector3();
            ymax = new Vector3();
            zmin = new Vector3();
            zmax = new Vector3();

            xmin.x = ymin.y = zmin.z = (float)g3dmath.inf();
            xmax.x = ymax.y = zmax.z = (float)-g3dmath.inf();

            for (int v = 0; v < vertex.size(); ++v)
            {
                Vector3 ver = vertex[v];

                if (ver.x < xmin.x)
                {
                    xmin = ver;
                }

                if (ver.x > xmax.x)
                {
                    xmax = ver;
                }

                if (ver.y < ymin.y)
                {
                    ymin = ver;
                }

                if (ver.y > ymax.y)
                {
                    ymax = ver;
                }

                if (ver.z < zmin.z)
                {
                    zmin = ver;
                }

                if (ver.z > zmax.z)
                {
                    zmax = ver;
                }
            }

            // Set points dia1 & dia2 to the maximally separated pair
            Vector3 dia1 = xmin;
            Vector3 dia2 = xmax;
            {
                // Set xspan = distance between the 2 points xmin & xmax (squared)
                double xspan = (xmax - xmin).squaredMagnitude();

                // Same for y & z spans
                double yspan = (ymax - ymin).squaredMagnitude();
                double zspan = (zmax - zmin).squaredMagnitude();

                double maxspan = xspan;

                if (yspan > maxspan)
                {
                    maxspan = yspan;
                    dia1 = ymin;
                    dia2 = ymax;
                }

                if (zspan > maxspan)
                {
                    maxspan = zspan;
                    dia1 = zmin;
                    dia2 = zmax;
                }
            }

            // dia1, dia2 is a diameter of initial sphere

            // calc initial center
            Vector3 center = (dia1 + dia2) / 2.0f;

            // calculate initial radius^2 and radius
            Vector3 d = dia2 - sphere.center;

            double radSq = d.squaredMagnitude();
            double rad = Math.Sqrt(radSq);

            // SECOND PASS: increment current sphere
            double old_to_p, old_to_new;

            for (int v = 0; v < vertex.size(); ++v)
            {
                Vector3 ver = vertex[v];

                d = ver - center;

                double old_to_p_sq = d.squaredMagnitude();

                // do r^2 test first 
                if (old_to_p_sq > radSq)
                {
                    // this point is outside of current sphere
                    old_to_p = Math.Sqrt(old_to_p_sq);

                    // calc radius of new sphere
                    rad = (rad + old_to_p) / 2.0;

                    // for next r^2 compare
                    radSq = rad * rad;
                    old_to_new = old_to_p - rad;

                    // calc center of new sphere
                    center = (rad * center + old_to_new * ver) / (float)old_to_p;
                }
            }

            Vector3 min = new Vector3(xmin.x, ymin.y, zmin.z);
            Vector3 max = new Vector3(xmax.x, ymax.y, zmax.z);

            box = new Box(min, max);

            double boxRadSq = (max - min).squaredMagnitude() * 0.25;

            if (boxRadSq >= radSq)
            {
                sphere = new Sphere(center, (float)rad);
            }
            else
            {
                sphere = new Sphere((max + min) * 0.5f, (float)Math.Sqrt(boxRadSq));
            }
        }

        /** Computes bounds for a subset of the vertices.  It is ok if vertices appear more than once in the index array. */
        public static void computeBounds(Array<Vector3> vertex, Array<int> index, ref Box box, ref Sphere sphere)
        {
            Array<Vector3> newArray = new Array<Vector3>(index.size());
            for (int i = 0; i < index.size(); ++i)
            {
                newArray[i] = vertex[index[i]];
            }
            computeBounds(newArray, ref box, ref sphere);
        }

        /** Converts quadlist (QUADS), tristrip(TRIANGLE_STRIP), and quadstrip (QUAD_STRIP)indices into triangle list (TRIANGLES)  indices. */
        public static void toIndexedTriList(
            Array<int> inIndices,
            Primitive inType,
            Array<int> outIndices)
        {
            toIndexedTriList<int>(inIndices, inType, outIndices);
        }

        public static void toIndexedTriList<IndexType>(
            Array<IndexType> inIndices,
            Primitive inType,
            Array<IndexType> outIndices) where IndexType : struct
        {
            bool supported = inType == Primitive.TRIANGLE_STRIP ||
                inType == Primitive.QUADS ||
                inType == Primitive.QUAD_STRIP;
            if (!supported)
            {
                throw new ArgumentException("Only triangle strips, quad lists, and quad strips can be converted to triangle lists.", "inType");
            }
            Debug.debugAssert(supported);

            int inSize = inIndices.size();

            if (inType == Primitive.TRIANGLE_STRIP && inSize < 3)
            {
                throw new ArgumentException("A triangle strip requires at least three indices.", "inIndices");
            }
            if (inType == Primitive.QUADS && inSize < 4)
            {
                throw new ArgumentException("A quad list requires at least four indices.", "inIndices");
            }
            if (inType == Primitive.QUAD_STRIP && (inSize < 4 || (inSize & 1) != 0))
            {
                throw new ArgumentException("A quad strip requires an even number of at least four indices.", "inIndices");
            }

            switch (inType)
            {
            case Primitive.TRIANGLE_STRIP:
                {
                    Debug.debugAssert(inSize >= 3);

                    outIndices.resize((inSize - 2) * 3);

                    bool atEven = false;
                    for (int i = 0, outIndex = 0; i < (inSize - 2); ++i, outIndex += 3)
                    {
                        if (atEven)
                        {
                            outIndices[outIndex] = inIndices[i + 1];
                            outIndices[outIndex + 1] = inIndices[i];
                            outIndices[outIndex + 2] = inIndices[i + 2];
                            atEven = false;
                        }
                        else
                        {
                            outIndices[outIndex] = inIndices[i];
                            outIndices[outIndex + 1] = inIndices[i + 1];
                            outIndices[outIndex + 2] = inIndices[i + 2];
                            atEven = true;
                        }
                    }

                    break;
                }
            case Primitive.QUADS:
                {
                    Debug.debugAssert(inIndices.size() >= 4);

                    outIndices.resize((int)(inSize * 1.5));

                    for (int i = 0, outIndex = 0; i <= (inSize - 4); i += 4, outIndex += 6)
                    {
                        outIndices[outIndex] = inIndices[i];
                        outIndices[outIndex + 1] = inIndices[i + 1];
                        outIndices[outIndex + 2] = inIndices[i + 3];
                        outIndices[outIndex + 3] = inIndices[i + 1];
                        outIndices[outIndex + 4] = inIndices[i + 2];
                        outIndices[outIndex + 5] = inIndices[i + 3];
                    }

                    break;
                }
            case Primitive.QUAD_STRIP:
                {
                    Debug.debugAssert(inSize >= 4);

                    outIndices.resize((inSize - 2) * 3);

                    for (int i = 0, outIndex = 0; i < (inSize - 2); i += 2, outIndex += 6)
                    {
                        outIndices[outIndex] = inIndices[i];
                        outIndices[outIndex + 1] = inIndices[i + 1];
                        outIndices[outIndex + 2] = inIndices[i + 2];
                        outIndices[outIndex + 3] = inIndices[i + 2];
                        outIndices[outIndex + 4] = inIndices[i + 1];
                        outIndices[outIndex + 5] = inIndices[i + 3];
                    }
                    break;
                }
            default:
                throw new ArgumentException("Only triangle strips, quad lists, and quad strips can be converted to triangle lists.", "inType");
            }
        }

        /**
         Helper for computeAdjacency.  If a directed edge with index e already
         exists from i0 to i1 then e is returned.  If a directed edge with index e
         already exists from i1 to i0, ~e is returned (the complement) and
         edgeArray[e] is set to f.  Otherwise, a new edge is created from i0 to i1
         with first face index f and its index is returned.
         
         @param vertexArray Vertex positions to use when deciding colocation.
         
         @param area Area of face f.  When multiple edges of the same direction 
           are found between the same vertices (usually because of degenerate edges)
           the face with larger area is kept in the edge table.
         */
        protected static int findEdgeIndex(
            Array<Vector3> vertexArray,
            Array<Edge> geometricEdgeArray,
            int i0, int i1, int f, double area)
        {
            throw new NotSupportedException("G3D 6.09 declares MeshAlg.findEdgeIndex but does not define or call it.");
        }
    }
}


