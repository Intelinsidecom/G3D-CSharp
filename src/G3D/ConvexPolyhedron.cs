/**
  @file ConvexPolyhedron.h
  
  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2001-11-11
  @edited  2006-04-10
 
  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

/**
  @file ConvexPolyhedron.cpp
  
  @author Morgan McGuire, morgan@graphics3d.com

  @created 2001-11-11
  @edited  2006-01-10
 
  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */
namespace G3D
{
    public struct DirectedEdge
    {
        public Vector3 start;
        public Vector3 stop;
    }

    public class ConvexPolygon
    {
        internal Array<Vector3> _vertex;

        public ConvexPolygon()
        {
            _vertex = new Array<Vector3>();
        }

        public ConvexPolygon(Array<Vector3> __vertex)
        {
            _vertex = new Array<Vector3>(__vertex);
        }

        public ConvexPolygon(ConvexPolygon other)
        {
            _vertex = new Array<Vector3>(other._vertex);
        }

        /**
         Counter clockwise winding order.  
         */
        public Vector3 vertex(int i)
        {
            return _vertex[i];
        }

        public void setVertex(int i, Vector3 v)
        {
            _vertex[i] = v;
        }

        /**
         Zero vertices indicates an empty polygon (zero area).
         */
        public int numVertices()
        {
            return _vertex.size();
        }

        public void setNumVertices(int n)
        {
            _vertex.resize(n);
        }

        /**
         O(n) in the number of edges
         */
        public bool isEmpty()
        {
            return (_vertex.length() == 0) || (getArea() <= g3dmath.fuzzyEpsilon);
        }

        /**
           Cuts the polygon at the plane. If the polygon is entirely above or below
           the plane, one of the returned polygons will be empty.
         
           @param above The part of the polygon above (on the side the
                  normal points to or in the plane) the plane
           @param below The part of the polygon below the plane.
           @param newEdge  If a new edge was introduced, this is that edge (on the above portion; the below portion is the opposite winding.
        */
        public void cut(Plane plane, ConvexPolygon above, ConvexPolygon below, ref DirectedEdge newEdge)
        {
            above._vertex.resize(0);
            below._vertex.resize(0);

            if (isEmpty())
            {
                //debugPrintf("Empty\n");
                return;
            }

            int v = 0;
            int length = _vertex.length();

            Vector3 polyNormal = normal();
            Vector3 planeNormal = plane.normal();

            // See if the polygon is *in* the plane.
            if (planeNormal.fuzzyEq(polyNormal) || planeNormal.fuzzyEq(-polyNormal))
            {
                // Polygon is parallel to the plane.  It must be either above,
                // below, or in the plane.

                double a, b, c, d;
                Vector3 pt = _vertex[0];

                plane.getEquation(out a, out b, out c, out d);
                float r = (float)(a * pt.x + b * pt.y + c * pt.z + d);

                if (g3dmath.fuzzyGe(r, 0))
                {
                    // The polygon is entirely in the plane.
                    //debugPrintf("Entirely above\n");
                    above.copyFrom(this);
                    return;
                }
                else
                {
                    //debugPrintf("Entirely below (1)\n");
                    below.copyFrom(this);
                    return;
                }
            }

            // Number of edges crossing the plane.  Used for 
            // debug assertions.
            int count = 0;

            // True when the last _vertex we looked at was above the plane
            bool lastAbove = plane.halfSpaceContains(_vertex[v]);

            if (lastAbove)
            {
                above._vertex.append(_vertex[v]);
            }
            else
            {
                below._vertex.append(_vertex[v]);
            }

            for (v = 1; v < length; v++)
            {
                bool isAbove = plane.halfSpaceContains(_vertex[v]);

                if (lastAbove ^ isAbove)
                {
                    // Switched sides.
                    // Create an interpolated point that lies
                    // in the plane, between the two points.
                    Line line = Line.fromTwoPoints(_vertex[v - 1], _vertex[v]);
                    Vector3 interp = line.intersection(plane);

                    if (!interp.isFinite())
                    {
                        // Since the polygon is not in the plane (we checked above), 
                        // it must be the case that this edge (and only this edge)
                        // is in the plane.  This only happens when the polygon is
                        // entirely below the plane except for one edge.  This edge
                        // forms a degenerate polygon, so just treat the whole polygon
                        // as below the plane.
                        below.copyFrom(this);
                        above._vertex.resize(0);
                        //debugPrintf("Entirely below\n");
                        return;
                    }

                    above._vertex.append(interp);
                    below._vertex.append(interp);
                    if (lastAbove)
                    {
                        newEdge.stop = interp;
                    }
                    else
                    {
                        newEdge.start = interp;
                    }
                    count++;
                }

                lastAbove = isAbove;
                if (lastAbove)
                {
                    above._vertex.append(_vertex[v]);
                }
                else
                {
                    below._vertex.append(_vertex[v]);
                }
            }

            // Loop back to the first point, seeing if an interpolated point is
            // needed.
            bool lastIsAbove = plane.halfSpaceContains(_vertex[0]);
            if (lastAbove ^ lastIsAbove)
            {
                Line line = Line.fromTwoPoints(_vertex[length - 1], _vertex[0]);
                Vector3 interp = line.intersection(plane);
                if (!interp.isFinite())
                {
                    // Since the polygon is not in the plane (we checked above), 
                    // it must be the case that this edge (and only this edge)
                    // is in the plane.  This only happens when the polygon is
                    // entirely below the plane except for one edge.  This edge
                    // forms a degenerate polygon, so just treat the whole polygon
                    // as below the plane.
                    below.copyFrom(this);
                    above._vertex.resize(0);
                    //debugPrintf("Entirely below\n");
                    return;
                }

                above._vertex.append(interp);
                below._vertex.append(interp);
                Debug.debugAssertM(count < 2, "Convex polygons may only intersect planes at two edges.");
                if (lastAbove)
                {
                    newEdge.stop = interp;
                }
                else
                {
                    newEdge.start = interp;
                }
                count++;
            }

            Debug.debugAssertM((count == 2) || (count == 0), "Convex polygons may only intersect planes at two edges.");
        }

        internal void copyFrom(ConvexPolygon other)
        {
            _vertex = new Array<Vector3>(other._vertex);
        }

        public void cut(Plane plane, ConvexPolygon above, ConvexPolygon below)
        {
            DirectedEdge edge = new DirectedEdge();
            cut(plane, above, below, ref edge);
        }

        /**
            When a cut plane grazes a vertex in the polygon, two near-identical vertices may be created.
            The closeness of these two points can cause a number of problems, such as ConvexPolygon::normal() 
            returning an infinite vector.  It should be noted, however, that not all applications are
            sensitive to near-identical vertices.

            removeDuplicateVertices() detects and eliminates redundant vertices.
        */
        public void removeDuplicateVertices()
        {
            // Any valid polygon should have 3 or more vertices, but why take chances?
            if (_vertex.size() >= 2)
            {
                // Remove duplicate vertices.
                for (int i = 0; i < _vertex.size() - 1; ++i)
                {
                    if (_vertex[i].fuzzyEq(_vertex[i + 1]))
                    {
                        _vertex.remove(i + 1);
                        --i; // Don't move forward.
                    }
                }

                // Check the last vertex against the first.
                if (_vertex[_vertex.size() - 1].fuzzyEq(_vertex[0]))
                {
                    _vertex.pop();
                }
            }
        }

        /**
         O(n) in the number of edges
         */
        public float getArea()
        {
            if (_vertex.length() < 3)
            {
                return 0;
            }

            float sum = 0;

            int length = _vertex.length();
            // Split into triangle fan, compute individual area
            for (int v = 2; v < length; v++)
            {
                int i0 = 0;
                int i1 = v - 1;
                int i2 = v;

                sum += (_vertex[i1] - _vertex[i0]).cross(_vertex[i2] - _vertex[i0]).magnitude() / 2;
            }

            return sum;
        }

        public Vector3 normal()
        {
            Debug.debugAssert(_vertex.length() >= 3);
            return (_vertex[1] - _vertex[0]).cross(_vertex[2] - _vertex[0]).direction();
        }

        /**
         Returns the same polygon with inverse winding.
         */
        public ConvexPolygon inverse()
        {
            ConvexPolygon result = new ConvexPolygon();
            int length = _vertex.length();
            result._vertex.resize(length);

            for (int v = 0; v < length; v++)
            {
                result._vertex[v] = _vertex[length - v - 1];
            }

            return result;
        }
    }

    public class ConvexPolyhedron
    {
        /**
         Zero faces indicates an empty polyhedron
         */
        public Array<ConvexPolygon> face;

        public ConvexPolyhedron()
        {
            face = new Array<ConvexPolygon>();
        }

        public ConvexPolyhedron(Array<ConvexPolygon> _face)
        {
            face = new Array<ConvexPolygon>();
            for (int i = 0; i < _face.size(); ++i)
            {
                face.append(new ConvexPolygon(_face[i]));
            }
        }

        public ConvexPolyhedron(ConvexPolyhedron other)
        {
            face = new Array<ConvexPolygon>();
            for (int i = 0; i < other.face.size(); ++i)
            {
                face.append(new ConvexPolygon(other.face[i]));
            }
        }

        /**
         O(n) in the number of edges
         */
        public bool isEmpty()
        {
            return (face.length() == 0) || (getVolume() <= g3dmath.fuzzyEpsilon);
        }

        /**
         O(n) in the number of edges
         */
        public float getVolume()
        {
            if (face.length() < 4)
            {
                return 0;
            }

            // The volume of any pyramid is 1/3 * h * base area.
            // Discussion at: http://nrich.maths.org/mathsf/journalf/oct01/art1/

            float sum = 0;

            // Choose the first _vertex of the first face as the origin.
            // This lets us skip one face, too, and avoids negative heights.
            Vector3 v0 = face[0]._vertex[0];
            for (int f = 1; f < face.length(); f++)
            {
                ConvexPolygon poly = face[f];

                float height = (poly._vertex[0] - v0).dot(poly.normal());
                float base_ = poly.getArea();

                sum += height * base_;
            }

            return sum / 3;
        }

        /**
           Cuts the polyhedron at the plane. If the polyhedron is entirely above or below
           the plane, one of the returned polyhedra will be empty.
         
           @param above The part of the polyhedron above (on the side the
                  normal points to or in the plane) the plane
           @param below The part of the polyhedron below the plane.
         */
        public void cut(Plane plane, ConvexPolyhedron above, ConvexPolyhedron below)
        {
            above.face.resize(0);
            below.face.resize(0);

            Array<DirectedEdge> edge = new Array<DirectedEdge>();

            int f;

            // See if the plane cuts this polyhedron at all.  Detect when
            // the polyhedron is entirely to one side or the other.
            int numAbove = 0, numIn = 0, numBelow = 0;
            bool ruledOut = false;
            double d;
            Vector3 abc;
            plane.getEquation(out abc, out d);

            // This number has to be fairly large to prevent precision problems down
            // the road.
            const float eps = 0.005f;
            for (f = face.length() - 1; (f >= 0) && (!ruledOut); f--)
            {
                ConvexPolygon poly = face[f];
                for (int v = poly._vertex.length() - 1; (v >= 0) && (!ruledOut); v--)
                {
                    double r = abc.dot(poly._vertex[v]) + d;
                    if (r > eps)
                    {
                        numAbove++;
                    }
                    else if (r < -eps)
                    {
                        numBelow++;
                    }
                    else
                    {
                        numIn++;
                    }

                    ruledOut = (numAbove != 0) && (numBelow != 0);
                }
            }

            if (numBelow == 0)
            {
                copyFrom(above, this);
                return;
            }
            else if (numAbove == 0)
            {
                copyFrom(below, this);
                return;
            }

            // Clip each polygon, collecting split edges.
            for (f = face.length() - 1; f >= 0; f--)
            {
                ConvexPolygon a = new ConvexPolygon();
                ConvexPolygon b = new ConvexPolygon();
                DirectedEdge e = new DirectedEdge();
                face[f].cut(plane, a, b, ref e);

                bool aEmpty = a.isEmpty();
                bool bEmpty = b.isEmpty();

                //debugPrintf("\n");
                if (!aEmpty)
                {
                    //debugPrintf(" Above %f\n", a.getArea());
                    above.face.append(a);
                }

                if (!bEmpty)
                {
                    //debugPrintf(" Below %f\n", b.getArea());
                    below.face.append(b);
                }

                if (!aEmpty && !bEmpty)
                {
                    //debugPrintf(" == Split\n");
                    edge.append(e);
                }
                else
                {
                    // Might be the case that the polygon is entirely on 
                    // one side of the plane yet there is an edge we need
                    // because it touches the plane.
                    // 
                    // Extract the non-empty _vertex list and examine it.
                    // If we find exactly one edge in the plane, add that edge.
                    Array<Vector3> _vertex = (aEmpty ? b._vertex : a._vertex);
                    int L = _vertex.length();
                    int count = 0;
                    for (int v = 0; v < L; v++)
                    {
                        if (plane.fuzzyContains(_vertex[v]) && plane.fuzzyContains(_vertex[(v + 1) % L]))
                        {
                            e.start = _vertex[v];
                            e.stop = _vertex[(v + 1) % L];
                            count++;
                        }
                    }

                    if (count == 1)
                    {
                        edge.append(e);
                    }
                }
            }

            if (above.face.length() == 1)
            {
                // Only one face above means that this entire 
                // polyhedron is below the plane.  Move that face over.
                below.face.append(above.face[0]);
                above.face.resize(0);
            }
            else if (below.face.length() == 1)
            {
                // This shouldn't happen, but it arises in practice
                // from numerical imprecision.
                above.face.append(below.face[0]);
                below.face.resize(0);
            }

            if ((above.face.length() > 0) && (below.face.length() > 0))
            {
                // The polyhedron was actually cut; create a cap polygon
                ConvexPolygon cap = new ConvexPolygon();

                // Collect the final polgyon by sorting the edges
                int numVertices = edge.length();

                // Need at least three points to make a polygon
                Debug.debugAssert(numVertices >= 3);

                Vector3 last_vertex = edge.last().stop;
                cap._vertex.append(last_vertex);

                // Search for the next _vertex.  Because of accumulating
                // numerical error, we have to find the closest match, not 
                // just the one we expect.
                for (int v = numVertices - 1; v >= 0; v--)
                {
                    // matching edge index
                    int index = 0;
                    int num = edge.length();
                    double distance = (edge[index].start - last_vertex).squaredMagnitude();
                    for (int e = 1; e < num; e++)
                    {
                        double eDistance = (edge[e].start - last_vertex).squaredMagnitude();

                        if (eDistance < distance)
                        {
                            // This is the new closest one
                            index = e;
                            distance = eDistance;
                        }
                    }

                    // Don't tolerate ridiculous error.
                    Debug.debugAssertM(distance < 0.02, "Edge missing while closing polygon.");

                    last_vertex = edge[index].stop;
                    cap._vertex.append(last_vertex);
                }

                //debugPrintf("\n");
                //debugPrintf("Cap (both) %f\n", cap.getArea());
                above.face.append(cap);
                below.face.append(cap.inverse());
            }

            // Make sure we put enough faces on each polyhedra
            Debug.debugAssert((above.face.length() == 0) || (above.face.length() >= 4));
            Debug.debugAssert((below.face.length() == 0) || (below.face.length() >= 4));
        }

        private static void copyFrom(ConvexPolyhedron target, ConvexPolyhedron source)
        {
            target.face = new Array<ConvexPolygon>();
            for (int i = 0; i < source.face.size(); ++i)
            {
                target.face.append(new ConvexPolygon(source.face[i]));
            }
        }
    }

    /**
     */
    public class ConvexPolygon2D
    {
        private Array<Vector2> m_vertex;

        public ConvexPolygon2D()
        {
            m_vertex = new Array<Vector2>();
        }

        /** 
         Points are counter-clockwise in a Y = down, X = right coordinate
         system.

         @param reverse If true, the points are reversed (i.e. winding direction is changed)
         before the polygon is created.
         */
        public ConvexPolygon2D(Array<Vector2> pts, bool reverse)
        {
            m_vertex = new Array<Vector2>(pts);
            if (reverse)
            {
                m_vertex.reverse();
            }
        }

        public ConvexPolygon2D(Array<Vector2> pts)
            : this(pts, false)
        {
        }

        public int numVertices()
        {
            return m_vertex.size();
        }

        public Vector2 vertex(int index)
        {
            Debug.debugAssert((index >= 0) && (index <= m_vertex.size()));
            return m_vertex[index];
        }

        /** @param reverseWinding If true, the winding direction of the polygon is reversed for this test.*/
        public bool contains(Vector2 p, bool reverseWinding)
        {
            // Compute the signed area of each polygon from p to an edge.  
            // If the area is non-negative for all polygons then p is inside 
            // the polygon.  (To adapt this algorithm for a concave polygon,
            // the *sum* of the areas must be non-negative).

            float r = reverseWinding ? -1 : 1;

            for (int i0 = 0; i0 < m_vertex.size(); ++i0)
            {
                int i1 = (i0 + 1) % m_vertex.size();
                Vector2 v0 = m_vertex[i0];
                Vector2 v1 = m_vertex[i1];

                Vector2 e0 = v0 - p;
                Vector2 e1 = v1 - p;

                // Area = (1/2) cross product, negated to be ccw in
                // a 2D space; we neglect the 1/2
                float area = -(e0.x * e1.y - e0.y * e1.x);

                if (area * r < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool contains(Vector2 p)
        {
            return contains(p, false);
        }
    }
}
