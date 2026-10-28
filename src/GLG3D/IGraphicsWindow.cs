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
    }
}
