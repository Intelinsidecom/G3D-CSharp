/**
  @file tesselate.h
 
  @maintainer Morgan McGuire, matrix@graphics3d.com
 
  @created 2003-05-01
  @edited  2003-05-01

  Copyright 2000-2003, Morgan McGuire.
  All rights reserved.
 */

using System;
using System.Collections.Generic;

namespace G3D
{
    /**
     Tesselates a complex polygon into a triangle set which is appended
     to the output.  The input is a series of counter-clockwise winding
     vertices, where the last is implicitly connected to the first.
     Self-intersections are allowed; "inside" is determined by an "odd"
     winding rule.  You may need to introduce a sliver polygon to cut
     holes out of the center.
     */
    public static class Tesselate
    {
        private class Point2
        {
            public double x;
            public double y;
            public double z;

            public Point2(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vector3 toVector3()
            {
                return new Vector3((float)x, (float)y, (float)z);
            }
        }

        public static void tesselateComplexPolygon(Array<Vector3> input, Array<Triangle> output)
        {
            int count = input.size();
            if (count < 3)
            {
                return;
            }

            List<Point2> polygon = new List<Point2>();
            for (int i = 0; i < count; ++i)
            {
                Vector3 v = input[i];
                polygon.Add(new Point2(v.x, v.y, v.z));
            }

            List<Point2> cleaned = new List<Point2>();
            for (int i = 0; i < polygon.Count; ++i)
            {
                Point2 p = polygon[i];
                Point2 q = polygon[(i + 1) % polygon.Count];
                if ((p.x != q.x) || (p.y != q.y))
                {
                    cleaned.Add(p);
                }
            }
            polygon = cleaned;

            if (polygon.Count < 3)
            {
                return;
            }

            if (hasSelfIntersection(polygon))
            {
                tesselateSelfIntersecting(polygon, output);
            }
            else
            {
                earClip(polygon, output);
            }
        }

        private static bool hasSelfIntersection(List<Point2> polygon)
        {
            int n = polygon.Count;
            for (int i = 0; i < n; ++i)
            {
                Point2 a1 = polygon[i];
                Point2 a2 = polygon[(i + 1) % n];
                for (int j = i + 1; j < n; ++j)
                {
                    if (j == i + 1)
                    {
                        continue;
                    }
                    if ((i == 0) && (j == n - 1))
                    {
                        continue;
                    }

                    Point2 b1 = polygon[j];
                    Point2 b2 = polygon[(j + 1) % n];
                    if (segmentsProperlyIntersect(a1, a2, b1, b2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool segmentsProperlyIntersect(Point2 p1, Point2 p2, Point2 p3, Point2 p4)
        {
            double d1 = cross(p3, p4, p1);
            double d2 = cross(p3, p4, p2);
            double d3 = cross(p1, p2, p3);
            double d4 = cross(p1, p2, p4);

            return (((d1 > 0) && (d2 < 0)) || ((d1 < 0) && (d2 > 0))) &&
                   (((d3 > 0) && (d4 < 0)) || ((d3 < 0) && (d4 > 0)));
        }

        private static bool intersectSegments(Point2 p1, Point2 p2, Point2 p3, Point2 p4, out double t, out double u)
        {
            t = 0;
            u = 0;

            double rx = p2.x - p1.x;
            double ry = p2.y - p1.y;
            double sx = p4.x - p3.x;
            double sy = p4.y - p3.y;

            double denominator = rx * sy - ry * sx;
            if (denominator == 0)
            {
                return false;
            }

            double dx = p3.x - p1.x;
            double dy = p3.y - p1.y;

            double tt = (dx * sy - dy * sx) / denominator;
            double uu = (dx * ry - dy * rx) / denominator;

            if ((tt <= 0) || (tt >= 1) || (uu <= 0) || (uu >= 1))
            {
                return false;
            }

            t = tt;
            u = uu;
            return true;
        }

        private static double cross(Point2 a, Point2 b, Point2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        private static double polygonScale(List<Point2> polygon)
        {
            double minX = polygon[0].x;
            double maxX = polygon[0].x;
            double minY = polygon[0].y;
            double maxY = polygon[0].y;

            for (int i = 1; i < polygon.Count; ++i)
            {
                minX = Math.Min(minX, polygon[i].x);
                maxX = Math.Max(maxX, polygon[i].x);
                minY = Math.Min(minY, polygon[i].y);
                maxY = Math.Max(maxY, polygon[i].y);
            }

            double dx = maxX - minX;
            double dy = maxY - minY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void earClip(List<Point2> polygon, Array<Triangle> output)
        {
            int n = polygon.Count;

            double area2 = 0;
            for (int i = 0; i < n; ++i)
            {
                Point2 a = polygon[i];
                Point2 b = polygon[(i + 1) % n];
                area2 += a.x * b.y - b.x * a.y;
            }

            int orientation = (area2 >= 0) ? 1 : -1;

            List<Point2> remaining = new List<Point2>(polygon);

            while (remaining.Count > 3)
            {
                int count = remaining.Count;
                bool clipped = false;

                for (int i = 1; i < count; ++i)
                {
                    int previousIndex = (i - 1 + count) % count;
                    int nextIndex = (i + 1) % count;

                    Point2 a = remaining[previousIndex];
                    Point2 b = remaining[i];
                    Point2 c = remaining[nextIndex];

                    double turn = cross(a, b, c);
                    if (turn * orientation <= 0)
                    {
                        continue;
                    }

                    bool contains = false;
                    for (int j = 0; j < count; ++j)
                    {
                        if ((j == previousIndex) || (j == i) || (j == nextIndex))
                        {
                            continue;
                        }
                        if (pointInTriangle(remaining[j], a, b, c, orientation))
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (contains)
                    {
                        continue;
                    }

                    output.append(new Triangle(a.toVector3(), b.toVector3(), c.toVector3()));
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    int best = -1;
                    double bestTurn = 0;
                    for (int i = 0; i < count; ++i)
                    {
                        int previousIndex = (i - 1 + count) % count;
                        int nextIndex = (i + 1) % count;

                        double turn = Math.Abs(cross(remaining[previousIndex], remaining[i], remaining[nextIndex])) * orientation;
                        if (turn > bestTurn)
                        {
                            bestTurn = turn;
                            best = i;
                        }
                    }

                    if (best < 0)
                    {
                        break;
                    }

                    int bestPrevious = (best - 1 + count) % count;
                    int bestNext = (best + 1) % count;
                    output.append(new Triangle(
                        remaining[bestPrevious].toVector3(),
                        remaining[best].toVector3(),
                        remaining[bestNext].toVector3()));
                    remaining.RemoveAt(best);
                }
            }

            if (remaining.Count == 3)
            {
                output.append(new Triangle(
                    remaining[0].toVector3(),
                    remaining[1].toVector3(),
                    remaining[2].toVector3()));
            }
        }

        private static bool pointInTriangle(Point2 p, Point2 a, Point2 b, Point2 c, int orientation)
        {
            double c1 = cross(a, b, p);
            double c2 = cross(b, c, p);
            double c3 = cross(c, a, p);

            if (orientation > 0)
            {
                return (c1 > 0) && (c2 > 0) && (c3 > 0);
            }
            else
            {
                return (c1 < 0) && (c2 < 0) && (c3 < 0);
            }
        }

        private static void tesselateSelfIntersecting(List<Point2> polygon, Array<Triangle> output)
        {
            int n = polygon.Count;

            List<double> nodeX = new List<double>();
            List<double> nodeY = new List<double>();
            List<double> nodeZ = new List<double>();

            List<List<KeyValuePair<double, int>>> edgeNodes = new List<List<KeyValuePair<double, int>>>();
            for (int i = 0; i < n; ++i)
            {
                edgeNodes.Add(new List<KeyValuePair<double, int>>());
            }

            for (int i = 0; i < n; ++i)
            {
                int node = addNode(nodeX, nodeY, nodeZ, polygon[i].x, polygon[i].y, polygon[i].z);
                edgeNodes[i].Add(new KeyValuePair<double, int>(0.0, node));
                edgeNodes[(i + 1) % n].Add(new KeyValuePair<double, int>(1.0, node));
            }

            for (int i = 0; i < n; ++i)
            {
                Point2 a1 = polygon[i];
                Point2 a2 = polygon[(i + 1) % n];

                for (int j = i + 1; j < n; ++j)
                {
                    if (j == i + 1)
                    {
                        continue;
                    }
                    if ((i == 0) && (j == n - 1))
                    {
                        continue;
                    }

                    Point2 b1 = polygon[j];
                    Point2 b2 = polygon[(j + 1) % n];

                    double t, u;
                    if (!intersectSegments(a1, a2, b1, b2, out t, out u))
                    {
                        continue;
                    }

                    int node = addNode(nodeX, nodeY, nodeZ,
                        a1.x + (a2.x - a1.x) * t,
                        a1.y + (a2.y - a1.y) * t,
                        a1.z + (a2.z - a1.z) * t);

                    edgeNodes[i].Add(new KeyValuePair<double, int>(t, node));
                    edgeNodes[j].Add(new KeyValuePair<double, int>(u, node));
                }
            }

            mergeCoincidentNodes(nodeX, nodeY, nodeZ, edgeNodes, polygonScale(polygon));

            List<int> halfFrom = new List<int>();
            List<int> halfTo = new List<int>();
            List<double> halfAngle = new List<double>();

            for (int i = 0; i < n; ++i)
            {
                edgeNodes[i].Sort(delegate(KeyValuePair<double, int> a, KeyValuePair<double, int> b)
                {
                    return a.Key.CompareTo(b.Key);
                });

                for (int k = 0; k < edgeNodes[i].Count - 1; ++k)
                {
                    int a = edgeNodes[i][k].Value;
                    int b = edgeNodes[i][k + 1].Value;
                    if (a == b)
                    {
                        continue;
                    }
                    addHalfEdge(halfFrom, halfTo, halfAngle, a, b, nodeX, nodeY);
                    addHalfEdge(halfFrom, halfTo, halfAngle, b, a, nodeX, nodeY);
                }
            }

            int halfCount = halfFrom.Count;
            if (halfCount == 0)
            {
                return;
            }

            List<List<int>> outgoing = new List<List<int>>();
            for (int i = 0; i < nodeX.Count; ++i)
            {
                outgoing.Add(new List<int>());
            }
            for (int h = 0; h < halfCount; ++h)
            {
                outgoing[halfFrom[h]].Add(h);
            }

            int[] next = new int[halfCount];
            for (int h = 0; h < halfCount; ++h)
            {
                int u = halfFrom[h];
                int v = halfTo[h];

                int twin = -1;
                List<int> outAtV = outgoing[v];
                for (int k = 0; k < outAtV.Count; ++k)
                {
                    if (halfTo[outAtV[k]] == u)
                    {
                        twin = outAtV[k];
                        break;
                    }
                }

                if (twin < 0)
                {
                    next[h] = h;
                    continue;
                }

                double twinAngle = halfAngle[twin];
                int best = twin;
                double bestRelative = -1;
                for (int k = 0; k < outAtV.Count; ++k)
                {
                    double relative = halfAngle[outAtV[k]] - twinAngle;
                    if (relative < 0)
                    {
                        relative += 2 * Math.PI;
                    }
                    if (relative > bestRelative)
                    {
                        bestRelative = relative;
                        best = outAtV[k];
                    }
                }
                next[h] = best;
            }

            double scale = polygonScale(polygon);
            double epsilon = (scale > 0) ? (1e-6 * scale) : 1e-9;

            bool[] visited = new bool[halfCount];
            for (int h = 0; h < halfCount; ++h)
            {
                if (visited[h])
                {
                    continue;
                }

                List<int> walk = new List<int>();
                int current = h;
                int guard = 0;
                while ((!visited[current]) && (guard <= halfCount))
                {
                    visited[current] = true;
                    walk.Add(current);
                    current = next[current];
                    ++guard;
                }

                if (walk.Count < 3)
                {
                    continue;
                }

                int first = walk[0];
                double ax = nodeX[halfFrom[first]];
                double ay = nodeY[halfFrom[first]];
                double bx = nodeX[halfTo[first]];
                double by = nodeY[halfTo[first]];

                double dx = bx - ax;
                double dy = by - ay;
                double length = Math.Sqrt(dx * dx + dy * dy);
                if (length == 0)
                {
                    continue;
                }

                double interiorX = (ax + bx) * 0.5 + (-dy / length) * epsilon;
                double interiorY = (ay + by) * 0.5 + (dx / length) * epsilon;

                int winding = windingNumber(polygon, interiorX, interiorY);
                if ((winding & 1) == 0)
                {
                    continue;
                }

                List<Point2> face = new List<Point2>();
                for (int k = 0; k < walk.Count; ++k)
                {
                    int node = halfFrom[walk[k]];
                    face.Add(new Point2(nodeX[node], nodeY[node], nodeZ[node]));
                }

                earClip(face, output);
            }
        }

        private static int addNode(List<double> nodeX, List<double> nodeY, List<double> nodeZ, double x, double y, double z)
        {
            nodeX.Add(x);
            nodeY.Add(y);
            nodeZ.Add(z);
            return nodeX.Count - 1;
        }

        private static void mergeCoincidentNodes(
            List<double> nodeX,
            List<double> nodeY,
            List<double> nodeZ,
            List<List<KeyValuePair<double, int>>> edgeNodes,
            double scale)
        {
            int nodeCount = nodeX.Count;
            if (nodeCount < 2)
            {
                return;
            }

            double tolerance = 1e-9 * ((scale > 0) ? scale : 1.0);

            int[] remap = new int[nodeCount];
            for (int i = 0; i < nodeCount; ++i)
            {
                remap[i] = i;
            }

            for (int i = 0; i < nodeCount; ++i)
            {
                for (int j = 0; j < i; ++j)
                {
                    if ((Math.Abs(nodeX[i] - nodeX[j]) <= tolerance) &&
                        (Math.Abs(nodeY[i] - nodeY[j]) <= tolerance))
                    {
                        remap[i] = remap[j];
                        break;
                    }
                }
            }

            for (int i = 0; i < nodeCount; ++i)
            {
                if (remap[i] != i)
                {
                    nodeX[i] = nodeX[remap[i]];
                    nodeY[i] = nodeY[remap[i]];
                    nodeZ[i] = nodeZ[remap[i]];
                }
            }

            for (int i = 0; i < edgeNodes.Count; ++i)
            {
                for (int k = 0; k < edgeNodes[i].Count; ++k)
                {
                    KeyValuePair<double, int> pair = edgeNodes[i][k];
                    edgeNodes[i][k] = new KeyValuePair<double, int>(pair.Key, remap[pair.Value]);
                }
            }
        }

        private static void addHalfEdge(
            List<int> halfFrom,
            List<int> halfTo,
            List<double> halfAngle,
            int from,
            int to,
            List<double> nodeX,
            List<double> nodeY)
        {
            double dx = nodeX[to] - nodeX[from];
            double dy = nodeY[to] - nodeY[from];

            double angle = Math.Atan2(dy, dx);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

            halfFrom.Add(from);
            halfTo.Add(to);
            halfAngle.Add(angle);
        }

        private static int windingNumber(List<Point2> polygon, double px, double py)
        {
            int winding = 0;
            int n = polygon.Count;

            for (int i = 0; i < n; ++i)
            {
                Point2 vi = polygon[i];
                Point2 vj = polygon[(i + 1) % n];

                if (vi.y <= py)
                {
                    if ((vj.y > py) && (isLeft(vi, vj, px, py) > 0))
                    {
                        ++winding;
                    }
                }
                else
                {
                    if ((vj.y <= py) && (isLeft(vi, vj, px, py) < 0))
                    {
                        --winding;
                    }
                }
            }

            return winding;
        }

        private static double isLeft(Point2 vi, Point2 vj, double px, double py)
        {
            return (vj.x - vi.x) * (py - vi.y) - (px - vi.x) * (vj.y - vi.y);
        }
    }
}
