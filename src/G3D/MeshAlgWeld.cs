/**
  @file MeshAlgWeld.cpp

  The MeshAlg::computeWeld method.

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2003-10-22
  @edited  2005-02-24

  Copyright 2000-2003, Morgan McGuire.
  All rights reserved.

 */

using System;

namespace G3D
{
    namespace _internal
    {
        internal sealed class Welder
        {
            public const int GRID_RES = 32;

            /** Indices of newVertexArray elements in <B>or near</B> a grid cell. */
            private readonly Array<int>[,,] grid = new Array<int>[GRID_RES, GRID_RES, GRID_RES];

            private readonly Array<Vector3> oldVertexArray;
            private readonly Array<Vector3> newVertexArray;
            private readonly Array<int> toNew;
            private readonly Array<int> toOld;

            /** Must be less than one grid cell, not checked */
            private readonly double radius;

            /** (oldVertexArray[i] - offset) * scale is on the range [0, 1] */
            private Vector3 offset;
            private Vector3 scale;

            internal Welder(
                Array<Vector3> _oldVertexArray,
                Array<Vector3> _newVertexArray,
                Array<int> _toNew,
                Array<int> _toOld,
                double _radius)
            {
                oldVertexArray = _oldVertexArray;
                newVertexArray = _newVertexArray;
                toNew = _toNew;
                toOld = _toOld;
                radius = _radius;

                for (int x = 0; x < GRID_RES; ++x)
                {
                    for (int y = 0; y < GRID_RES; ++y)
                    {
                        for (int z = 0; z < GRID_RES; ++z)
                        {
                            grid[x, y, z] = new Array<int>();
                        }
                    }
                }

                // Compute a scale factor that moves the range
                // of all ordinates to [0, 1]
                Vector3 minBound = Vector3.inf();
                Vector3 maxBound = -minBound;

                for (int i = 0; i < oldVertexArray.size(); ++i)
                {
                    minBound = minBound.min(oldVertexArray[i]);
                    maxBound = maxBound.max(oldVertexArray[i]);
                }

                offset = minBound;
                scale = maxBound - minBound;

                // The model might have zero extent along some axis
                if (g3dmath.fuzzyEq(scale.x, 0.0))
                {
                    scale.x = 1.0f;
                }
                else
                {
                    scale.x = 1.0f / scale.x;
                }

                if (g3dmath.fuzzyEq(scale.y, 0.0))
                {
                    scale.y = 1.0f;
                }
                else
                {
                    scale.y = 1.0f / scale.y;
                }

                if (g3dmath.fuzzyEq(scale.z, 0.0))
                {
                    scale.z = 1.0f;
                }
                else
                {
                    scale.z = 1.0f / scale.z;
                }
            }

            /**
             Computes the grid index from an ordinate.
             */
            private void toGridCoords(Vector3 v, out int x, out int y, out int z)
            {
                v = (v - offset) * scale;
                x = g3dmath.iClamp(g3dmath.iFloor(v.x * GRID_RES), 0, GRID_RES - 1);
                y = g3dmath.iClamp(g3dmath.iFloor(v.y * GRID_RES), 0, GRID_RES - 1);
                z = g3dmath.iClamp(g3dmath.iFloor(v.z * GRID_RES), 0, GRID_RES - 1);
            }

            /** Gets the index of a vertex, adding it to 
                newVertexArray if necessary. */
            private int getIndex(Vector3 vertex)
            {
                int closestIndex = -1;
                double distanceSquared = g3dmath.inf();

                int ix, iy, iz;
                toGridCoords(vertex, out ix, out iy, out iz);

                // Check against all vertices within radius of this grid cube
                Array<int> list = grid[ix, iy, iz];

                for (int i = 0; i < list.size(); ++i)
                {
                    double d = (newVertexArray[list[i]] - vertex).squaredMagnitude();

                    if (d < distanceSquared)
                    {
                        distanceSquared = d;
                        closestIndex = list[i];
                    }
                }

                if (distanceSquared <= radius * radius)
                {

                    return closestIndex;

                }
                else
                {

                    // This is a new vertex
                    int newIndex = newVertexArray.size();
                    newVertexArray.append(vertex);

                    // Create a new vertex and store its index in the
                    // neighboring grid cells (usually, only 1 neighbor)

                    Set<Array<int>> neighbors = new Set<Array<int>>();

                    for (int dx = -1; dx <= +1; ++dx)
                    {
                        for (int dy = -1; dy <= +1; ++dy)
                        {
                            for (int dz = -1; dz <= +1; ++dz)
                            {
                                int cx, cy, cz;
                                toGridCoords(vertex + new Vector3(dx, dy, dz) * radius, out cx, out cy, out cz);
                                neighbors.insert(grid[cx, cy, cz]);
                            }
                        }
                    }

                    Set<Array<int>>.Iterator neighbor = neighbors.begin();
                    Set<Array<int>>.Iterator none = neighbors.end();

                    while (neighbor != none)
                    {
                        neighbor.Current.append(newIndex);
                        neighbor.MoveNext();
                    }

                    return newIndex;
                }
            }

            internal void weld()
            {
                newVertexArray.resize(0);

                // Prime the vertex positions
                for (int i = 0; i < oldVertexArray.size(); ++i)
                {
                    getIndex(oldVertexArray[i]);
                }

                // Now create the official remapping by snapping to 
                // nearby vertices.
                toNew.resize(oldVertexArray.size());
                toOld.resize(newVertexArray.size());

                for (int oi = 0; oi < oldVertexArray.size(); ++oi)
                {
                    toNew[oi] = getIndex(oldVertexArray[oi]);
                    toOld[toNew[oi]] = oi;
                }
            }
        }
    }

    public partial class MeshAlg
    {
        /**
         Welds nearby and colocated elements of the <I>oldVertexArray</I> together so that
         <I>newVertexArray</I> contains no vertices within <I>radius</I> of one another.
         Every vertex in newVertexPositions also appears in oldVertexPositions.
         This is useful for downsampling meshes and welding cracks created by artist errors
         or numerical imprecision.  

         The two integer arrays map indices back and forth between the arrays according to:
         <PRE>
         oldVertexArray[toOld[ni]] == newVertexArray[ni]
         oldVertexArray[oi] == newVertexArray[toNew[ni]]
         </PRE>

         Note that newVertexPositions is never longer than oldVertexPositions
         and is shorter when vertices are welded.

         Welding with a large radius will effectively compute a lower level of detail for
         the mesh.

         The welding method runs in roughly linear time in the length of oldVertexArray--
         a uniform spatial grid is used to achieve nearly constant time vertex collapses
         for uniformly distributed vertices.

         It is sometimes desirable to keep the original vertex ordering but 
         identify the unique vertices.  The following code computes 
         array canonical s.t. canonical[v] = first occurance of 
         a vertex near oldVertexPositions[v] in oldVertexPositions.

         <PRE>
            Array<int> canonical(oldVertexPositions.size()), toNew, toOld;
            computeWeld(oldVertexPositions, Array<Vector3>(), toNew, toOld, radius);
            for (int v = 0; v < canonical.size(); ++v) {
                canonical[v] = toOld[toNew[v]];
            }
         </PRE>

         See also G3D::MeshAlg::weldAdjacency.

         @cite The method is that described as the 'Grouper' in Baum, Mann, Smith, and Winget, 
         Making Radiosity Usable: Automatic Preprocessing and Meshing Techniques for 
         the Generation of Accurate Radiosity Solutions, Computer Graphics vol 25, no 4, July 1991.
         */
        public static void computeWeld(
            Array<Vector3> oldVertexArray,
            Array<Vector3> newVertexArray,
            Array<int> toNew,
            Array<int> toOld,
            double radius = g3dmath.fuzzyEpsilon)
        {
            _internal.Welder welder = new _internal.Welder(oldVertexArray, newVertexArray, toNew, toOld, radius);
            welder.weld();
        }
    }
}

