/**
  @file GLG3D/Shape.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2005-08-10
  @edited  2006-03-25
*/

using System;

namespace G3D
{
    /**
       Base class for other shapes.  G3D primitives like Box and Cylinder
       have no base class so that their implementations are maximally efficient,
       and have no "render" method because they are lower-level than the
       rendering API.  
       
       The Shape classes provide a parallel set of classes to the G3D primitives
       that support more functionality.
 
       Mesh shapes are intentionally immutable because they precompute data.
 
       <b>BETA API</b> This API is subject to change in future releases.
     */
    public abstract class Shape : ReferenceCountedObject
    {
        public enum Type { NONE = 0, MESH = 1, BOX, CYLINDER, SPHERE, RAY, CAPSULE, PLANE, AXES };

        public static string typeToString(Type t)
        {
            string[] name = new string[] 
                {"NONE", "MESH", "BOX", "CYLINDER", "SPHERE", "RAY", "CAPSULE", "PLANE", "AXES"};

            if (t <= Type.AXES)
            {
                return name[(int)t];
            } else
            {
                return "";
            }
        }

        public abstract Type type();

        public abstract void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor);

        public void render(
            RenderDevice rd, 
            CoordinateFrame cframe)
        {
            render(rd, cframe, new Color4(0.5f, 0.5f, 0.0f, 0.5f), new Color4(Color3.black(), 1.0f));
        }

        public void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor)
        {
            render(rd, cframe, solidColor, new Color4(Color3.black(), 1.0f));
        }

        public virtual Box box() { 
            Debug.debugAssertM(false, "Not a box");
            return new Box();
        }

        public virtual CoordinateFrame axes() {
            Debug.debugAssertM(false, "Not axes");
            return CoordinateFrame.identity();
        }

        public virtual Ray ray() {
            Debug.debugAssertM(false, "Not a ray");
            return new Ray();
        }

        public virtual Cylinder cylinder() { 
            Debug.debugAssertM(false, "Not a cylinder");
            return new Cylinder();
        }

        public virtual Sphere sphere() { 
            Debug.debugAssertM(false, "Not a sphere");
            return new Sphere();
        }

        public virtual Capsule capsule() { 
            Debug.debugAssertM(false, "Not a capsule");
            return new Capsule();
        }

        public virtual Plane plane() { 
            Debug.debugAssertM(false, "Not a plane");
            return new Plane();
        }

        public virtual Array<int> indexArray() {
            Debug.debugAssertM(false, "Not a mesh");
            return new Array<int>();
        }

        public virtual Array<Vector3> vertexArray() {
            Debug.debugAssertM(false, "Not a mesh");
            return new Array<Vector3>();
        }

        /** Surface area of the outside of this object. */
        public abstract float area();

        /** Volume of the interior of this object. */
        public abstract float volume();

        /** Center of mass for this object. */
        public abstract Vector3 center();

        /** Bounding sphere of this object. */
        public abstract Sphere boundingSphere();

        /** Bounding axis aligned box of this object. */
        public abstract AABox boundingAABox();

        /** A point selected uniformly at random with respect to the
            surface area of this object.  Not available on the Plane or
            Ray, which have infinite extent.  The normal has unit length
            and points out of the surface.*/
        public abstract void getRandomSurfacePoint(out Vector3 P, out Vector3 N);

        public void getRandomSurfacePoint(out Vector3 P) {
            Vector3 N;
            getRandomSurfacePoint(out P, out N);
        }

        /** A point selected uniformly at random with respect to the
            volume of this object.  Not available on objects with infinite
            extent.*/
        public abstract Vector3 randomInteriorPoint();
    };

    /** 
     Mesh shape is intended for debugging and for collision detection.  It is not a general purpose
     mesh. 

     @sa G3D::PosedModel, G3D::IFSModel, G3D::MD2Model, G3D::MeshAlg, contrib/ArticulatedModel/ArticulatedModel.h
     */
    public class MeshShape : Shape
    {
        private Array<Vector3>      _vertexArray;
        private Array<int>          _indexArray;

        /** If true, the bspTree and _area, _boundingSphere, _boundingAABox have been initialized. */
        private bool                _hasTree;
        private AABSPTree<Triangle> _bspTree;
        private double              _area;
        private Sphere              _boundingSphere;
        private AABox               _boundingAABox;

        /** Creates _bspTree */
        private void buildBSP() {
            Debug.debugAssert(! _hasTree);

            _area = 0;
            for (int i = 0; i < _indexArray.size(); i += 3) {
                int v0 = _indexArray[i];
                int v1 = _indexArray[i + 1];
                int v2 = _indexArray[i + 2];

                Triangle tri = new Triangle(_vertexArray[v0], _vertexArray[v1], _vertexArray[v2]);
                _area += tri.area();
                _bspTree.insert(tri);
            }
            _bspTree.balance();

            Box box = new Box();
            Sphere sphere = new Sphere();
            MeshAlg.computeBounds(_vertexArray, ref box, ref sphere);
            _boundingSphere = sphere;
            _boundingSphere.getBounds(out _boundingAABox);

            _hasTree = true;
        }

        public MeshShape(Array<Vector3> vertex, Array<int> index) {
            _vertexArray = new Array<Vector3>(vertex);
            _indexArray = new Array<int>(index);
            _bspTree = new AABSPTree<Triangle>();
            _hasTree = false;

            Debug.debugAssert(index.size() % 3 == 0);
            Debug.debugAssert(index.size() >= 0);
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();

            rd.pushState();
                rd.setObjectToWorldMatrix(cframe0 * cframe);
                if (solidColor.a < 1.0) {
                    rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                }
                rd.setColor(solidColor);
                rd.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
                for (int i = 0; i < _indexArray.size(); i += 3) {
                    Vector3 v0 = _vertexArray[_indexArray[i + 0]];
                    Vector3 v1 = _vertexArray[_indexArray[i + 1]];
                    Vector3 v2 = _vertexArray[_indexArray[i + 2]];
                    rd.sendVertex(v0);
                    rd.sendVertex(v1);
                    rd.sendVertex(v2);
                }
                rd.endPrimitive();

                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                rd.setLineWidth(2);
                rd.setColor(wireColor);
                rd.beginPrimitive(RenderDevice.Primitive.LINES);
                    for (int i = 0; i < _indexArray.size(); i += 3) {
                        Vector3 v0 = _vertexArray[_indexArray[i + 0]];
                        Vector3 v1 = _vertexArray[_indexArray[i + 1]];
                        Vector3 v2 = _vertexArray[_indexArray[i + 2]];
                        rd.sendVertex(v0);
                        rd.sendVertex(v1);
                        rd.sendVertex(v1);
                        rd.sendVertex(v2);
                        rd.sendVertex(v2);
                        rd.sendVertex(v0);
                    }
                rd.endPrimitive();
            rd.popState();
        }

        public override Shape.Type type() {
            return Shape.Type.MESH;
        }

        public override Array<Vector3> vertexArray() {  
            return _vertexArray;
        }

        /** Tri-list index array into vertexArray(). */
        public override Array<int> indexArray() {
            return _indexArray;
        }

        /** Not computed until the first call to bspTree, area, or getRandomSurfacePoint. */
        public AABSPTree<Triangle> bspTree() {
            if (! _hasTree) {
                buildBSP();
            }
            return _bspTree;
        }

        /** Slow the first time it is called because the BSP tree must be computed. */
        public override float area() {
            if (! _hasTree) {
                buildBSP();
            }
            return (float)_area;
        }

        /** No volume; Mesh is treated as a 2D surface */
        public override float volume() {
            return 0;
        }

        public override Vector3 center() {
            if (! _hasTree) {
                buildBSP();
            }

            return _boundingAABox.center();
        }

        public override Sphere boundingSphere() {
            if (! _hasTree) {
                buildBSP();
            }
            return _boundingSphere;
        }

        public override AABox boundingAABox() {
            if (! _hasTree) {
                buildBSP();
            }
            return _boundingAABox;
        }

        public override void getRandomSurfacePoint(
            out Vector3 P, 
            out Vector3 N) {
            
            if (! _hasTree) {
                buildBSP();
            }

            if (_bspTree.size() == 0) {
                throw new InvalidOperationException("MeshShape contains no triangles.");
            }

            // Choose uniformly at random based on surface area
            float sum = (float)g3dmath.random(0, _area);

            AABSPTree<Triangle>.Iterator it  = _bspTree.begin();

            while (it.MoveNext()) {
                Triangle tri = it.Current;
                sum -= (float)tri.area();
                if (sum <= 0) {
                    // We want a point from this triangle.
                    N = tri.normal();
                    P = tri.randomPoint();
                    return;
                }
            }
            
            // Should never get here, but we might due to roundoff.  Choose a point 
            // on the first triangle.
            AABSPTree<Triangle>.Iterator first = _bspTree.begin();
            first.MoveNext();
            Triangle firstTri = first.Current;
            N = firstTri.normal();
            P = firstTri.randomPoint();
        }

        public override Vector3 randomInteriorPoint() {
            Vector3 P;
            Vector3 N;
            getRandomSurfacePoint(out P, out N);
            return P;
        }
    }; // Mesh


    public class BoxShape : Shape
    {
        private Box          geometry;

        public BoxShape(Box b) {
            geometry = b;
        }

        /** Creates a box of the given extents centered at the origin. */
        public BoxShape(Vector3 extent) {
            geometry = new Box(extent * -0.5f, extent);
        }

        /** Creates a box of the given extents centered at the origin. */
        public BoxShape(float x, float y, float z) {
            geometry = new Box(new Vector3(-x / 2, -y / 2, -z / 2), new Vector3(x / 2, y / 2, z / 2));
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();
            rd.setObjectToWorldMatrix(cframe0 * cframe);
            Draw.box(geometry, rd, solidColor, wireColor);
            rd.setObjectToWorldMatrix(cframe0);
        }

        public override Shape.Type type() {
            return Shape.Type.BOX;
        }

        public override Box box() { 
            return geometry;
        }

        public override float area() {
            return geometry.area();
        }

        public override float volume() {
            return geometry.volume();
        }

        public override Vector3 center() {
            return geometry.center();
        }

        public override Sphere boundingSphere() {
            Sphere s = new Sphere(geometry.center(), geometry.extent().magnitude() / 2);
            return s;
        }

        public override AABox boundingAABox() {
            AABox aab;
            geometry.getBounds(out aab);
            return aab;
        }

        public override void getRandomSurfacePoint(
            out Vector3 P, 
            out Vector3 N) {
            geometry.getRandomSurfacePoint(out P, out N);
        }

        public override Vector3 randomInteriorPoint() {
            return geometry.randomInteriorPoint();
        }
    };


    public class RayShape : Shape
    {
        private Ray          geometry;

        public RayShape(Ray r) {
            geometry = r;
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();
            rd.setObjectToWorldMatrix(cframe0 * cframe);
            Draw.ray(geometry, rd, solidColor);
            rd.setObjectToWorldMatrix(cframe0);
        }

        public override Shape.Type type() {
            return Shape.Type.RAY;
        }

        public override Ray ray() { 
            return geometry;
        }

        public override float area() {
            return 0.0f;
        }

        public override float volume() {
            return 0.0f;
        }

        /** Set to origin of ray. */
        public override Vector3 center() {
            return geometry.origin;
        }

        /** Bounds the graphic representation of the ray */
        public Sphere BoundingSphere() {
            return new Sphere(geometry.origin + geometry.direction / 2, geometry.direction.length() / 2);
        }

        /** Bounds the graphic representation of the ray */
        public override Sphere boundingSphere() {
            return BoundingSphere();
        }

        /** Bounds the graphic representation of the ray */
        public AABox BoundingAABox() {
            Debug.debugAssertM(false, "No bounding axis aligned box for ray.");
            return new AABox();
        }

        /** Bounds the graphic representation of the ray */
        public override AABox boundingAABox() {
            return BoundingAABox();
        }

        /** Returns a point along the ray.  The normal is NaN */
        public override void getRandomSurfacePoint(out Vector3 P, out Vector3 N) {
            P = randomInteriorPoint();
            N = Vector3.nan();
        }

        /** Returns a random point along the ray */
        public override Vector3 randomInteriorPoint() {
            return geometry.origin + geometry.direction * (float)g3dmath.unitRandom();
        }

    };


    public class AxesShape : Shape
    {
        private CoordinateFrame  geometry;

        public AxesShape(CoordinateFrame a) {
            geometry = a;
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();
            rd.setObjectToWorldMatrix(cframe0 * cframe);
            Draw.axes(geometry, rd);
            rd.setObjectToWorldMatrix(cframe0);
        }

        public override Shape.Type type() {
            return Shape.Type.AXES;
        }

        public override CoordinateFrame axes() { 
            return geometry;
        }

        public override float area() {
            return 0.0f;
        }

        public override float volume() {
            return 0.0f;
        }

        public override Vector3 center() {
            return geometry.translation;
        }

        /** Returns a bound on the graphic representation of the axes */
        public Sphere BoundingSphere() {
            return new Sphere(geometry.translation, 1);
        }

        /** Returns a bound on the graphic representation of the axes */
        public override Sphere boundingSphere() {
            return BoundingSphere();
        }

        /** Returns a bound on the graphic representation of the axes */
        public AABox BoundingAABox() {
            Debug.debugAssertM(false, "No bounding axis aligned box for axes.");
            return new AABox();
        }

        /** Returns a bound on the graphic representation of the axes */
        public override AABox boundingAABox() {
            return BoundingAABox();
        }

        /** Returns the origin, normal = NaN*/
        public override void getRandomSurfacePoint(out Vector3 P, out Vector3 N) {
            P = geometry.translation;
            N = Vector3.nan();
        }

        public override Vector3 randomInteriorPoint() {
            return geometry.translation;
        }
    };


    public class CylinderShape : Shape
    {
        private Cylinder       geometry;

        public CylinderShape(Cylinder c) {
            geometry = c;
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();
            rd.setObjectToWorldMatrix(cframe0 * cframe);
            Draw.cylinder(geometry, rd, solidColor, wireColor);
            rd.setObjectToWorldMatrix(cframe0);
        }

        public override Shape.Type type() {
            return Shape.Type.CYLINDER;
        }

        public override Cylinder cylinder() { 
            return geometry;
        }

        public override float area() {
            return geometry.area();
        }

        public override float volume() {
            return geometry.volume();
        }

        public override Vector3 center() {
            return geometry.center();
        }

        public override Sphere boundingSphere() {
            Vector3 v = new Vector3(0, geometry.height() / 2, 0);
            v += new Vector3(geometry.radius(), 0, 0);
            Sphere s = new Sphere(geometry.center(), Math.Abs(v.magnitude()));
            return s;
        }

        public override AABox boundingAABox() {
            AABox aab;
            geometry.getBounds(out aab);
            return aab;
        }

        public override void getRandomSurfacePoint(
            out Vector3 P, 
            out Vector3 N) {
            geometry.getRandomSurfacePoint(out P, out N);
        }

        public override Vector3 randomInteriorPoint() {
            return geometry.randomInteriorPoint();
        }

    };


    public class SphereShape : Shape
    {
        private Sphere         geometry;

        public SphereShape(Sphere s) {
            geometry = s;
        }

        public SphereShape(Vector3 center, float radius) {
            geometry = new Sphere(center, radius);
        }

        /** Creates a sphere centered at the origin with the given radius. */
        public SphereShape(float radius) {
            geometry = new Sphere(Vector3.zero(), radius);
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();
            rd.setObjectToWorldMatrix(cframe0 * cframe);
            Draw.sphere(geometry, rd, solidColor, wireColor);
            rd.setObjectToWorldMatrix(cframe0);
        }

        public override Shape.Type type() {
            return Shape.Type.SPHERE;
        }

        public override Sphere sphere() { 
            return geometry;
        }

        public override void getRandomSurfacePoint(out Vector3 P, out Vector3 N) {
            P = geometry.randomSurfacePoint();
            N = (P - geometry.center).direction();
        }

        public override Vector3 randomInteriorPoint() {
            return geometry.randomInteriorPoint();
        }

        public override float area() {
            return geometry.area();
        }

        public override float volume() {
            return geometry.volume();
        }

        public override Vector3 center() {
            return geometry.center;
        }

        public override Sphere boundingSphere() {
            return geometry;
        }

        public override AABox boundingAABox() {
            AABox aab;
            geometry.getBounds(out aab);
            return aab;
        }
    };


    public class CapsuleShape : Shape
    {
        private Capsule         geometry;

        public CapsuleShape(Capsule c) {
            geometry = c;
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();
            rd.setObjectToWorldMatrix(cframe0 * cframe);
            Draw.capsule(geometry, rd, solidColor, wireColor);
            rd.setObjectToWorldMatrix(cframe0);
        }

        public override Shape.Type type() {
            return Shape.Type.CAPSULE;
        }

        public override Capsule capsule() { 
            return geometry;
        }

        public override float area() {
            return geometry.area();
        }

        public override float volume() {
            return geometry.volume();
        }

        public override Vector3 center() {
            return geometry.center();
        }

        public override Sphere boundingSphere() {
            Sphere s = new Sphere(geometry.center(), Math.Abs((geometry.center() - geometry.getPoint1()).magnitude()) + geometry.getRadius());
            return s;
        }

        public override AABox boundingAABox() {
            AABox aab;
            geometry.getBounds(out aab);
            return aab;
        }

        public override void getRandomSurfacePoint(out Vector3 P, out Vector3 N) {
            geometry.getRandomSurfacePoint(out P, out N);
        }

        public override Vector3 randomInteriorPoint() {
            return geometry.randomInteriorPoint();
        }

    };


    public class PlaneShape : Shape
    {
        private Plane         geometry;

        public PlaneShape(Plane p) {
            geometry = p;
        }

        public override void render(
            RenderDevice rd, 
            CoordinateFrame cframe, 
            Color4 solidColor, 
            Color4 wireColor) {
            CoordinateFrame cframe0 = rd.getObjectToWorldMatrix();

            rd.setObjectToWorldMatrix(cframe0 * cframe);

            Draw.plane(geometry, rd, solidColor, wireColor);
            
            rd.setObjectToWorldMatrix(cframe0);
        }

        public override Shape.Type type() {
            return Shape.Type.PLANE;
        }

        public override Plane plane() { 
            return geometry;
        }

        public override float area() {
            return float.PositiveInfinity;
        }

        public override float volume() {
            return 0.0f;
        }

        public override Vector3 center() {
            return geometry.center();
        }

        public override Sphere boundingSphere() {
            return new Sphere(geometry.center(), float.PositiveInfinity);
        }

        public override AABox boundingAABox() {
            return new AABox(-Vector3.inf(), Vector3.inf());
        }

        public override void getRandomSurfacePoint(out Vector3 P, out Vector3 N) {
            P = Vector3.nan();
            N = geometry.normal();
        }

        public override Vector3 randomInteriorPoint() {
            return Vector3.nan();
        }

    };
}
