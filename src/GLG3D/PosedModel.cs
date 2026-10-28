/**
  @file PosedModel.h
  
  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2003-11-15
  @edited  2006-04-22
*/ 

using System;
using System.Runtime.InteropServices;
using G3D;

namespace G3D
{
    /**
     Base class for posed models.  A posed model is a snapshot of geometry
     and other rendering information, frozen in time, of a potentially 
     animated or deformable "Model".  G3D has no API (but some conventions) for
     how you get a PosedModel from a model class.
     
     <B>G3D does not provide a scene graph structure</B> (e.g. there is no Model base class)
     because the visible
     surface determination, rendering, and caching behavior of a scene
     graph are different for each application.  There is currently no 
     one-size-fits-all scene graph design pattern.
     
     G3D <B>does</B> provide some basic infrastructure for making your
     own scene graph, however.  Model classes (e.g. G3D::IFSModel, G3D::MD2Model),
     which have no common base class, should all implement a <CODE>pose</CODE> 
     method.  The arguments to this method are all of the parameters (e.g. 
     animation frame, limb position) needed for reducing the general purpose,
     poseable model to a specific world space mesh for rendering.  This
     instance specific mesh is a G3D::PosedModel.  Because all PosedModels
     have the same interface, they can be used interchangably.

     Use G3D::PosedModelWrapper to encapsulate an existing posed model
     with your own.

     A common strategy when implementing PosedModel is to not compute "derived"
     values like the object space face normals until they are needed.  That is,
     the first call to a method might be very expensive because it goes off
     and computes some value that will be cached for future calls.
    */
    public abstract class PosedModel : ReferenceCountedObject
    {
        private VARArea defaultRenderVARArea;
        private VAR defaultRenderVertexVAR;
        private VAR defaultRenderNormalVAR;
        private VAR defaultRenderIndexVAR;

        protected PosedModel()
        {
        }

        public abstract string name();

        /** If true, this object depends on back-to-front rendering order and should be
            rendered in sorted order. Default is false.*/
        public virtual bool hasTransparency()
        {
            return false;
        }

        private sealed class ModelSorter
        {
            public double sortKey;
            public ReferenceCountedPointer<PosedModel> model;

            public ModelSorter(ReferenceCountedPointer<PosedModel> m, Vector3 axis)
            {
                model = m;
                Sphere s;
                m.pointer().getWorldSpaceBoundingSphere(out s);
                sortKey = axis.dot(s.center);
            }
        }

        /** 
          Divides the inModels into a front-to-back sorted array of opaque models and
          a back-to-front sorted array of potentially transparent models.  Any data
          originally in the output arrays is cleared.

          @param wsLookVector Sort axis; usually the -Z axis of the camera.
         */
        public static void sort(
            Array<ReferenceCountedPointer<PosedModel>> inModels,
            Vector3 wsLookVector,
            Array<ReferenceCountedPointer<PosedModel>> opaque,
            Array<ReferenceCountedPointer<PosedModel>> transparent)
        {
            Array<ModelSorter> op = new Array<ModelSorter>();
            Array<ModelSorter> tr = new Array<ModelSorter>();

            for (int m = 0; m < inModels.size(); ++m)
            {
                if (inModels[m].pointer().hasTransparency())
                {
                    tr.append(new ModelSorter(inModels[m], wsLookVector));
                }
                else
                {
                    op.append(new ModelSorter(inModels[m], wsLookVector));
                }
            }

            // Sort
            tr.sort(delegate(ModelSorter a, ModelSorter b) { return b.sortKey.CompareTo(a.sortKey); });
            op.sort(delegate(ModelSorter a, ModelSorter b) { return a.sortKey.CompareTo(b.sortKey); });

            transparent.resize(tr.size(), Array<ReferenceCountedPointer<PosedModel>>.DONT_SHRINK_UNDERLYING_ARRAY);
            for (int m = 0; m < tr.size(); ++m)
            {
                transparent[m] = tr[m].model;
            }

            opaque.resize(op.size(), Array<ReferenceCountedPointer<PosedModel>>.DONT_SHRINK_UNDERLYING_ARRAY);
            for (int m = 0; m < op.size(); ++m)
            {
                opaque[m] = op[m].model;
            }
        }

        /** Sorts the array in place along the look vector from front-to-back.*/
        public static void sort(
            Array<ReferenceCountedPointer<PosedModel>> inModels,
            Vector3 wsLookVector,
            Array<ReferenceCountedPointer<PosedModel>> opaque)
        {
            if (object.ReferenceEquals(inModels, opaque))
            {
                // The user is trying to sort in place.  Make a separate array for them.
                Array<ReferenceCountedPointer<PosedModel>> temp = new Array<ReferenceCountedPointer<PosedModel>>(inModels);
                sort(temp, wsLookVector, opaque);
                return;
            }

            Array<ModelSorter> op = new Array<ModelSorter>();

            for (int m = 0; m < inModels.size(); ++m)
            {
                op.append(new ModelSorter(inModels[m], wsLookVector));
            }

            // Sort
            op.sort(delegate(ModelSorter a, ModelSorter b) { return a.sortKey.CompareTo(b.sortKey); });

            opaque.resize(op.size(), Array<ReferenceCountedPointer<PosedModel>>.DONT_SHRINK_UNDERLYING_ARRAY);
            for (int m = 0; m < op.size(); ++m)
            {
                opaque[m] = op[m].model;
            }
        }

        /** Object to world space coordinate frame.*/
        public abstract void getCoordinateFrame(out CoordinateFrame cframe);

        public virtual CoordinateFrame coordinateFrame()
        {
            CoordinateFrame c;
            getCoordinateFrame(out c);
            return c;
        }

        /** Get the <B>object space</B> geometry (faster than getWorldSpaceGeometry). 
            Object and world space geometry only differ by a CoordinateFrame transformation.*/
        public abstract MeshAlg.Geometry objectSpaceGeometry();

        /** Get the <B>world space</B> geometry. */
        public virtual void getWorldSpaceGeometry(out MeshAlg.Geometry geometry)
        {
            CoordinateFrame c;
            getCoordinateFrame(out c);

            MeshAlg.Geometry osgeometry = objectSpaceGeometry();

            Array<Vector3> worldVertexArray;
            Array<Vector3> worldNormalArray;
            c.pointToWorldSpace(osgeometry.vertexArray, out worldVertexArray);
            c.normalToWorldSpace(osgeometry.normalArray, out worldNormalArray);

            geometry = new MeshAlg.Geometry();
            geometry.vertexArray = worldVertexArray;
            geometry.normalArray = worldNormalArray;
        }

        /** @deprecated Use objectSpaceFaceNormals() */
        public virtual void getObjectSpaceFaceNormals(out Array<Vector3> faceNormals)
        {
            getObjectSpaceFaceNormals(out faceNormals, true);
        }

        /** @deprecated Use objectSpaceFaceNormals() */
        public virtual void getObjectSpaceFaceNormals(out Array<Vector3> faceNormals, bool normalize)
        {
            MeshAlg.Geometry geometry = objectSpaceGeometry();
            Array<MeshAlg.Face> faceArray = faces();

            faceNormals = new Array<Vector3>();
            MeshAlg.computeFaceNormals(geometry.vertexArray, faceArray, faceNormals, normalize);
        }

        public virtual void getWorldSpaceFaceNormals(out Array<Vector3> faceNormals)
        {
            getWorldSpaceFaceNormals(out faceNormals, true);
        }

        public virtual void getWorldSpaceFaceNormals(out Array<Vector3> faceNormals, bool normalize)
        {
            MeshAlg.Geometry geometry;
            getWorldSpaceGeometry(out geometry);

            Array<MeshAlg.Face> faceArray = faces();

            faceNormals = new Array<Vector3>();
            MeshAlg.computeFaceNormals(geometry.vertexArray, faceArray, faceNormals, normalize);
        }

        /** Return a pointer to an array of object space face normals. */
        public abstract Array<Vector3> objectSpaceFaceNormals(bool normalize);

        public virtual Array<Vector3> objectSpaceFaceNormals()
        {
            return objectSpaceFaceNormals(true);
        }

        // Returns a reference rather than filling out an array because most
        // PosedModels have this information available.
        /**
          Adjacency information respecting the underlying connectivity
          of the mesh-- colocated vertices are treated as distinct.
         */
        public abstract Array<MeshAlg.Face> faces();

        public abstract Array<MeshAlg.Edge> edges();

        public abstract Array<MeshAlg.Vertex> vertices();

        /** If this model has texture coordinates, returns per-vertex texture coordinates
            in an array indexed the same as vertices() and geometry.vertexArray. */
        public virtual Array<Vector2> texCoords()
        {
            throw new InvalidOperationException("Model has no texture coordinates.");
        }

        /** Returns true if this model has texture coordinates. */
        public virtual bool hasTexCoords()
        {
            return false;
        }


        /** Contain adjacency information that merges colocated vertices
            (see MeshAlg::weldAdjacency) */
        public abstract Array<MeshAlg.Face> weldedFaces();

        public abstract Array<MeshAlg.Edge> weldedEdges();

        public abstract Array<MeshAlg.Vertex> weldedVertices();

        public abstract Array<int> triangleIndices();

        public abstract void getObjectSpaceBoundingSphere(out Sphere sphere);

        public virtual Sphere objectSpaceBoundingSphere()
        {
            Sphere s;
            getObjectSpaceBoundingSphere(out s);
            return s;
        }

        public virtual void getWorldSpaceBoundingSphere(out Sphere sphere)
        {
            CoordinateFrame C;
            getCoordinateFrame(out C);

            Sphere objectSpace;
            getObjectSpaceBoundingSphere(out objectSpace);
            sphere = C.toWorldSpace(objectSpace);
        }

        public virtual Sphere worldSpaceBoundingSphere()
        {
            Sphere s;
            getWorldSpaceBoundingSphere(out s);
            return s;
        }

        public abstract void getObjectSpaceBoundingBox(out Box box);

        public virtual Box objectSpaceBoundingBox()
        {
            Box b;
            getObjectSpaceBoundingBox(out b);
            return b;
        }

        public virtual void getWorldSpaceBoundingBox(out Box box)
        {
            CoordinateFrame C;
            getCoordinateFrame(out C);

            Box objectSpace;
            getObjectSpaceBoundingBox(out objectSpace);
            box = C.toWorldSpace(objectSpace);
        }

        public virtual Box worldSpaceBoundingBox()
        {
            Box b;
            getWorldSpaceBoundingBox(out b);
            return b;
        }

        /** Render using current fixed function lighting environment. Do not 
            change the steBehavior 
            with regard to stencil, shadowing, etc. is intentionally undefinded. 

            Default implementation calls defaultRender.
            */
        public virtual void render(RenderDevice renderDevice)
        {
            defaultRender(renderDevice);
        }

        /**
         Number of edges that have only one adjacent face in edges().
         These boundary edges are all at the end of the edge list.
         */
        public abstract int numBoundaryEdges();

        /**
         Number of edges that have only one adjacent face in weldedEdges().
         These boundary edges are all at the end of the edge list.
         */
        public abstract int numWeldedBoundaryEdges();

        /** @deprecated */
        public int numBrokenEdges()
        {
            return numBoundaryEdges();
        }

        /** 
         Render all terms that are independent of shadowing 
         (e.g., transparency, reflection, ambient illumination, 
         emissive illumination, nonShadowCastingLights). Transparent objects are assumed to render additively 
         (but should set the blend mode themselves). Restore all state to the original form
         on exit.  Default implementation invokes render.

         Implementation must obey the current stencil, depth write, color write, and depth test modes.
         Implementation may freely set the blending, and alpha test modes.

         The caller should invoke this in depth sorted back to front order for transparent objects.

         The default implementation configures the non-shadow casting lights and calls render.

         Implementation advice:
          <UL>
            <LI> If color write is disabled, don't bother performing any shading on this object.
            <LI> It may be convenient to support multiple lights by invoking renderShadowedLightPass multiple times.
          </UL>

        */
        public virtual void renderNonShadowed(
            RenderDevice rd,
            ReferenceCountedPointer<Lighting> lighting)
        {
            rd.pushState();
                if (rd.colorWrite())
                {
                    Lighting light = lighting.pointer();
                    rd.setAmbientLightColor(light.ambientTop);
                    Color3 C = light.ambientBottom - light.ambientTop;

                    int shift = 0;
                    if ((C.r != 0) || (C.g != 0) || (C.b != 0))
                    {
                        rd.setLight(0, GLight.directional(-Vector3.unitY(), C, false));
                        ++shift;
                    }

                    for (int L = 0; L < Math.Min(7, light.lightArray.size()); ++L)
                    {
                        rd.setLight(L + shift, light.lightArray[L]);
                    }
                    rd.enableLighting();
                }
                render(rd);
            rd.popState();
        }

        /** Render illumination from this light source additively. Implementation must set the
            alpha blending mode to additive.  Must obey the current stencil, depth write, and depth test modes.
            Default implementation enables a single light and calls render.
        */
        public virtual void renderShadowedLightPass(
            RenderDevice rd,
            GLight light)
        {
            rd.pushState();
                rd.enableLighting();
                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ONE);
                rd.setLight(0, light);
                rd.setAmbientLightColor(Color3.black());
                render(rd);
            rd.popState();
        }

        /** Render illumination from this source additively, held out by the shadow map (which the caller 
            must have computed, probably using renderNonShadowed).  Default implementation
            configures the shadow map in texture unit 1 and calls render. */
        public virtual void renderShadowMappedLightPass(
            RenderDevice rd,
            GLight light,
            Matrix4 lightMVP,
            ReferenceCountedPointer<Texture> shadowMap)
        {
            if (shadowMap == null || shadowMap.isNull())
            {
                throw new ArgumentNullException("shadowMap");
            }

            rd.pushState();
                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ONE);
                rd.configureShadowMap(1, lightMVP, shadowMap.pointer());
                rd.setLight(0, light);
                rd.enableLighting();
                rd.setAmbientLightColor(Color3.black());
                render(rd);
            rd.popState();
        }

        /**
           Implementation must obey the current stencil, depth write, color write, and depth test modes.
           Implementation may freely set the blending, and alpha test modes.

           Default implementation renders the triangles returned by getIndices
           and getGeometry. 
         */
        protected virtual void defaultRender(RenderDevice rd)
        {
            MeshAlg.Geometry geometry = objectSpaceGeometry();
            Array<int> indices = triangleIndices();

            updateDefaultRenderArrays(geometry, indices);

            rd.pushState();
                rd.setObjectToWorldMatrix(coordinateFrame());
                rd.beginIndexedPrimitives();
                    rd.setNormalArray(defaultRenderNormalVAR);
                    rd.setVertexArray(defaultRenderVertexVAR);
                    rd.setIndexArray(defaultRenderIndexVAR);
                    rd.sendIndices(RenderDevice.Primitive.TRIANGLES, indices.size());
                rd.endIndexedPrimitives();
            rd.popState();
        }

        /**
           Creates (or refreshes) the vertex arrays used by defaultRender.  The
           arrays are kept so that repeated draws of the same model do not
           allocate a new vertex buffer for every frame; the contents are
           uploaded again on every call so that animated models stay current.
         */
        private void updateDefaultRenderArrays(MeshAlg.Geometry geometry, Array<int> indices)
        {
            Vector3[] vertices = geometry.vertexArray.ToArray();
            Vector3[] normals = geometry.normalArray.ToArray();
            int[] indexValues = indices.ToArray();

            if (vertices.Length == 0 || indexValues.Length == 0)
            {
                defaultRenderVARArea = null;
                defaultRenderVertexVAR = null;
                defaultRenderNormalVAR = null;
                defaultRenderIndexVAR = null;
                return;
            }

            int vertexBytes = Marshal.SizeOf(typeof(Vector3)) * vertices.Length;
            int normalBytes = Marshal.SizeOf(typeof(Vector3)) * normals.Length;
            int indexBytes = Marshal.SizeOf(typeof(int)) * indexValues.Length;

            bool recreate = (defaultRenderVARArea == null) ||
                            (defaultRenderVertexVAR == null) ||
                            (defaultRenderIndexVAR == null) ||
                            !defaultRenderVertexVAR.valid() ||
                            !defaultRenderIndexVAR.valid() ||
                            (defaultRenderVertexVAR.maxSize() < vertexBytes) ||
                            (defaultRenderIndexVAR.maxSize() < indexBytes);

            if ((defaultRenderNormalVAR != null) &&
                !defaultRenderNormalVAR.valid())
            {
                recreate = true;
            }

            if ((defaultRenderNormalVAR != null) &&
                (defaultRenderNormalVAR.maxSize() < normalBytes))
            {
                recreate = true;
            }

            if (recreate)
            {
                defaultRenderVARArea = VARArea.create(
                    (long)(vertexBytes + normalBytes + indexBytes + 16));

                defaultRenderVertexVAR = createVertexVAR(vertices, defaultRenderVARArea);
                defaultRenderIndexVAR = createIndexVAR(indexValues, defaultRenderVARArea);

                if (normals.Length > 0)
                {
                    defaultRenderNormalVAR = createNormalVAR(normals, defaultRenderVARArea);
                }
                else
                {
                    defaultRenderNormalVAR = null;
                }
            }
            else
            {
                defaultRenderVertexVAR.setData(vertices);
                defaultRenderIndexVAR.setData(indexValues);

                if ((defaultRenderNormalVAR != null) && (normals.Length > 0))
                {
                    defaultRenderNormalVAR.setData(normals);
                }
                else
                {
                    defaultRenderNormalVAR = null;
                }
            }
        }

        /**
           Creates a vertex array object inside the given area.
         */
        protected static VAR createVertexVAR(Vector3[] values, VARArea area)
        {
            return new VAR(values, area);
        }

        protected static VAR createNormalVAR(Vector3[] values, VARArea area)
        {
            VAR var = new VAR(values, area);
            var.setType(VAR.Type.NORMAL);
            return var;
        }

        protected static VAR createTexCoordVAR(Vector2[] values, VARArea area)
        {
            Vector3[] expanded = new Vector3[values == null ? 0 : values.Length];
            for (int i = 0; i < expanded.Length; ++i)
            {
                expanded[i] = new Vector3(values[i].x, values[i].y, 0.0f);
            }

            VAR var = new VAR(expanded, area);
            var.setType(VAR.Type.TEXCOORD0);
            return var;
        }

        protected static VAR createIndexVAR(int[] values, VARArea area)
        {
            return new VAR(values, area);
        }
    }



    /**
     A wrapper that trampolines every method to an internal model.
     Use this as a base class when you have a PosedModelRef and 
     want to override its render() [or some other] method.
     @deprecated (Will be moved to contrib)
     */
    public class PosedModelWrapper : PosedModel
    {
        protected ReferenceCountedPointer<PosedModel> model;

        protected PosedModelWrapper(ReferenceCountedPointer<PosedModel> _model)
        {
            model = _model;
        }

        public override string name()
        {
            return model.pointer().name();
        }

        public override void getCoordinateFrame(out CoordinateFrame cframe)
        {
            model.pointer().getCoordinateFrame(out cframe);
        }

        public override CoordinateFrame coordinateFrame()
        {
            return model.pointer().coordinateFrame();
        }

        public override MeshAlg.Geometry objectSpaceGeometry()
        {
            return model.pointer().objectSpaceGeometry();
        }

        public override void getWorldSpaceGeometry(out MeshAlg.Geometry geometry)
        {
            model.pointer().getWorldSpaceGeometry(out geometry);
        }

        public override Array<MeshAlg.Face> faces()
        {
            return model.pointer().faces();
        }

        public override Array<MeshAlg.Edge> edges()
        {
            return model.pointer().edges();
        }

        public override bool hasTexCoords()
        {
            return model.pointer().hasTexCoords();
        }

        public override Array<Vector2> texCoords()
        {
            return model.pointer().texCoords();
        }

        public override Array<MeshAlg.Vertex> vertices()
        {
            return model.pointer().vertices();
        }

        public override Array<MeshAlg.Face> weldedFaces()
        {
            return model.pointer().weldedFaces();
        }

        public override Array<MeshAlg.Edge> weldedEdges()
        {
            return model.pointer().weldedEdges();
        }

        public override Array<MeshAlg.Vertex> weldedVertices()
        {
            return model.pointer().weldedVertices();
        }

        public override void getObjectSpaceFaceNormals(out Array<Vector3> faceNormals)
        {
            model.pointer().getObjectSpaceFaceNormals(out faceNormals);
        }

        public override void getObjectSpaceFaceNormals(out Array<Vector3> faceNormals, bool normalize)
        {
            model.pointer().getObjectSpaceFaceNormals(out faceNormals, normalize);
        }

        public override void getWorldSpaceFaceNormals(out Array<Vector3> faceNormals)
        {
            model.pointer().getWorldSpaceFaceNormals(out faceNormals);
        }

        public override void getWorldSpaceFaceNormals(out Array<Vector3> faceNormals, bool normalize)
        {
            model.pointer().getWorldSpaceFaceNormals(out faceNormals, normalize);
        }

        public override Array<Vector3> objectSpaceFaceNormals(bool normalize)
        {
            return model.pointer().objectSpaceFaceNormals(normalize);
        }

        public override Array<int> triangleIndices()
        {
            return model.pointer().triangleIndices();
        }

        public override void getObjectSpaceBoundingSphere(out Sphere sphere)
        {
            model.pointer().getObjectSpaceBoundingSphere(out sphere);
        }

        public override Sphere objectSpaceBoundingSphere()
        {
            return model.pointer().objectSpaceBoundingSphere();
        }

        public override void getWorldSpaceBoundingSphere(out Sphere sphere)
        {
            model.pointer().getWorldSpaceBoundingSphere(out sphere);
        }

        public override Sphere worldSpaceBoundingSphere()
        {
            return model.pointer().worldSpaceBoundingSphere();
        }

        public override void getObjectSpaceBoundingBox(out Box box)
        {
            model.pointer().getObjectSpaceBoundingBox(out box);
        }

        public override Box objectSpaceBoundingBox()
        {
            return model.pointer().objectSpaceBoundingBox();
        }

        public override void getWorldSpaceBoundingBox(out Box box)
        {
            model.pointer().getWorldSpaceBoundingBox(out box);
        }

        public override Box worldSpaceBoundingBox()
        {
            return model.pointer().worldSpaceBoundingBox();
        }

        public override void render(RenderDevice renderDevice)
        {
            model.pointer().render(renderDevice);
        }

        public override int numBoundaryEdges()
        {
            return model.pointer().numBoundaryEdges();
        }

        public override int numWeldedBoundaryEdges()
        {
            return model.pointer().numWeldedBoundaryEdges();
        }
    }

    /////////////////////////////////////////////////////////////////

    /** Primarily for use in GUI rendering. */
    public abstract class PosedModel2D : ReferenceCountedObject
    {
        /** Assumes that the RenderDevice is configured in in RenderDevice::push2D mode. */
        public abstract void render(RenderDevice rd);

        /** Conservative 2D rendering bounds.
         */
        public abstract Rect2D bounds();

        /**
         2D objects are drawn from back to front, creating the perception of overlap.
         0 = closest to the front, 1 = closest to the back. 
         */
        public abstract float depth();

        /** Sorts from farthest to nearest. */
        public static void sort(Array<ReferenceCountedPointer<PosedModel2D>> array)
        {
            array.sort(delegate(ReferenceCountedPointer<PosedModel2D> a, ReferenceCountedPointer<PosedModel2D> b)
            {
                return b.pointer().depth().CompareTo(a.pointer().depth());
            });
        }
    }

    /**
       Simple material used by IFSModel and MD2Model pose methods.
       This class is provided as a convenience; it is not necessary
       to use it.  If you do not provide a material, whatever
       state is currently on the G3D::RenderDevice is used.  You
       probably don't want to use GMaterial at all if you
       are writing vertex and pixel programs.
    */
    public static class GMaterialExtensions
    {
        /** Applies this material to the render device */
        public static void configure(this GMaterial material, RenderDevice rd)
        {
            if (material == null)
            {
                throw new ArgumentNullException("material");
            }

            rd.setColor(material.diffuseColor);

            if (material.diffuseTexture != null && material.diffuseTexture.notNull())
            {
                rd.setTexture(0, material.diffuseTexture.pointer());
            }

            if (material.normalMap != null && material.normalMap.notNull())
            {
                rd.setTexture(1, material.normalMap.pointer());
            }

            if (material.specularMap != null && material.specularMap.notNull())
            {
                rd.setTexture(2, material.specularMap.pointer());
            }

            rd.setShininess(material.shininess);
            rd.setSpecularCoefficient(material.specularColor);
        }
    }
}
