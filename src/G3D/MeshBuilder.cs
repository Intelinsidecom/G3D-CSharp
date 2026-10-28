/**
  @file MeshBuilder.h
  
  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2002-02-27
  @edited  2004-10-04
 */ 

/**
  @file MeshBuilder.cpp

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2002-02-27
  @edited 2005-02-24
 */

using System;

namespace G3D
{
    /**
     Allows creation of optimized watertight meshes from unoptimized polygon soups.
     See also G3D::MeshAlg for algorithms that operate on the output.
     */
    public class MeshBuilder
    {
        /**
         Set setWeldRadius to AUTO_WELD to weld vertices closer than 1/2
         the smallest edge length in a model.
         */
        public const int AUTO_WELD = -100;

        private string name = string.Empty;
        private Array<Vector3> triList = new Array<Vector3>();
        private bool _twoSided;
        private double close;

        public MeshBuilder()
            : this(false)
        {
        }

        public MeshBuilder(bool twoSided)
        {
            _twoSided = twoSided;
            close = AUTO_WELD;
        }

        /** Writes the model to the arrays, which can then be used with
            G3D::IFSModel::save and G3D::MeshAlg */
        public void commit(ref string n, Array<int> indexArray, Array<Vector3> outvertexArray)
        {
            if (triList.size() == 0)
            {
                throw new InvalidOperationException("Cannot commit an empty mesh.");
            }

            n = name;

            // Make the data fit in a unit cube
            centerTriList();

            Array<int> toNew = new Array<int>();
            Array<int> toOld = new Array<int>();

            if (close == AUTO_WELD)
            {
                Array<int> index = new Array<int>();
                MeshAlg.createIndexArray(triList.size(), index);
                double minEdgeLen, maxEdgeLen, meanEdgeLen, medianEdgeLen;
                double minFaceArea, maxFaceArea, meanFaceArea, medianFaceArea;
                MeshAlg.computeAreaStatistics(triList, index,
                    out minEdgeLen, out meanEdgeLen, out medianEdgeLen, out maxEdgeLen,
                    out minFaceArea, out meanFaceArea, out medianFaceArea, out maxFaceArea);
                close = minEdgeLen * 0.1;
            }

            MeshAlg.computeWeld(triList, outvertexArray, toNew, toOld, close);

            // Construct triangles
            for (int t = 0; t < triList.size(); t += 3)
            {
                int[] index = new int[3];

                for (int i = 0; i < 3; ++i)
                {
                   index[i] = toNew[t + i];
                }

                // Throw out zero size triangles
                if ((index[0] != index[1]) &&
                    (index[1] != index[2]) &&
                    (index[2] != index[0]))
                {
                    indexArray.append(index[0], index[1], index[2]);
                }
            }
        }

        /**
         Adds a new triangle to the model. (Counter clockwise)
         */
        public void addTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            triList.append(a, b, c);

            if (_twoSided)
            {
                triList.append(c, b, a);
            }
        }

        /**
         Adds two new triangles to the model. (Counter clockwise)
         */
        public void addQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            addTriangle(a, b, c);
            addTriangle(a, c, d);
        }

        public void addTriangle(Triangle t)
        {
            addTriangle(t.vertex(0), t.vertex(1), t.vertex(2));
        }

        public void setName(string n)
        {
            if (n == null)
            {
                throw new ArgumentNullException("n");
            }
            name = n;
        }

        /** Vertices within this distance are considered identical.  
            Use AUTO_WELD (the default) to have the distance be a function of the model size.*/
        public void setWeldRadius(double r)
        {
            close = r;
        }

        private void centerTriList()
        {
            // Compute the range of the vertices
            Vector3 vmin, vmax;

            computeBounds(out vmin, out vmax);

            Vector3 diagonal = vmax - vmin;
            double scale = Math.Max(Math.Max(diagonal.x, diagonal.y), diagonal.z) / 2;
            if (!(scale > 0) || double.IsInfinity(scale) || double.IsNaN(scale))
            {
                throw new InvalidOperationException("MeshBuilder requires a model with non-zero extent.");
            }

            Vector3 translation = vmin + diagonal / 2;

            // Center and scale all vertices in the input list
            int v;

            //Matrix3 rot90 = Matrix3::fromAxisAngle(Vector3::UNIT_Y, toRadians(180)) * Matrix3::fromAxisAngle(Vector3::UNIT_X, toRadians(90));
            for (v = 0; v < triList.size(); ++v)
            {
                triList[v] = (triList[v] - translation) / (float)scale;
                //triList[v] = rot90 * triList[v];
            }
        }

        private void computeBounds(out Vector3 min, out Vector3 max)
        {
            min = Vector3.INF3; 
            max = -min;

            int v;
            for (v = 0; v < triList.size(); ++v)
            {
                min = min.min(triList[v]);
                max = max.max(triList[v]);
            }
        }
    }
}
