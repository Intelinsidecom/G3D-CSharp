using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testConvexPolyhedron()
        {
            printf("ConvexPolyhedron ");

            Array<Vector3> squareVertices = new Array<Vector3>();
            squareVertices.append(new Vector3(0, 0, 0));
            squareVertices.append(new Vector3(1, 0, 0));
            squareVertices.append(new Vector3(1, 1, 0));
            squareVertices.append(new Vector3(0, 1, 0));

            ConvexPolygon square = new ConvexPolygon(squareVertices);
            CheckEqual(1.0, square.getArea(), "ConvexPolygon area");
            Check(square.normal().fuzzyEq(Vector3.unitZ()), "ConvexPolygon normal");
            Check(square.inverse().normal().fuzzyEq(-Vector3.unitZ()), "ConvexPolygon inverse normal");
            CheckEqual(1.0, square.inverse().getArea(), "ConvexPolygon inverse area");

            ConvexPolygon abovePolygon = new ConvexPolygon();
            ConvexPolygon belowPolygon = new ConvexPolygon();
            DirectedEdge cutEdge = new DirectedEdge();
            square.cut(new Plane(Vector3.unitX(), new Vector3(0.5f, 0, 0)), abovePolygon, belowPolygon, ref cutEdge);
            CheckEqual(0.5, abovePolygon.getArea(), "ConvexPolygon cut above area");
            CheckEqual(0.5, belowPolygon.getArea(), "ConvexPolygon cut below area");
            Check(abovePolygon.numVertices() == 4, "ConvexPolygon cut above vertices");
            Check(belowPolygon.numVertices() == 4, "ConvexPolygon cut below vertices");
            Check(cutEdge.start.fuzzyEq(new Vector3(0.5f, 0, 0)), "ConvexPolygon cut edge start");
            Check(cutEdge.stop.fuzzyEq(new Vector3(0.5f, 1, 0)), "ConvexPolygon cut edge stop");

            ConvexPolygon emptyPolygon = new ConvexPolygon();
            Check(emptyPolygon.isEmpty(), "ConvexPolygon empty");
            CheckEqual(0.0, emptyPolygon.getArea(), "ConvexPolygon empty area");

            Array<Vector3> duplicateVertices = new Array<Vector3>();
            duplicateVertices.append(new Vector3(0, 0, 0));
            duplicateVertices.append(new Vector3(0, 0, 0));
            duplicateVertices.append(new Vector3(1, 0, 0));
            duplicateVertices.append(new Vector3(0, 1, 0));
            ConvexPolygon duplicate = new ConvexPolygon(duplicateVertices);
            duplicate.removeDuplicateVertices();
            CheckEqual(3, duplicate.numVertices(), "ConvexPolygon duplicate removal");

            Array<ConvexPolygon> faces = new Array<ConvexPolygon>();
            faces.append(new ConvexPolygon(makeFace(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0))));
            faces.append(new ConvexPolygon(makeFace(new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1))));
            faces.append(new ConvexPolygon(makeFace(new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1))));
            faces.append(new ConvexPolygon(makeFace(new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1))));
            faces.append(new ConvexPolygon(makeFace(new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1))));
            faces.append(new ConvexPolygon(makeFace(new Vector3(0, 1, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1))));

            ConvexPolyhedron cube = new ConvexPolyhedron(faces);
            Check(!cube.isEmpty(), "ConvexPolyhedron is not empty");
            CheckEqual(1.0, cube.getVolume(), "ConvexPolyhedron volume");
            CheckEqual(6, cube.face.size(), "ConvexPolyhedron face count");

            faces[0].setVertex(0, new Vector3(9, 9, 9));
            CheckEqual(1.0, cube.getVolume(), "ConvexPolyhedron copy semantics");

            ConvexPolyhedron above = new ConvexPolyhedron();
            ConvexPolyhedron below = new ConvexPolyhedron();
            cube.cut(new Plane(Vector3.unitX(), new Vector3(0.5f, 0, 0)), above, below);
            Check(!above.isEmpty(), "ConvexPolyhedron cut above is not empty");
            Check(!below.isEmpty(), "ConvexPolyhedron cut below is not empty");
            CheckEqual(0.5, above.getVolume(), "ConvexPolyhedron cut above volume");
            CheckEqual(0.5, below.getVolume(), "ConvexPolyhedron cut below volume");
            CheckEqual(6, cube.face.size(), "ConvexPolyhedron cut preserves source");

            ConvexPolyhedron aboveEmpty = new ConvexPolyhedron();
            ConvexPolyhedron belowFull = new ConvexPolyhedron();
            cube.cut(new Plane(Vector3.unitX(), new Vector3(2, 0, 0)), aboveEmpty, belowFull);
            Check(aboveEmpty.isEmpty(), "ConvexPolyhedron cut entirely below empty");
            CheckEqual(1.0, belowFull.getVolume(), "ConvexPolyhedron cut entirely below volume");

            ConvexPolyhedron aboveFull = new ConvexPolyhedron();
            ConvexPolyhedron belowEmpty = new ConvexPolyhedron();
            cube.cut(new Plane(Vector3.unitX(), new Vector3(-1, 0, 0)), aboveFull, belowEmpty);
            CheckEqual(1.0, aboveFull.getVolume(), "ConvexPolyhedron cut entirely above volume");
            Check(belowEmpty.isEmpty(), "ConvexPolyhedron cut entirely above empty");

            ConvexPolyhedron emptyPolyhedron = new ConvexPolyhedron();
            Check(emptyPolyhedron.isEmpty(), "ConvexPolyhedron empty");
            CheckEqual(0.0, emptyPolyhedron.getVolume(), "ConvexPolyhedron empty volume");

            Array<Vector2> polygon2DVertices = new Array<Vector2>();
            polygon2DVertices.append(new Vector2(0, 0));
            polygon2DVertices.append(new Vector2(1, 1));
            polygon2DVertices.append(new Vector2(2, 0));
            ConvexPolygon2D polygon2D = new ConvexPolygon2D(polygon2DVertices);
            CheckEqual(3, polygon2D.numVertices(), "ConvexPolygon2D vertex count");
            Check(polygon2D.vertex(0).fuzzyEq(new Vector2(0, 0)), "ConvexPolygon2D vertex");
            Check(polygon2D.contains(new Vector2(1, 0.5f)), "ConvexPolygon2D contains");
            Check(!polygon2D.contains(new Vector2(10, 2)), "ConvexPolygon2D excludes");
            Check(!polygon2D.contains(new Vector2(1, 0.5f), true), "ConvexPolygon2D reverse winding");

            ConvexPolygon2D reversedPolygon2D = new ConvexPolygon2D(polygon2DVertices, true);
            Check(!reversedPolygon2D.contains(new Vector2(1, 0.5f)), "ConvexPolygon2D reversed constructor");
            Check(reversedPolygon2D.contains(new Vector2(1, 0.5f), true), "ConvexPolygon2D reversed winding");

            printf("passed\n");
        }

        private static Array<Vector3> makeFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Array<Vector3> result = new Array<Vector3>();
            result.append(a, b, c, d);
            return result;
        }
    }
}
