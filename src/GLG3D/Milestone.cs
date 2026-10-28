/**
  @file Milestone.h

  Used by RenderDevice to force the CPU to wait for the
  GPU to complete for series of commands.  Create using
  RenderDevice::createMilestone.  These are equivalent to
  NVIDIA fences.  On ATI cards the semantics are identical
  but performance is lower.
*/

using System;

namespace G3D
{
    /**
     Used by RenderDevice to force the CPU to wait for the
     GPU to complete for series of commands.  Create using
     RenderDevice::createMilestone.  These are equivalent to
     NVIDIA fences.  On ATI cards the semantics are identical
     but performance is lower.
     */
    public sealed class Milestone : ReferenceCountedObject
    {
        private object backendHandle;
        private string _name;
        private int backendMilestoneID;
        private bool _isSet;
        private RenderDevice renderDevice;

        internal Milestone(string milestoneName)
        {
            _name = milestoneName;
            backendMilestoneID = 0;
            _isSet = false;
            backendHandle = null;
            renderDevice = null;
        }

        public string name()
        {
            return _name;
        }

        public string getName()
        {
            return _name;
        }

        public bool isSet()
        {
            return _isSet;
        }

        internal int getBackendMilestoneID()
        {
            return backendMilestoneID;
        }

        internal void setBackendMilestoneID(int id)
        {
            backendMilestoneID = id;
        }

        internal void markSet()
        {
            _isSet = true;
        }

        internal void markWaited()
        {
            _isSet = false;
        }

        internal void setRenderDevice(RenderDevice device)
        {
            renderDevice = device;
        }

        internal RenderDevice getRenderDevice()
        {
            return renderDevice;
        }

        /** Set the milestone. */
        internal void set()
        {
            if (renderDevice == null)
            {
                throw new InvalidOperationException("The milestone is not bound to a render device.");
            }
            if (backendMilestoneID == 0)
            {
                throw new InvalidOperationException("The milestone was not created by a render device and has no backend fence.");
            }
            renderDevice.setMilestone(this);
        }

        /** Wait for it to be reached. */
        internal void wait()
        {
            Debug.debugAssertM(_isSet, "Waiting for a milestone (\"" + _name + "\") that was not set!");

            if (!_isSet)
            {
                throw new InvalidOperationException("Waiting for a milestone (\"" + _name + "\") that was not set!");
            }
            if (renderDevice == null)
            {
                throw new InvalidOperationException("The milestone is not bound to a render device.");
            }
            renderDevice.waitForMilestone(this);
        }

        internal object getBackendHandle()
        {
            return backendHandle;
        }

        internal void setBackendHandle(object handle)
        {
            backendHandle = handle;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (backendMilestoneID != 0)
                {
                    IGraphicsBackend backend = GLCaps.getBackendInstance();
                    if (backend != null)
                    {
                        backend.DeleteMilestone(backendMilestoneID);
                    }
                    backendMilestoneID = 0;
                }
                _isSet = false;
                backendHandle = null;
                renderDevice = null;
            }
            base.Dispose(disposing);
        }
    }
}
