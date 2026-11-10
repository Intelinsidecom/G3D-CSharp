using System;

namespace G3D
{
    public interface IGraphicsWindow : IDisposable
    {
        IntPtr NativeHandle { get; }
        int Width { get; }
        int Height { get; }
        void MakeCurrent();
        void Present();
        void Resize(int width, int height);

        // For describeSystem
        GWindow.Settings getSettings();
        string getAPIName();
        string getAPIVersion();
        bool hasFocus();

        /**
          Raised by the platform window when the user produced an input event.
          The backend translates its own platform event into the backend-neutral
          GEvent representation; GWindow subscribes and queues it so that
          GApplet::doUserInput can poll it.
        */
        event Action<GEvent> InputEvent;
    }
}
