using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testMeshBuilder()
        {
            printf("MeshBuilder ");

            MeshBuilder builder = new MeshBuilder();
            builder.setName("quad");
            builder.addQuad(
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0));

            Array<int> indices = new Array<int>();
            Array<Vector3> vertices = new Array<Vector3>();
            string name = "";
            builder.commit(ref name, indices, vertices);

            CheckEqual("quad", name, "MeshBuilder name");
            CheckEqual(6, indices.size(), "MeshBuilder triangle index count");
            CheckEqual(4, vertices.size(), "MeshBuilder welded vertex count");
            Check(indices[0] != indices[1] && indices[1] != indices[2] && indices[2] != indices[0], "MeshBuilder nondegenerate triangle");
            Check(indices[3] != indices[4] && indices[4] != indices[5] && indices[5] != indices[3], "MeshBuilder nondegenerate triangle 2");

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < vertices.size(); ++i)
            {
                minX = Math.Min(minX, vertices[i].x);
                maxX = Math.Max(maxX, vertices[i].x);
                minY = Math.Min(minY, vertices[i].y);
                maxY = Math.Max(maxY, vertices[i].y);
            }
            CheckEqual(-1.0, minX, "MeshBuilder centered x minimum");
            CheckEqual(1.0, maxX, "MeshBuilder centered x maximum");
            CheckEqual(-1.0, minY, "MeshBuilder centered y minimum");
            CheckEqual(1.0, maxY, "MeshBuilder centered y maximum");

            MeshBuilder twoSided = new MeshBuilder(true);
            twoSided.setName("two-sided");
            twoSided.addTriangle(
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0));
            Array<int> twoSidedIndices = new Array<int>();
            Array<Vector3> twoSidedVertices = new Array<Vector3>();
            string twoSidedName = "";
            twoSided.commit(ref twoSidedName, twoSidedIndices, twoSidedVertices);
            CheckEqual("two-sided", twoSidedName, "MeshBuilder two-sided name");
            CheckEqual(6, twoSidedIndices.size(), "MeshBuilder two-sided index count");
            CheckEqual(3, twoSidedVertices.size(), "MeshBuilder two-sided vertex count");

            MeshBuilder triangleBuilder = new MeshBuilder();
            triangleBuilder.setWeldRadius(0.0);
            triangleBuilder.addTriangle(new Triangle(
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0)));
            triangleBuilder.addTriangle(new Triangle(
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0)));
            Array<int> repeatedIndices = new Array<int>();
            Array<Vector3> repeatedVertices = new Array<Vector3>();
            string repeatedName = "";
            triangleBuilder.commit(ref repeatedName, repeatedIndices, repeatedVertices);
            CheckEqual(6, repeatedIndices.size(), "MeshBuilder keeps nondegenerate repeated triangles");
            CheckEqual(3, repeatedVertices.size(), "MeshBuilder welds repeated vertices");

            MeshBuilder degenerateBuilder = new MeshBuilder();
            degenerateBuilder.setWeldRadius(0.01);
            degenerateBuilder.addTriangle(new Triangle(
                new Vector3(0, 0, 0),
                new Vector3(0.0001f, 0, 0),
                new Vector3(0, 0.0001f, 0)));
            degenerateBuilder.addTriangle(new Triangle(
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0)));
            Array<int> degenerateIndices = new Array<int>();
            Array<Vector3> degenerateVertices = new Array<Vector3>();
            string degenerateName = "";
            degenerateBuilder.commit(ref degenerateName, degenerateIndices, degenerateVertices);
            CheckEqual(3, degenerateIndices.size(), "MeshBuilder removes degenerate triangles");
            CheckEqual(4, degenerateVertices.size(), "MeshBuilder welds degenerate vertices");
            Check(degenerateIndices[0] != degenerateIndices[1] && degenerateIndices[1] != degenerateIndices[2] && degenerateIndices[2] != degenerateIndices[0], "MeshBuilder retained nondegenerate triangle");

            bool emptyRejected = false;
            try
            {
                MeshBuilder emptyBuilder = new MeshBuilder();
                Array<int> emptyIndices = new Array<int>();
                Array<Vector3> emptyVertices = new Array<Vector3>();
                string emptyName = "";
                emptyBuilder.commit(ref emptyName, emptyIndices, emptyVertices);
            }
            catch (InvalidOperationException)
            {
                emptyRejected = true;
            }
            Check(emptyRejected, "MeshBuilder rejects empty model");

            bool zeroExtentRejected = false;
            try
            {
                MeshBuilder zeroExtentBuilder = new MeshBuilder();
                zeroExtentBuilder.addTriangle(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 0, 0),
                    new Vector3(0, 0, 0));
                Array<int> zeroExtentIndices = new Array<int>();
                Array<Vector3> zeroExtentVertices = new Array<Vector3>();
                string zeroExtentName = "";
                zeroExtentBuilder.commit(ref zeroExtentName, zeroExtentIndices, zeroExtentVertices);
            }
            catch (InvalidOperationException)
            {
                zeroExtentRejected = true;
            }
            Check(zeroExtentRejected, "MeshBuilder rejects zero-extent model");

            printf("passed\n");
        }
    }
}
