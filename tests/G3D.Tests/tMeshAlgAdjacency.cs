using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testAdjacency()
        {
            printf("MeshAlg.computeAdjacency\n");

            {
                //          2
                //        /|
                //       / |
                //      /  |
                //     /___|
                //    0     1
                //
                //


                MeshAlg.Geometry geometry = new MeshAlg.Geometry();
                Array<int> index = new Array<int>();
                Array<MeshAlg.Face> faceArray = new Array<MeshAlg.Face>();
                Array<MeshAlg.Edge> edgeArray = new Array<MeshAlg.Edge>();
                Array<MeshAlg.Vertex> vertexArray = new Array<MeshAlg.Vertex>();

                geometry.vertexArray.append(new Vector3(0, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 1, 0));

                index.append(0, 1, 2);

                MeshAlg.computeAdjacency(
                    geometry.vertexArray,
                    index,
                    faceArray,
                    edgeArray,
                    vertexArray);

                Debug.debugAssert(faceArray.size() == 1);
                Debug.debugAssert(edgeArray.size() == 3);

                Debug.debugAssert(faceArray[0].containsVertex(0));
                Debug.debugAssert(faceArray[0].containsVertex(1));
                Debug.debugAssert(faceArray[0].containsVertex(2));

                Debug.debugAssert(faceArray[0].containsEdge(0));
                Debug.debugAssert(faceArray[0].containsEdge(1));
                Debug.debugAssert(faceArray[0].containsEdge(2));

                Debug.debugAssert(edgeArray[0].inFace(0));
                Debug.debugAssert(edgeArray[1].inFace(0));
                Debug.debugAssert(edgeArray[2].inFace(0));

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);

                // Severely weld, creating a degenerate face
                MeshAlg.weldAdjacency(geometry.vertexArray, faceArray, edgeArray, vertexArray, 1.1);

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);
                Debug.debugAssert(!faceArray[0].containsVertex(0));

            }

            {
                //      
                //    0====1
                //  (degenerate face)
                //

                MeshAlg.Geometry geometry = new MeshAlg.Geometry();
                Array<int> index = new Array<int>();
                Array<MeshAlg.Face> faceArray = new Array<MeshAlg.Face>();
                Array<MeshAlg.Edge> edgeArray = new Array<MeshAlg.Edge>();
                Array<MeshAlg.Vertex> vertexArray = new Array<MeshAlg.Vertex>();

                geometry.vertexArray.append(new Vector3(0, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 0, 0));

                index.append(0, 1, 0);

                MeshAlg.computeAdjacency(
                    geometry.vertexArray,
                    index,
                    faceArray,
                    edgeArray,
                    vertexArray);

                Debug.debugAssert(faceArray.size() == 1);
                Debug.debugAssert(edgeArray.size() == 2);

                Debug.debugAssert(faceArray[0].containsVertex(0));
                Debug.debugAssert(faceArray[0].containsVertex(1));

                Debug.debugAssert(faceArray[0].containsEdge(0));
                Debug.debugAssert(faceArray[0].containsEdge(1));

                Debug.debugAssert(edgeArray[0].inFace(0));
                Debug.debugAssert(edgeArray[1].inFace(0));
                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);
            }

            {
                //          2                       .
                //        /|\                       .
                //       / | \                      .
                //      /  |  \                     . 
                //     /___|___\                    . 
                //    0     1    3
                //
                //


                MeshAlg.Geometry geometry = new MeshAlg.Geometry();
                Array<int> index = new Array<int>();
                Array<MeshAlg.Face> faceArray = new Array<MeshAlg.Face>();
                Array<MeshAlg.Edge> edgeArray = new Array<MeshAlg.Edge>();
                Array<MeshAlg.Vertex> vertexArray = new Array<MeshAlg.Vertex>();

                geometry.vertexArray.append(new Vector3(0, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 1, 0));
                geometry.vertexArray.append(new Vector3(2, 0, 0));

                index.append(0, 1, 2);
                index.append(1, 3, 2);

                MeshAlg.computeAdjacency(
                    geometry.vertexArray,
                    index,
                    faceArray,
                    edgeArray,
                    vertexArray);

                Debug.debugAssert(faceArray.size() == 2);
                Debug.debugAssert(edgeArray.size() == 5);
                Debug.debugAssert(vertexArray.size() == 4);

                Debug.debugAssert(faceArray[0].containsVertex(0));
                Debug.debugAssert(faceArray[0].containsVertex(1));
                Debug.debugAssert(faceArray[0].containsVertex(2));

                Debug.debugAssert(faceArray[1].containsVertex(3));
                Debug.debugAssert(faceArray[1].containsVertex(1));
                Debug.debugAssert(faceArray[1].containsVertex(2));

                // The non-boundary edge must be first
                Debug.debugAssert(!edgeArray[0].boundary());
                Debug.debugAssert(edgeArray[1].boundary());
                Debug.debugAssert(edgeArray[2].boundary());
                Debug.debugAssert(edgeArray[3].boundary());
                Debug.debugAssert(edgeArray[4].boundary());

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);

                MeshAlg.weldAdjacency(geometry.vertexArray, faceArray, edgeArray, vertexArray);

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);

                Debug.debugAssert(faceArray.size() == 2);
                Debug.debugAssert(edgeArray.size() == 5);
                Debug.debugAssert(vertexArray.size() == 4);
            }


            {
                // Test Welding


                //         2                  .
                //        /|\                 .
                //       / | \                .
                //      /  |  \               .
                //     /___|___\              .
                //    0   1,4   3
                //


                MeshAlg.Geometry geometry = new MeshAlg.Geometry();
                Array<int> index = new Array<int>();
                Array<MeshAlg.Face> faceArray = new Array<MeshAlg.Face>();
                Array<MeshAlg.Edge> edgeArray = new Array<MeshAlg.Edge>();
                Array<MeshAlg.Vertex> vertexArray = new Array<MeshAlg.Vertex>();

                geometry.vertexArray.append(new Vector3(0, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 1, 0));
                geometry.vertexArray.append(new Vector3(2, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 0, 0));

                index.append(0, 1, 2);
                index.append(2, 4, 3);

                MeshAlg.computeAdjacency(
                    geometry.vertexArray,
                    index,
                    faceArray,
                    edgeArray,
                    vertexArray);

                Debug.debugAssert(faceArray.size() == 2);
                Debug.debugAssert(edgeArray.size() == 6);
                Debug.debugAssert(vertexArray.size() == 5);

                Debug.debugAssert(edgeArray[0].boundary());
                Debug.debugAssert(edgeArray[1].boundary());
                Debug.debugAssert(edgeArray[2].boundary());
                Debug.debugAssert(edgeArray[3].boundary());
                Debug.debugAssert(edgeArray[4].boundary());
                Debug.debugAssert(edgeArray[5].boundary());

                Debug.debugAssert(faceArray[0].containsVertex(0));
                Debug.debugAssert(faceArray[0].containsVertex(1));
                Debug.debugAssert(faceArray[0].containsVertex(2));

                Debug.debugAssert(faceArray[1].containsVertex(2));
                Debug.debugAssert(faceArray[1].containsVertex(3));
                Debug.debugAssert(faceArray[1].containsVertex(4));

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);

                MeshAlg.weldAdjacency(geometry.vertexArray, faceArray, edgeArray, vertexArray);

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);

                Debug.debugAssert(faceArray.size() == 2);
                Debug.debugAssert(edgeArray.size() == 5);
                Debug.debugAssert(vertexArray.size() == 5);

                Debug.debugAssert(!edgeArray[0].boundary());
                Debug.debugAssert(edgeArray[1].boundary());
                Debug.debugAssert(edgeArray[2].boundary());
                Debug.debugAssert(edgeArray[3].boundary());
                Debug.debugAssert(edgeArray[4].boundary());
            }
            {
                // Test Welding


                //        2,5 
                //        /|\         . 
                //       / | \        .
                //      /  |  \       .
                //     /___|___\      .
                //    0   1,4   3
                //


                MeshAlg.Geometry geometry = new MeshAlg.Geometry();
                Array<int> index = new Array<int>();
                Array<MeshAlg.Face> faceArray = new Array<MeshAlg.Face>();
                Array<MeshAlg.Edge> edgeArray = new Array<MeshAlg.Edge>();
                Array<MeshAlg.Vertex> vertexArray = new Array<MeshAlg.Vertex>();

                geometry.vertexArray.append(new Vector3(0, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 1, 0));
                geometry.vertexArray.append(new Vector3(2, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 0, 0));
                geometry.vertexArray.append(new Vector3(1, 1, 0));

                index.append(0, 1, 2);
                index.append(5, 4, 3);

                MeshAlg.computeAdjacency(
                    geometry.vertexArray,
                    index,
                    faceArray,
                    edgeArray,
                    vertexArray);

                Debug.debugAssert(faceArray.size() == 2);
                Debug.debugAssert(edgeArray.size() == 6);
                Debug.debugAssert(vertexArray.size() == 6);

                Debug.debugAssert(edgeArray[0].boundary());
                Debug.debugAssert(edgeArray[1].boundary());
                Debug.debugAssert(edgeArray[2].boundary());
                Debug.debugAssert(edgeArray[3].boundary());
                Debug.debugAssert(edgeArray[4].boundary());
                Debug.debugAssert(edgeArray[5].boundary());

                Debug.debugAssert(faceArray[0].containsVertex(0));
                Debug.debugAssert(faceArray[0].containsVertex(1));
                Debug.debugAssert(faceArray[0].containsVertex(2));

                Debug.debugAssert(faceArray[1].containsVertex(5));
                Debug.debugAssert(faceArray[1].containsVertex(3));
                Debug.debugAssert(faceArray[1].containsVertex(4));

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);

                MeshAlg.weldAdjacency(geometry.vertexArray, faceArray, edgeArray, vertexArray);

                MeshAlg.debugCheckConsistency(faceArray, edgeArray, vertexArray);

                Debug.debugAssert(faceArray.size() == 2);
                Debug.debugAssert(edgeArray.size() == 5);
                Debug.debugAssert(vertexArray.size() == 6);
                Debug.debugAssert(!edgeArray[0].boundary());
                Debug.debugAssert(edgeArray[1].boundary());
                Debug.debugAssert(edgeArray[2].boundary());
                Debug.debugAssert(edgeArray[3].boundary());
                Debug.debugAssert(edgeArray[4].boundary());

            }

            {
                Array<Vector3> vertices = new Array<Vector3>();
                vertices.append(new Vector3(0, 0, 0));
                vertices.append(new Vector3(1, 0, 0));
                vertices.append(new Vector3(1, 1, 0));
                vertices.append(new Vector3(2, 0, 0));
                vertices.append(new Vector3(1, 0, 0));

                Array<int> indices = new Array<int>();
                indices.append(0, 1, 2);
                indices.append(2, 4, 3);

                Array<MeshAlg.Face> faces = new Array<MeshAlg.Face>();
                Array<MeshAlg.Edge> edges = new Array<MeshAlg.Edge>();
                Array<MeshAlg.Vertex> meshVertices = new Array<MeshAlg.Vertex>();
                MeshAlg.computeAdjacency(vertices, indices, faces, edges, meshVertices);

                CheckEqual(2, faces.size(), "MeshAlg.computeAdjacency welded case face count");
                CheckEqual(6, edges.size(), "MeshAlg.computeAdjacency welded case edge count");
                CheckEqual(5, meshVertices.size(), "MeshAlg.computeAdjacency welded case vertex count");
                CheckEqual(6, MeshAlg.countBoundaryEdges(edges), "MeshAlg.computeAdjacency welded case boundary count");

                MeshAlg.weldAdjacency(vertices, faces, edges, meshVertices);
                MeshAlg.debugCheckConsistency(faces, edges, meshVertices);

                CheckEqual(2, faces.size(), "MeshAlg.weldAdjacency welded case face count");
                CheckEqual(5, edges.size(), "MeshAlg.weldAdjacency welded case edge count");
                CheckEqual(5, meshVertices.size(), "MeshAlg.weldAdjacency welded case vertex count");
                CheckEqual(4, MeshAlg.countBoundaryEdges(edges), "MeshAlg.weldAdjacency welded case boundary count");
                Check(!edges[0].boundary(), "MeshAlg.weldAdjacency welded case shared edge");
                Check(faces[0].containsVertex(0) && faces[0].containsVertex(4) && faces[0].containsVertex(2), "MeshAlg.weldAdjacency welded case first face");
                Check(faces[1].containsVertex(2) && faces[1].containsVertex(4) && faces[1].containsVertex(3), "MeshAlg.weldAdjacency welded case second face");
            }

            {
                Array<Vector3> vertices = new Array<Vector3>();
                vertices.append(new Vector3(0, 0, 0));
                vertices.append(new Vector3(1, 0, 0));
                vertices.append(new Vector3(1, 1, 0));
                vertices.append(new Vector3(0, 1, 0));
                vertices.append(new Vector3(0, 0, 1));
                vertices.append(new Vector3(1, 0, 1));
                vertices.append(new Vector3(1, 1, 1));
                vertices.append(new Vector3(0, 1, 1));

                Array<int> indices = new Array<int>();
                indices.append(0, 2, 1);
                indices.append(0, 3, 2);
                indices.append(4, 5, 6);
                indices.append(4, 6, 7);
                indices.append(0, 1, 5);
                indices.append(0, 5, 4);
                indices.append(1, 2, 6);
                indices.append(1, 6, 5);
                indices.append(2, 3, 7);
                indices.append(2, 7, 6);
                indices.append(3, 0, 4);
                indices.append(3, 4, 7);

                Array<MeshAlg.Face> faces = new Array<MeshAlg.Face>();
                Array<MeshAlg.Edge> edges = new Array<MeshAlg.Edge>();
                Array<MeshAlg.Vertex> meshVertices = new Array<MeshAlg.Vertex>();
                MeshAlg.computeAdjacency(vertices, indices, faces, edges, meshVertices);

                CheckEqual(12, faces.size(), "MeshAlg.computeAdjacency cube face count");
                CheckEqual(18, edges.size(), "MeshAlg.computeAdjacency cube edge count");
                CheckEqual(8, meshVertices.size(), "MeshAlg.computeAdjacency cube vertex count");
                CheckEqual(0, MeshAlg.countBoundaryEdges(edges), "MeshAlg.computeAdjacency cube boundary count");

                for (int e = 0; e < edges.size(); ++e)
                {
                    Check(edges[e].faceIndex[0] != MeshAlg.Face.NONE && edges[e].faceIndex[1] != MeshAlg.Face.NONE, "MeshAlg.computeAdjacency cube manifold edge");
                    Check(edges[e].inFace(edges[e].faceIndex[0]) && edges[e].inFace(edges[e].faceIndex[1]), "MeshAlg.computeAdjacency cube edge faces");
                }

                for (int f = 0; f < faces.size(); ++f)
                {
                    for (int i = 0; i < 3; ++i)
                    {
                        int directedEdge = faces[f].edgeIndex[i];
                        int edgeIndex = directedEdge >= 0 ? directedEdge : ~directedEdge;
                        int e0 = edges[edgeIndex].vertexIndex[0];
                        int e1 = edges[edgeIndex].vertexIndex[1];
                        bool validPair = false;
                        for (int p = 0; p < 3; ++p)
                        {
                            int v0 = faces[f].vertexIndex[p];
                            int v1 = faces[f].vertexIndex[(p + 1) % 3];
                            if ((e0 == v0 && e1 == v1) || (e0 == v1 && e1 == v0))
                            {
                                validPair = true;
                            }
                        }
                        Check(validPair, "MeshAlg.computeAdjacency cube face edge vertices");
                    }
                }

                MeshAlg.debugCheckConsistency(faces, edges, meshVertices);
            }

            printf("passed\n");

        }
    }
}