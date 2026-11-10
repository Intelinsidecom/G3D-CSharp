/**
  @file GLSL_Demo/Entity.cs

  Port of the Entity class in GLSL_Demo/header.h and GLSL_Demo/Viewer.cpp.
 */

using System;
using G3D;

namespace GLSL_Demo
{
    public class Entity : ReferenceCountedObject
    {
        private Entity(Mesh m, CoordinateFrame c)
        {
            mesh = m;
            cframe = c;
        }

        public Mesh mesh;
        public CoordinateFrame cframe;

        public static ReferenceCountedPointer<Entity> create(Mesh m, CoordinateFrame c)
        {
            return new ReferenceCountedPointer<Entity>(new Entity(m, c));
        }

        public void render(RenderDevice rd)
        {
            rd.pushState();
            rd.setObjectToWorldMatrix(cframe);
            mesh.render(rd);
            rd.popState();
        }
    }
}
