/**
  @file shadowVolume.h
  
  @maintainer Morgan McGuire, morgan@graphics3d.com
  
  @created 2001-12-16
  @edited  2006-02-31
 */

using System;

namespace G3D
{
    public static class ShadowVolume
    {
        /**
         Used by begin/end/markShadows
         */
        private static bool inMarkShadows = false;

        /** See G3D::markShadows */
        public static void beginMarkShadows(RenderDevice renderDevice)
        {
            Debug.debugAssert(! inMarkShadows);
            inMarkShadows = true;

            renderDevice.pushState();
                // Render only to the stencil buffer.
                renderDevice.disableDepthWrite();
                renderDevice.disableColorWrite();
                renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_LESS);

                // z-fail; decrement for front faces and increment
                // for back faces.
                if (renderDevice.supportsTwoSidedStencil()) {
                    renderDevice.setCullFace(RenderDevice.CullFace.CULL_NONE);
                    renderDevice.setStencilOp(
                        RenderDevice.StencilOp.STENCIL_KEEP,
                        RenderDevice.StencilOp.STENCIL_DECR_WRAP,
                        RenderDevice.StencilOp.STENCIL_KEEP,

                        RenderDevice.StencilOp.STENCIL_KEEP,
                        RenderDevice.StencilOp.STENCIL_INCR_WRAP,
                        RenderDevice.StencilOp.STENCIL_KEEP);
                } else {

                    // Front face pass
                    renderDevice.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    renderDevice.setStencilOp(
                        RenderDevice.StencilOp.STENCIL_KEEP,
                        RenderDevice.StencilOp.STENCIL_DECR_WRAP,
                        RenderDevice.StencilOp.STENCIL_KEEP);
                }
        }

        /** See G3D::markShadows */
        public static void endMarkShadows(RenderDevice renderDevice)
        {
            Debug.debugAssert(inMarkShadows);
            inMarkShadows = false;
            renderDevice.popState();
        }

        /**
         Copies the source array over the dest array,
         setting w = 1 for all points.
         Helper for markShadows.
         */
        private static void vertexCopy(
            Array<Vector3> source, 
            Array<Vector4> dest) {

            Debug.alwaysAssertM(source.size() <= dest.size(), 
                "Destination array must be at least as large as source");

            for (int v = source.size() - 1; v >= 0; --v) {
                dest[v] = new Vector4(source[v].x, source[v].y, source[v].z, 1);
            }
        }

        /**
         Helper for markShadows.  Extrudes source to infinity
         away from L.
         */
        private static void vertexExtrude(
            Array<Vector3> source, 
            Array<Vector4> dest,
            int shift,
            Vector4 L) {

            Debug.debugAssert(L.w == 1.0);

            for (int i = source.size() - 1; i >= 0; --i) {
                dest[shift + i] = new Vector4(
                    source[i].x - L.x,
                    source[i].y - L.y,
                    source[i].z - L.z,
                    0.0f);
            }
        }

        /**
         Given a posed model, renders a shadow volume using the algorithm
         described in:
         
           C. Everitt, M. Kilgard, "Practical and Robust Shadow Volumes", 
           NVIDIA Technical Report, 2002
           http://developer.nvidia.com/object/robust_shadow_volumes.html

         This algorithm <B>requires</B> models with no broken edges.  Shadows
         will not be rendered for models with broken edges.

         Note that faster methods of rendering shadow volumes have since been
         proposed, such as: 

           M. McGuire, J. F. Hughes, K. T. Egan, M. Kilgard, C. Everitt,
           "Fast, Practical, and Robust Shadow Volumes", NVIDIA Technical
           Report 2003
           http://developer.nvidia.com/object/fast_shadow_volumes.html

         Set up your rendering code as follows:

           <PRE>
             renderDevice->clear(true, true, true);
             renderDevice->pushState();
                 renderDevice->enableLighting()
                 // Configure ambient light
                 renderDevice->setAmbientLightColor(Color3::white() * .25);

                 for (int m = 0; m < model.size(); ++m) {
                     model[m]->render(renderDevice);
                 }

                 renderDevice->disableDepthWrite();    
                   for (int L = 0; L < light.size(); ++L) {
                     beginMarkShadows(renderDevice);
                         for (int m = 0; m < model.size(); ++m) {
                             markShadows(renderDevice, model[m], light[L].position);
                         }
                     endMarkShadows(renderDevice);

                     renderDevice->setBlendFunc(RenderDevice::BLEND_ONE, RenderDevice::BLEND_ONE);
                     renderDevice->setStencilTest(RenderDevice::STENCIL_EQUAL);
                     renderDevice->setStencilConstant(0);
                     renderDevice->setAmbientLightColor(Color3::black());
                     renderDevice->setDepthTest(RenderDevice::DEPTH_LEQUAL);

                     renderDevice->setLight(0, light[L]);
                     for (int m = 0; m < model.size(); ++m) {
                         model[m]->render(renderDevice);
                     }
                 }
             renderDevice->popState();
           </PRE>

           <B>Limitations</B>

             This function requires the far clipping plane at z = -inf (the
             default for GCamera).

             Assumes that render calls do
             not set the blend func or stencil funcs, or change the
             camera matrix or projection matrix.

             Transparent objects should not be used
             with shadow marking.
         */
        public static void markShadows(
            RenderDevice renderDevice, 
            ReferenceCountedPointer<PosedModel> model,
            Vector4 light) {

            if ((model == null) || model.isNull())
            {
                throw new ArgumentNullException("model");
            }

            markShadows(renderDevice, model.pointer(), light);
        }

        /**
         Given a posed model, renders a shadow volume using the algorithm
         described in:
         
           C. Everitt, M. Kilgard, "Practical and Robust Shadow Volumes", 
           NVIDIA Technical Report, 2002
           http://developer.nvidia.com/object/robust_shadow_volumes.html

         This algorithm <B>requires</B> models with no broken edges.  Shadows
         will not be rendered for models with broken edges.

         Note that faster methods of rendering shadow volumes have since been
         proposed, such as: 

           M. McGuire, J. F. Hughes, K. T. Egan, M. Kilgard, C. Everitt,
           "Fast, Practical, and Robust Shadow Volumes", NVIDIA Technical
           Report 2003
           http://developer.nvidia.com/object/fast_shadow_volumes.html

         Set up your rendering code as follows:

           <PRE>
             renderDevice->clear(true, true, true);
             renderDevice->pushState();
                 renderDevice->enableLighting()
                 // Configure ambient light
                 renderDevice->setAmbientLightColor(Color3::white() * .25);

                 for (int m = 0; m < model.size(); ++m) {
                     model[m]->render(renderDevice);
                 }

                 renderDevice->disableDepthWrite();    
                   for (int L = 0; L < light.size(); ++L) {
                     beginMarkShadows(renderDevice);
                         for (int m = 0; m < model.size(); ++m) {
                             markShadows(renderDevice, model[m], light[L].position);
                         }
                     endMarkShadows(renderDevice);

                     renderDevice->setBlendFunc(RenderDevice::BLEND_ONE, RenderDevice::BLEND_ONE);
                     renderDevice->setStencilTest(RenderDevice::STENCIL_EQUAL);
                     renderDevice->setStencilConstant(0);
                     renderDevice->setAmbientLightColor(Color3::black());
                     renderDevice->setDepthTest(RenderDevice::DEPTH_LEQUAL);

                     renderDevice->setLight(0, light[L]);
                     for (int m = 0; m < model.size(); ++m) {
                         model[m]->render(renderDevice);
                     }
                 }
             renderDevice->popState();
           </PRE>

           <B>Limitations</B>

             This function requires the far clipping plane at z = -inf (the
             default for GCamera).

             Assumes that render calls do
             not set the blend func or stencil funcs, or change the
             camera matrix or projection matrix.

             Transparent objects should not be used
             with shadow marking.
         */
        public static void markShadows(
            RenderDevice renderDevice, 
            PosedModel model,
            Vector4 light) {

            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            Debug.debugAssertM(inMarkShadows, "Must call beginMarkShadows before markShadows");

            if (model.numWeldedBoundaryEdges() > 0) {
                // Can't cast shadows for such an object
                return;
            }

            //////////////////////////////////////////////////////////////////////////////
            // Create the geometry

            // Move to object space
            CoordinateFrame cframe;
            model.getCoordinateFrame(out cframe);

            Vector4 L = cframe.toObjectSpace(light);

            // Get the relevant object space mesh.
            // This is faster than getting world space
            // geometry for most PosedModel objects.

            MeshAlg.Geometry geometry = model.objectSpaceGeometry();
            Array<Vector3> vertexArray = geometry.vertexArray;
            Array<MeshAlg.Edge> edgeArray = model.weldedEdges();
            Array<MeshAlg.Face> faceArray = model.weldedFaces();

            Array<bool> backface = new Array<bool>();
            MeshAlg.identifyBackfaces(vertexArray, faceArray, L, backface);

            bool directional = (light.w == 0);

            int numPts;
            int n = vertexArray.size();
                
            // Directional lights need all of the vertices plus one at
            // infinity.  Point lights need a full copy of the object
            // at infinity.
            if (directional) {
                numPts = vertexArray.size() + 1;
            } else {
                numPts = vertexArray.size() * 2;
            }

            // Create an array of float4 for use on the graphics card.
            Array<Vector4> cpuVertex = new Array<Vector4>(numPts);
            vertexCopy(vertexArray, cpuVertex);

            if (directional) {
                cpuVertex[n] = -L;
            } else {
                // Extrude to infinity
                vertexExtrude(vertexArray, cpuVertex, n, L);
            }

            // Upload to graphics card
            VARArea varArea = VARArea.create(cpuVertex.size() * 16);
            VAR gpuVertex = new VAR(cpuVertex.ToArray(), varArea);

            // Triangle list indices
            index.resize(0, Array<int>.DONT_SHRINK_UNDERLYING_ARRAY);

            // Shadow volume sides
            for (int e = edgeArray.size() - 1; e >= 0; --e) {
                MeshAlg.Edge edge = edgeArray[e];
                if (backface[edge.faceIndex[0]] != backface[edge.faceIndex[1]]) {
                    // Silhouette edge

                    int i0 = edge.vertexIndex[0];
                    int i1 = edge.vertexIndex[1];

                    // Wind in the direction of the backface
                    if (directional) {
                        // Triangle
                        if (backface[edge.faceIndex[0]]) {
                            index.append(i0, i1, n);
                        } else {
                            index.append(n, i1, i0);
                        }
                    } else {
                        // Quad
                        if (backface[edge.faceIndex[0]]) {
                            index.append(i0, i1, i1 + n);
                            index.append(i0, i1 + n, i0 + n);
                        } else {
                            index.append(i1 + n, i1, i0);
                            index.append(i0 + n, i1 + n, i0);
                        }
                    }
                }
            }

            // Shadow volume caps
            for (int f = faceArray.size() - 1; f >= 0; --f) {
                MeshAlg.Face face = faceArray[f];
                if (! backface[f]) {
                    index.append(face.vertexIndex[0], face.vertexIndex[1], face.vertexIndex[2]);
                } else if (! directional) {
                    // Point light requires dark cap as well 
                    index.append(face.vertexIndex[0] + n, face.vertexIndex[1] + n, face.vertexIndex[2] + n);
                }
            }

            //////////////////////////////////////////////////////////////////////////////
            // Draw the geometry

            renderDevice.setObjectToWorldMatrix(cframe);
            renderDevice.beginIndexedPrimitives();
                renderDevice.setVertexArray(gpuVertex);

                //
                renderDevice.sendIndices(RenderDevice.Primitive.TRIANGLES, index);

                if (! renderDevice.supportsTwoSidedStencil()) {
                    // Render a second pass for the back faces
                    renderDevice.setCullFace(RenderDevice.CullFace.CULL_FRONT);
                    renderDevice.setStencilOp(
                        RenderDevice.StencilOp.STENCIL_KEEP,
                        RenderDevice.StencilOp.STENCIL_INCR_WRAP,
                        RenderDevice.StencilOp.STENCIL_KEEP);

                    renderDevice.sendIndices(RenderDevice.Primitive.TRIANGLES, index);

                    // Restore the stencil settings to what we need for front faces.
                    renderDevice.setCullFace(RenderDevice.CullFace.CULL_BACK);
                    renderDevice.setStencilOp(
                        RenderDevice.StencilOp.STENCIL_KEEP,
                        RenderDevice.StencilOp.STENCIL_DECR_WRAP,
                        RenderDevice.StencilOp.STENCIL_KEEP);
                }

            renderDevice.endIndexedPrimitives();
        }

        /**
         Triangle list indices
         */
        private static Array<int> index = new Array<int>();
    }
}
