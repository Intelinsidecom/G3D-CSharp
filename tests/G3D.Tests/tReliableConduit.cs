using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private sealed class Message
        {
            public int i32;
            public long i64;
            public string s;
            public float f;

            public Message()
            {
                i32 = g3dmath.iRandom(-10000, 10000);
                i64 = g3dmath.iRandom(-10000000, 1000000);
                f = (float)g3dmath.random(-10000, 10000);
                for (int x = 0; x < 20; ++x)
                {
                    s = s + (char)('A' + g3dmath.iRandom(0, 26));
                }
            }

            public static bool operator ==(Message a, Message b)
            {
                if (object.ReferenceEquals(a, b))
                {
                    return true;
                }
                if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null))
                {
                    return false;
                }
                return
                    (a.i32 == b.i32) &&
                    (a.i64 == b.i64) &&
                    (a.s == b.s) &&
                    (a.f == b.f);
            }

            public static bool operator !=(Message a, Message b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                return obj is Message && this == (Message)obj;
            }

            public override int GetHashCode()
            {
                return i32;
            }

            public void serialize(BinaryOutput b)
            {
                b.writeInt32(i32);
                b.writeInt64(i64);
                b.writeString(s);
                b.writeFloat32(f);
            }

            public void deserialize(BinaryInput b)
            {
                i32 = b.readInt32();
                i64 = b.readInt64();
                s = b.readString();
                f = b.readFloat32();
            }
        }


        private static void testReliableConduit(NetworkDevice nd)
        {
            printf("ReliableConduit ");

            Debug.debugAssert(nd != null);

            {
                ushort port = 10011;
                ReferenceCountedPointer<NetListener> listener = nd.createListener(port);

                ReferenceCountedPointer<ReliableConduit> clientSide =
                    nd.createReliableConduit(new NetAddress("localhost", port));
                ReferenceCountedPointer<ReliableConduit> serverSide =
                    listener.pointer().waitForConnection();

                Debug.debugAssert(clientSide.pointer().ok());
                Debug.debugAssert(serverSide.pointer().ok());

                uint type = 10;
                Message a = new Message();
                clientSide.pointer().send(type, a);

                // Wait for message
                while (serverSide.pointer().waitingMessageType() == 0) { }

                Message b = new Message();
                Debug.debugAssert(serverSide.pointer().waitingMessageType() == type);
                serverSide.pointer().receive(b);

                Debug.debugAssert(a == b);

                a = new Message();
                serverSide.pointer().send(type, a);

                // Wait for message
                while (clientSide.pointer().waitingMessageType() == 0) { }
                Debug.debugAssert(clientSide.pointer().waitingMessageType() == type);
                clientSide.pointer().receive(b);

                Debug.debugAssert(a == b);

                // Make sure no more messages are waiting
                Debug.debugAssert(clientSide.pointer().waitingMessageType() == 0);
                Debug.debugAssert(serverSide.pointer().waitingMessageType() == 0);

                serverSide.Dispose();
                clientSide.Dispose();
                listener.Dispose();
            }

            printf("passed\n");
        }
    }
}
