/**
  @file edgeFeatures.h
  
  @maintainer Morgan McGuire, morgan@graphics3d.com
  
  @created 2004-04-20
  @edited  2006-02-03
 */

using System;
using System.Runtime.InteropServices;

namespace G3D
{
    public static class EdgeFeatures
    {
        /**
          Renders boundaries, contours, and sharp creases as lines. 
          The caller should set the line width, color, and enable
          alpha blending (for smooth lines).
      
          Primarily useful for cartoon rendering.
      
          If the lines are lost in the object's body due to z-fighting,
          render the model first with a negative value for RenderDevice::setPolygonOffset.
      
          @param creaseAngle Angle below which a sharp corner is considered a crease.  
               Zero makes everything a crease,  G3D_PI/2 makes nothing a crease.
      
          
         <B>BETA API</B> -- subject to change.
         */
        public static void drawFeatureEdges(
            RenderDevice renderDevice, 
            ReferenceCountedPointer<PosedModel> model, 
            float creaseAngle) {

            if ((model == null) || model.isNull())
            {
                throw new ArgumentNullException("model");
            }

            drawFeatureEdges(renderDevice, model.pointer(), creaseAngle);
        }

        /**
          Renders boundaries, contours, and sharp creases as lines. 
          The caller should set the line width, color, and enable
          alpha blending (for smooth lines).
      
          Primarily useful for cartoon rendering.
      
          If the lines are lost in the object's body due to z-fighting,
          render the model first with a negative value for RenderDevice::setPolygonOffset.
      
          @param creaseAngle Angle below which a sharp corner is considered a crease.  
               Zero makes everything a crease,  G3D_PI/2 makes nothing a crease.
      
          
         <B>BETA API</B> -- subject to change.
         */
        public static void drawFeatureEdges(
            RenderDevice renderDevice, 
            ReferenceCountedPointer<PosedModel> model) {

            drawFeatureEdges(renderDevice, model, (float)g3dmath.toRadians(40));
        }

        /**
          Renders boundaries, contours, and sharp creases as lines. 
          The caller should set the line width, color, and enable
          alpha blending (for smooth lines).
      
          Primarily useful for cartoon rendering.
      
          If the lines are lost in the object's body due to z-fighting,
          render the model first with a negative value for RenderDevice::setPolygonOffset.
      
          @param creaseAngle Angle below which a sharp corner is considered a crease.  
               Zero makes everything a crease,  G3D_PI/2 makes nothing a crease.
      
          
         <B>BETA API</B> -- subject to change.
         */
        public static void drawFeatureEdges(
            RenderDevice renderDevice, 
            PosedModel model, 
            float creaseAngle) {

            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            float dotThreshold = Math.Max((float)Math.Cos(creaseAngle), 0.0f);

            bool drawCreases = (creaseAngle <= (float)(g3dmath.G3D_PI / 2));

            Vector3 wsEye = renderDevice.getCameraToWorldMatrix().translation;

            Array<MeshAlg.Edge> edgeArray = model.weldedEdges();
            Array<Vector3> faceNormal = model.objectSpaceFaceNormals(drawCreases);
            Array<MeshAlg.Face> faceArray = model.weldedFaces();
            Array<Vector3> vertexArray = model.objectSpaceGeometry().vertexArray;

            // Work in the object space of the model so we don't
            // have to transform the geometry. 
            CoordinateFrame cframe = model.coordinateFrame();
            Vector3 eye = cframe.pointToObjectSpace(wsEye);

            // Compute backfaces
            Array<bool> backface = new Array<bool>();
            backface.resize(faceNormal.size(), Array<bool>.DONT_SHRINK_UNDERLYING_ARRAY);
            for (int f = faceNormal.size() - 1; f >= 0; --f) {
                // View vector
                Vector3 V = (eye - vertexArray[faceArray[f].vertexIndex[0]]);
                backface[f] = faceNormal[f].dot(V) < 0;
            }

            // Find contour edges
            Array<Vector3> cpuVertexArray = new Array<Vector3>();
            cpuVertexArray.resize(0, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);
            for (int e = edgeArray.size() - 1; e >= 0; --e) {
                MeshAlg.Edge edge = edgeArray[e];

                int f0 = edge.faceIndex[0];
                int f1 = edge.faceIndex[1];

                if (
                    // Boundaries:
                    (f0 == MeshAlg.Face.NONE) ||
                    (f1 == MeshAlg.Face.NONE) || 

                    // Contours:
                    (backface[f0] ^ backface[f1]) ||

                    // Front-face creases:
                    (drawCreases &&
                     (faceNormal[f0].dot(faceNormal[f1]) <= dotThreshold) && 
                     ! (backface[f0] && backface[f1]))) {

                    cpuVertexArray.append(
                        vertexArray[edge.vertexIndex[0]], 
                        vertexArray[edge.vertexIndex[1]]);
                }
            }

            VARArea varArea = VARArea.create(cpuVertexArray.size() * Marshal.SizeOf(typeof(Vector3)));
            VAR gpuVertexArray = new VAR(cpuVertexArray.ToArray(), varArea);

            renderDevice.pushState();
                renderDevice.setObjectToWorldMatrix(cframe);
                renderDevice.beginIndexedPrimitives();
                    renderDevice.setVertexArray(gpuVertexArray);
                    renderDevice.sendSequentialIndices(RenderDevice.Primitive.LINES, cpuVertexArray.size());
                renderDevice.endIndexedPrimitives();
            renderDevice.popState();
        }

        /**
          Renders boundaries, contours, and sharp creases as lines. 
          The caller should set the line width, color, and enable
          alpha blending (for smooth lines).
      
          Primarily useful for cartoon rendering.
      
          If the lines are lost in the object's body due to z-fighting,
          render the model first with a negative value for RenderDevice::setPolygonOffset.
      
          @param creaseAngle Angle below which a sharp corner is considered a crease.  
               Zero makes everything a crease,  G3D_PI/2 makes nothing a crease.
      
          
         <B>BETA API</B> -- subject to change.
         */
        public static void drawFeatureEdges(
            RenderDevice renderDevice, 
            PosedModel model) {

            drawFeatureEdges(renderDevice, model, (float)g3dmath.toRadians(40));
        }
    }
}
