/**
  @file Network_Demo/messages.cs

  Port of Network_Demo/messages.h and messages.cpp.

  Messages sent between client and server.
 */

using System;
using G3D;

namespace Network_Demo
{
    /**
      Sent from the server to the client to tell them their ID.
     */
    public class SignOnMessage
    {
        /**
          The ID assigned to the player.
         */
        public int id;

        public SignOnMessage()
        {
            id = Entity.NO_ID;
        }

        public SignOnMessage(int _id)
        {
            id = _id;
        }

        public virtual void serialize(BinaryOutput b)
        {
            b.writeInt32(id);
        }

        public virtual void deserialize(BinaryInput b)
        {
            id = b.readInt32();
        }
    }

    /**
      Sent from the client to the server to indicate
      new control settings, and from the server back
      to the clients to distribute that information.
     */
    public class EntityStateMessage
    {
        /** ID of the object for which these controls apply */
        public int id;

        public Controls controls = new Controls();

        public PhysicsFrame frame = PhysicsFrame.identity();

        public Vector3 velocity;

        public virtual void serialize(BinaryOutput b)
        {
            b.writeInt32(id);
            controls.serialize(b);
            frame.serialize(b);
            velocity.serialize(b);
        }

        public virtual void deserialize(BinaryInput b)
        {
            id = b.readInt32();
            controls.deserialize(b);
            frame.deserialize(b);
            velocity.deserialize(b);
        }
    }
}
