/**
  @file ObjectShader.h
  Deprecated object shader abstraction.
*/

using System;
using G3D;

namespace G3D
{
    public sealed class ObjectShader : ReferenceCountedObject
    {
        private object backendHandle;

        public ObjectShader()
        {
        }

        public void run(RenderDevice rd)
        {
            throw new NotSupportedException("G3D::ObjectShader is deprecated and cannot be executed.");
        }

        public bool ok()
        {
            return true;
        }

        public string messages()
        {
            return "";
        }

        internal object getBackendHandle()
        {
            return backendHandle;
        }

        internal void setBackendHandle(object handle)
        {
            backendHandle = handle;
        }
    }
}