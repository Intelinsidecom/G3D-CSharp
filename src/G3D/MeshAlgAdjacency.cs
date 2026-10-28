/**
  @file MeshAlgAdjacency.cpp

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2003-09-14
  @edited  2005-02-24

  Copyright 2000-2003, Morgan McGuire.
  All rights reserved.

 */

using System;

namespace G3D
{
    /**
     A half [i.e. directed] edge.
     */
    internal sealed class MeshDirectedEdgeKey
    {
        /** vertexIndex[0] <= vertexIndex[1] */
        public int[] vertexIndex = new int[2];

        public MeshDirectedEdgeKey() { }

        public MeshDirectedEdgeKey(int i0, int i1)
        {
            if (i0 <= i1)
            {
                vertexIndex[0] = i0;
                vertexIndex[1] = i1;
            }
            else
            {
                vertexIndex[0] = i1;
                vertexIndex[1] = i0;
            }
        }

        public override bool Equals(object obj)
        {
            MeshDirectedEdgeKey e2 = obj as MeshDirectedEdgeKey;
            if (object.ReferenceEquals(e2, null))
            {
                return false;
            }

            for (int i = 0; i < 2; ++i)
            {
                if (vertexIndex[i] != e2.vertexIndex[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return unchecked(vertexIndex[0] + (vertexIndex[1] << 16));
        }
    }

    /**
     A hashtable mapping edges to lists of indices for
     faces.  This is used instead of Table because of the
     special logic in insert.

     Used only for MeshAlg::computeAdjacency.

     In the face lists, index <I>f</I> >= 0 indicates that
     <I>f</I> contains the edge as a forward edge.  Index <I>f</I> < 0 
     indicates that ~<I>f</I> contains the edge as a backward edge.
     */
    internal sealed class MeshEdgeTable
    {
        private Table<MeshDirectedEdgeKey, Array<int>> table = new Table<MeshDirectedEdgeKey, Array<int>>();

        /**
         Clears the table.
         */
        public void clear()
        {
            table.clear();
        }

        /**
         Inserts the faceIndex into the edge's face list.
         The index may be a negative number indicating a backface.
         */
        public void insert(MeshDirectedEdgeKey edge, int faceIndex)
        {
            // debugAssertM((table.size() > 20) && (table.debugGetLoad() < 0.5 || table.debugGetNumBuckets() < 20),
            //    "MeshEdgeTable is using a poor hash function.");

            if (!table.containsKey(edge))
            {
                // First time
                Array<int> x = new Array<int>(1);
                x[0] = faceIndex;
                table.set(edge, x);
            }
            else
            {
                table[edge].append(faceIndex);
            }
        }

        /**
         Returns the face list for a given edge
         */
        public Array<int> get(MeshDirectedEdgeKey edge)
        {
            return table[edge];
        }

        public Table<MeshDirectedEdgeKey, Array<int>>.Iterator begin()
        {
            return table.begin();
        }

        public Table<MeshDirectedEdgeKey, Array<int>>.Iterator end()
        {
            return table.end();
        }
    }

    public partial class MeshAlg
    {
        /**
         edgeTable[edgeKey] is a list of faces containing
         
         Used and cleared by MeshModel::computeAdjacency()
         */
        private static MeshEdgeTable edgeTable = new MeshEdgeTable();

        /**
         Assigns the edge index into the next unassigned edge
         index.  The edge index may be negative, indicating
         a reverse edge.
         */
        private static void assignEdgeIndex(Face face, int e)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (face.edgeIndex[i] == Face.NONE)
                {
                    face.edgeIndex[i] = e;
                    return;
                }
            }

            Debug.debugAssertM(false, "Face has already been assigned 3 edges");
        }

        /**
         @deprecated Use the other version of computeAdjacency, which takes Array<Vertex>.
         @param facesAdjacentToVertex <I>Output</I> adjacentFaceArray[v] is an array of
                            indices for faces touching vertex index v
         */
        public static void computeAdjacency(
            Array<Vector3> vertexGeometry,
            Array<int> indexArray,
            Array<Face> faceArray,
            Array<Edge> edgeArray,
            Array<Array<int>> adjacentFaceArray)
        {
            Array<Vertex> vertexArray = new Array<Vertex>();

            computeAdjacency(vertexGeometry, indexArray, faceArray, edgeArray, vertexArray);

            // Convert the vertexArray into adjacentFaceArray
            adjacentFaceArray.clear();
            adjacentFaceArray.resize(vertexArray.size());
            for (int v = 0; v < adjacentFaceArray.size(); ++v)
            {
                adjacentFaceArray[v] = new Array<int>(vertexArray[v].faceIndex);
            }
        }

        /**
         Given a set of vertices and a set of indices for traversing them
         to create triangles, computes other mesh properties.  

         <B>Colocated vertices are treated as separate.</B>  To have
         colocated vertices collapsed (necessary for many algorithms,
         like shadowing), weld the mesh before computing adjacency.
         
         <I>Recent change: In version 6.00, colocated vertices were automatically
         welded by this routine and degenerate faces and edges were removed.  That
         is no longer the case.</I>

         Where two faces meet, there are two opposite directed edges.  These
         are collapsed into a single bidirectional edge in the edgeArray.
         If four faces meet exactly at the same edge, that edge will appear
         twice in the array, and so on.  If an edge is a boundary of the mesh 
         (i.e. if the edge has only one adjacent face) it will appear in the 
         array with one  face index set to MeshAlg::Face::NONE.

         @param vertexGeometry  %Vertex positions to use when deciding colocation.
         @param indexArray      Order to traverse vertices to make triangles
         @param faceArray       <I>Output</I>
         @param edgeArray       <I>Output</I>.  Sorted so that boundary edges are at the end of the array. 
         @param vertexArray     <I>Output</I> 
         */
        public static void computeAdjacency(
            Array<Vector3> vertexGeometry,
            Array<int> indexArray,
            Array<Face> faceArray,
            Array<Edge> edgeArray,
            Array<Vertex> vertexArray)
        {
            edgeArray.clear();
            vertexArray.clear();
            faceArray.clear();
            edgeTable.clear();

            // Face normals
            Array<Vector3> faceNormal = new Array<Vector3>();

            // This array has the same size as the vertex array
            vertexArray.resize(vertexGeometry.size());
            for (int v = 0; v < vertexArray.size(); ++v)
            {
                vertexArray[v] = new Vertex();
            }

            // Iterate through the triangle list
            for (int q = 0; q < indexArray.size(); q += 3)
            {

                Vector3[] vertex = new Vector3[3];
                int f = faceArray.size();
                Face face = new Face();
                faceArray.append(face);

                // Construct the face
                for (int j = 0; j < 3; ++j)
                {
                    int v = indexArray[q + j];
                    face.vertexIndex[j] = v;
                    face.edgeIndex[j] = Face.NONE;

                    // Store back pointers in the vertices
                    vertexArray[v].faceIndex.append(f);

                    // We'll need these vertices to find the face normal
                    vertex[j] = vertexGeometry[v];
                }

                // Compute the face normal
                Vector3 N = (vertex[1] - vertex[0]).cross(vertex[2] - vertex[0]);
                faceNormal.append(N.directionOrZero());

                int[] nextIndex = { 1, 2, 0 };

                // Add each edge to the edge table.
                for (int j = 0; j < 3; ++j)
                {
                    int i0 = indexArray[q + j];
                    int i1 = indexArray[q + nextIndex[j]];

                    MeshDirectedEdgeKey edge = new MeshDirectedEdgeKey(i0, i1);

                    if (i0 == edge.vertexIndex[0])
                    {
                        // The edge was directed in the same manner as in the face
                        edgeTable.insert(edge, f);
                    }
                    else
                    {
                        // The edge was directed in the opposite manner as in the face
                        edgeTable.insert(edge, ~f);
                    }
                }
            }

            // For each edge in the edge table, create an edge in the edge array.
            // Collapse every 2 edges from adjacent faces.

            Table<MeshDirectedEdgeKey, Array<int>>.Iterator cur = edgeTable.begin();
            Table<MeshDirectedEdgeKey, Array<int>>.Iterator end = edgeTable.end();

            Array<Edge> tempEdgeArray = new Array<Edge>();
            while (cur != end)
            {
                MeshDirectedEdgeKey edgeKey = cur.Current.key;
                Array<int> faceIndexArray = cur.Current.value;

                // Process this edge
                while (faceIndexArray.size() > 0)
                {

                    // Remove the last index
                    int f0 = faceIndexArray.pop();

                    // Find the normal to that face
                    Vector3 n0 = faceNormal[(f0 >= 0) ? f0 : ~f0];

                    bool found = false;

                    // We try to find the matching face with the closest
                    // normal.  This ensures that we don't introduce a lot
                    // of artificial ridges into flat parts of a mesh.
                    double ndotn = -2;
                    int f1 = -1, i1 = -1;

                    // Try to Find the face with the matching edge
                    for (int i = faceIndexArray.size() - 1; i >= 0; --i)
                    {
                        int f = faceIndexArray[i];

                        if ((f >= 0) != (f0 >= 0))
                        {
                            // This face contains the oppositely oriented edge
                            // and has not been assigned too many edges

                            Vector3 n1 = faceNormal[(f >= 0) ? f : ~f];
                            double d = n1.dot(n0);

                            if (found)
                            {
                                // We previously found a good face; see if this
                                // one is better.
                                if (d > ndotn)
                                {
                                    // This face is better.
                                    ndotn = d;
                                    f1 = f;
                                    i1 = i;
                                }
                            }
                            else
                            {
                                // This is the first face we've found
                                found = true;
                                ndotn = d;
                                f1 = f;
                                i1 = i;
                            }
                        }
                    }

                    // Create the new edge
                    int e = tempEdgeArray.size();
                    Edge edge = new Edge();
                    tempEdgeArray.append(edge);

                    edge.vertexIndex[0] = edgeKey.vertexIndex[0];
                    edge.vertexIndex[1] = edgeKey.vertexIndex[1];

                    if (f0 >= 0)
                    {
                        edge.faceIndex[0] = f0;
                        edge.faceIndex[1] = Face.NONE;
                        assignEdgeIndex(faceArray[f0], e);
                    }
                    else
                    {
                        // The face indices above are two's complemented.
                        // this code restores them to regular indices.
                        Debug.debugAssert((~f0) >= 0);
                        edge.faceIndex[1] = ~f0;
                        edge.faceIndex[0] = Face.NONE;

                        // The edge index *does* need to be inverted, however.
                        assignEdgeIndex(faceArray[~f0], ~e);
                    }

                    if (found)
                    {
                        // We found a matching face; remove both
                        // faces from the active list.
                        faceIndexArray.fastRemove(i1);

                        if (f1 >= 0)
                        {
                            edge.faceIndex[0] = f1;
                            assignEdgeIndex(faceArray[f1], e);
                        }
                        else
                        {
                            edge.faceIndex[1] = ~f1;
                            assignEdgeIndex(faceArray[~f1], ~e);
                        }
                    }
                }

                cur.MoveNext();
            }

            edgeTable.clear();

            // Move boundary edges to the end of the list and then
            // clean up the face references into them
            {
                // Map old edge indices to new edge indices
                Array<int> newIndex = new Array<int>(tempEdgeArray.size());

                // Index of the start and end of the edge array
                int i = 0;
                int j = tempEdgeArray.size() - 1;

                edgeArray.resize(tempEdgeArray.size());
                for (int e = 0; e < tempEdgeArray.size(); ++e)
                {
                    if (tempEdgeArray[e].boundary())
                    {
                        newIndex[e] = j;
                        --j;
                    }
                    else
                    {
                        newIndex[e] = i;
                        ++i;
                    }
                    edgeArray[newIndex[e]] = tempEdgeArray[e];
                }

                Debug.debugAssertM(i == j + 1, "Counting from front and back of array did not match");

                // Fix the faces
                for (int f = 0; f < faceArray.size(); ++f)
                {
                    Face face = faceArray[f];
                    for (int q = 0; q < 3; ++q)
                    {
                        int e = face.edgeIndex[q];
                        if (e < 0)
                        {
                            // Backwards edge; twiddle before and after conversion
                            face.edgeIndex[q] = ~newIndex[~e];
                        }
                        else
                        {
                            // Regular edge; remap the index
                            face.edgeIndex[q] = newIndex[e];
                        }
                    }
                }
            }

            // Now order the edge indices inside the faces correctly.
            for (int f = 0; f < faceArray.size(); ++f)
            {
                Face face = faceArray[f];
                int e0 = face.edgeIndex[0];
                int e1 = face.edgeIndex[1];
                int e2 = face.edgeIndex[2];

                // e0 will always remain first.  The only 
                // question is whether e1 and e2 should be swapped.

                // See if e1 begins at the vertex where e1 ends.
                int e0End = (e0 < 0) ?
                    edgeArray[~e0].vertexIndex[0] :
                    edgeArray[e0].vertexIndex[1];

                int e1Begin = (e1 < 0) ?
                    edgeArray[~e1].vertexIndex[1] :
                    edgeArray[e1].vertexIndex[0];

                if (e0End != e1Begin)
                {
                    // We must swap e1 and e2
                    face.edgeIndex[1] = e2;
                    face.edgeIndex[2] = e1;
                }
            }

            // Fill out the edge adjacency information in the vertex array
            for (int e = 0; e < edgeArray.size(); ++e)
            {
                Edge edge = edgeArray[e];
                vertexArray[edge.vertexIndex[0]].edgeIndex.append(e);
                vertexArray[edge.vertexIndex[1]].edgeIndex.append(~e);
            }
        }

        private static void weldBoundaryEdges(
            Array<Face> faceArray,
            Array<Edge> edgeArray,
            Array<Vertex> vertexArray)
        {
            // Copy over the original edge array
            Array<Edge> oldEdgeArray = new Array<Edge>(edgeArray);

            // newEdgeIndex[e] is the new index of the old edge with index e
            // Note that newEdgeIndex[e] might be negative, indicating that
            // the edge switched direction between the arrays.
            Array<int> newEdgeIndex = new Array<int>(edgeArray.size());
            edgeArray.resize(0);

            // boundaryEdgeIndices[v_low] is an array of the indices of 
            // all boundary edges whose lower vertex is v_low.
            Table<int, Array<int>> boundaryEdgeIndices = new Table<int, Array<int>>();

            // Copy over non-boundary edges to the new array
            for (int e = 0; e < oldEdgeArray.size(); ++e)
            {
                if (oldEdgeArray[e].boundary())
                {

                    // Add to the boundary table
                    int v_low = g3dmath.iMin(oldEdgeArray[e].vertexIndex[0], oldEdgeArray[e].vertexIndex[1]);
                    if (!boundaryEdgeIndices.containsKey(v_low))
                    {
                        boundaryEdgeIndices.set(v_low, new Array<int>());
                    }
                    boundaryEdgeIndices[v_low].append(e);

                    // We'll fill out newEdgeIndex[e] later, when we find pairs

                }
                else
                {

                    // Copy the edge to the new array
                    newEdgeIndex[e] = edgeArray.size();
                    edgeArray.append(oldEdgeArray[e]);

                }
            }

            // Remove all edges from the table that have pairs.
            Table<int, Array<int>>.Iterator cur = boundaryEdgeIndices.begin();
            Table<int, Array<int>>.Iterator end = boundaryEdgeIndices.end();
            while (cur != end)
            {
                Array<int> boundaryEdge = cur.Current.value;

                for (int i = 0; i < boundaryEdge.size(); ++i)
                {
                    int ei = boundaryEdge[i];
                    Edge edgei = oldEdgeArray[ei];

                    for (int j = i + 1; j < boundaryEdge.size(); ++j)
                    {
                        int ej = boundaryEdge[j];
                        Edge edgej = oldEdgeArray[ej];

                        // See if edge ei is the reverse (match) of edge ej.

                        // True if the edges match
                        bool match = false;

                        // True if edgej's vertex indices are reversed from
                        // edgei's (usually true).
                        bool reversej = false;

                        int u = edgei.vertexIndex[0];
                        int v = edgei.vertexIndex[1];

                        if (edgei.faceIndex[0] != Face.NONE)
                        {
                            //        verts|faces
                            // edgei = [u v A /]

                            if (edgej.faceIndex[0] != Face.NONE)
                            {
                                if ((edgej.vertexIndex[0] == v) && (edgej.vertexIndex[1] == u))
                                {
                                    // This is the most common of the four cases

                                    // edgej = [v u B /]
                                    match = true;
                                    reversej = true;
                                }
                            }
                            else
                            {
                                if ((edgej.vertexIndex[0] == u) && (edgej.vertexIndex[1] == v))
                                {
                                    // edgej = [u v / B]
                                    match = true;
                                }
                            }
                        }
                        else
                        {
                            // edgei = [u v / A]
                            if (edgej.faceIndex[0] != Face.NONE)
                            {
                                if ((edgej.vertexIndex[0] == u) && (edgej.vertexIndex[1] == v))
                                {
                                    // edgej = [u v B /]
                                    match = true;
                                }
                            }
                            else
                            {
                                if ((edgej.vertexIndex[0] == v) && (edgej.vertexIndex[1] == u))
                                {
                                    // edgej = [v u / B]
                                    match = true;
                                    reversej = true;
                                }
                            }
                        }

                        if (match)
                        {
                            // ei and ej can be paired as a single edge
                            int e = edgeArray.size();
                            Edge edge = new Edge();
                            edgeArray.append(edge);

                            // Follow the direction of edgei.
                            edge.vertexIndex[0] = edgei.vertexIndex[0];
                            edge.vertexIndex[1] = edgei.vertexIndex[1];
                            edge.faceIndex[0] = edgei.faceIndex[0];
                            edge.faceIndex[1] = edgei.faceIndex[1];
                            newEdgeIndex[ei] = e;

                            // Insert the face index for edgej.
                            int fj = edgej.faceIndex[0];
                            if (fj == Face.NONE)
                            {
                                fj = edgej.faceIndex[1];
                            }

                            if (edge.faceIndex[0] == Face.NONE)
                            {
                                edge.faceIndex[0] = fj;
                            }
                            else
                            {
                                edge.faceIndex[1] = fj;
                            }

                            if (reversej)
                            {
                                // The new edge is backwards of the old edge for ej
                                newEdgeIndex[ej] = ~e;
                            }
                            else
                            {
                                newEdgeIndex[ej] = e;
                            }

                            // Remove both ei and ej from being candidates for future pairing.
                            // Remove ej first since it comes later in the list (removing
                            // ei would decrease the index of ej to j - 1).
                            boundaryEdge.fastRemove(j);
                            boundaryEdge.fastRemove(i);

                            // Re-process element i, which is now a new edge index
                            --i;

                            // Jump out of the j for-loop
                            break;
                        }
                    }
                }
                cur.MoveNext();
            }

            // Anything remaining in the table is a real boundary edge; just copy it to
            // the end of the array.
            cur = boundaryEdgeIndices.begin();
            end = boundaryEdgeIndices.end();
            while (cur != end)
            {
                Array<int> boundaryEdge = cur.Current.value;

                for (int b = 0; b < boundaryEdge.size(); ++b)
                {
                    int e = boundaryEdge[b];

                    newEdgeIndex[e] = edgeArray.size();
                    edgeArray.append(oldEdgeArray[e]);
                }

                cur.MoveNext();
            }

            // Finally, fix up edge indices in the face and vertex arrays
            for (int f = 0; f < faceArray.size(); ++f)
            {
                Face face = faceArray[f];
                for (int i = 0; i < 3; ++i)
                {
                    int e = face.edgeIndex[i];

                    if (e < 0)
                    {
                        face.edgeIndex[i] = ~newEdgeIndex[~e];
                    }
                    else
                    {
                        face.edgeIndex[i] = newEdgeIndex[e];
                    }
                }
            }

            for (int v = 0; v < vertexArray.size(); ++v)
            {
                Vertex vertex = vertexArray[v];
                for (int i = 0; i < vertex.edgeIndex.size(); ++i)
                {
                    int e = vertex.edgeIndex[i];

                    if (e < 0)
                    {
                        vertex.edgeIndex[i] = ~newEdgeIndex[~e];
                    }
                    else
                    {
                        vertex.edgeIndex[i] = newEdgeIndex[e];
                    }
                }
            }
        }

        /**
         Modifies the face, edge, and vertex arrays in place so that
         colocated (within radius) vertices are treated as identical.
         Note that the vertexArray and corresponding geometry will
         contain elements that are no longer used.  In the vertexArray, 
         these elements are initialized to MeshAlg::Vertex() but not 
         removed (because removal would change the indexing).
         
         This is a good preprocessing step for algorithms that are only
         concerned with the shape of a mesh (e.g. cartoon rendering, fur, shadows)
         and not the indexing of the vertices.

         Use this method when you have already computed adjacency information
         and want to collapse colocated vertices within that data without
         disturbing the actual mesh vertices or indexing scheme.  
         
         If you have not computed adjacency already, use MeshAlg::computeWeld
         instead and compute adjacency information after welding.

         @param faceArray Mutated in place.  Size is maintained (degenerate
                faces are <b>not</B> removed).
         @param edgeArray Mutated in place.  May shrink if boundary edges
                are welded together.
         @param vertexArray Mutated in place.  Size is maintained (duplicate
                vertices contain no adjacency info).
        */
        public static void weldAdjacency(
            Array<Vector3> originalGeometry,
            Array<Face> faceArray,
            Array<Edge> edgeArray,
            Array<Vertex> vertexArray,
            double radius = g3dmath.fuzzyEpsilon)
        {
            // Num vertices
            int n = originalGeometry.size();

            // canonical[v] = first occurance of any vertex near oldVertexArray[v]
            Array<int> canonical = new Array<int>(n);

            Array<int> toNew = new Array<int>();
            Array<int> toOld = new Array<int>();
            // Throw away the new vertex array
            Array<Vector3> dummy = new Array<Vector3>();
            computeWeld(originalGeometry, dummy, toNew, toOld, radius);

            for (int v = 0; v < canonical.size(); ++v)
            {
                // Round-trip through the toNew/toOld process.  This will give
                // us the original vertex.
                canonical[v] = toOld[toNew[v]];
            }

            // Destroy vertexArray (we reconstruct it below)
            vertexArray.clear();
            vertexArray.resize(n);
            for (int v = 0; v < vertexArray.size(); ++v)
            {
                vertexArray[v] = new Vertex();
            }

            bool hasBoundaryEdges = false;

            // Fix edge vertex indices
            for (int e = 0; e < edgeArray.size(); ++e)
            {
                Edge edge = edgeArray[e];

                int v0 = canonical[edge.vertexIndex[0]];
                int v1 = canonical[edge.vertexIndex[1]];

                edge.vertexIndex[0] = v0;
                edge.vertexIndex[1] = v1;

                vertexArray[v0].edgeIndex.append(e);
                vertexArray[v1].edgeIndex.append(~e);

                hasBoundaryEdges = hasBoundaryEdges || edge.boundary();
            }

            // Fix face vertex indices
            for (int f = 0; f < faceArray.size(); ++f)
            {
                Face face = faceArray[f];
                for (int i = 0; i < 3; ++i)
                {
                    int v = canonical[face.vertexIndex[i]];

                    face.vertexIndex[i] = v;

                    // Add the back pointer
                    vertexArray[v].faceIndex.append(f);
                }
            }

            if (hasBoundaryEdges)
            {
                // As a result of the welding, some of the boundary edges at
                // the end of the array may now have mates and no longer be
                // boundaries.  Try to pair these up.

                weldBoundaryEdges(faceArray, edgeArray, vertexArray);
            }
        }

        /**
         In debug mode, asserts that the adjacency references between the
         face, edge, and vertex array are consistent.
         */
        public static void debugCheckConsistency(
            Array<Face> faceArray,
            Array<Edge> edgeArray,
            Array<Vertex> vertexArray)
        {
#if !DEBUG
            return;
#else
            for (int v = 0; v < vertexArray.size(); ++v)
            {
                Vertex vertex = vertexArray[v];

                for (int i = 0; i < vertex.edgeIndex.size(); ++i)
                {
                    int e = vertex.edgeIndex[i];
                    Debug.debugAssert(edgeArray[(e >= 0) ? e : ~e].containsVertex(v));
                }

                for (int i = 0; i < vertex.faceIndex.size(); ++i)
                {
                    int f = vertex.faceIndex[i];
                    Debug.debugAssert(faceArray[f].containsVertex(v));
                }

            }

            for (int e = 0; e < edgeArray.size(); ++e)
            {
                Edge edge = edgeArray[e];

                for (int i = 0; i < 2; ++i)
                {
                    Debug.debugAssert((edge.faceIndex[i] == Face.NONE) ||
                        faceArray[edge.faceIndex[i]].containsEdge(e));

                    Debug.debugAssert(vertexArray[edge.vertexIndex[i]].inEdge(e));
                }
            }

            // Every face's edge must be on that face
            for (int f = 0; f < faceArray.size(); ++f)
            {
                Face face = faceArray[f];
                for (int i = 0; i < 3; ++i)
                {
                    int e = face.edgeIndex[i];
                    int ei = (e >= 0) ? e : ~e;
                    Debug.debugAssert(edgeArray[ei].inFace(f));

                    // Make sure the edge is oriented appropriately 
                    if (e >= 0)
                    {
                        Debug.debugAssert(edgeArray[ei].faceIndex[0] == (int)f);
                    }
                    else
                    {
                        Debug.debugAssert(edgeArray[ei].faceIndex[1] == (int)f);
                    }

                    Debug.debugAssert(vertexArray[face.vertexIndex[i]].inFace(f));
                }
            }
#endif
        }
    }
}
