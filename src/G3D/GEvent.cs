/**
  @file GEvent.cs

  @maintainer Morgan McGuire, matrix@graphics3d.com
*/

using System;
using G3D;

namespace G3D
{
    public enum GEventType
    {
        NONE,
        KEY_DOWN,
        KEY_UP,
        MOUSE_DOWN,
        MOUSE_UP,
        MOUSE_MOVE,
        MOUSE_WHEEL,
        RESIZE,
        QUIT
    }

    public class GEvent
    {
        public GEventType type = GEventType.NONE;
        public int key;
        public int mouseButton;
        public int mouseX, mouseY;
        public int mouseDeltaX, mouseDeltaY;
        public int wheelDelta;
        public int width, height;

        public GEvent()
        {
        }

        public GEvent(GEventType t)
        {
            type = t;
        }
    }
}