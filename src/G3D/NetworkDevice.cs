/**
  @file NetworkDevice.h

  These classes abstract networking from the socket level to a
  serialized messaging style that is more appropriate for games.  The
  performance has been tuned for sending many small messages.  The
  message protocol contains a header that prevents them from being used
  with raw UDP/TCP (e.g. connecting to an HTTP server). 

  LightweightConduit and ReliableConduits have different interfaces
  because they have different semantics.  You would never want to
  interchange them without rewriting the surrounding code.

  NetworkDevice creates conduits because they need access to a global
  log pointer and because I don't want non-reference counted conduits
  being created.

  Be careful with threads and reference counting.  The reference
  counters are not threadsafe, and are also not updated correctly if a
  thread is explicitly killed.  Since the conduits will be passed by
  const XConduitRef& most of the time this doesn't appear as a major
  problem.  With non-blocking conduits, you should need few threads
  anyway.

  LightweightConduits preceed each message with a 4-byte host order
  unsigned integer that is the message type.  This does not appear in
  the message serialization/deserialization.

  ReliableConduits preceed each message with two 4-byte host order
  unsigned integers.  The first is the message type and the second
  indicates the length of the rest of the data.  The size does not
  include the size of the header itself.  The minimum message is 9
  bytes, a 4-byte types, a 4-byte header of "1" and one byte of data.

 @maintainer Morgan McGuire, morgan@graphics3d.com
 @created 2002-11-22
 @edited  2006-02-25
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace G3D
{
    /**
     Interface for data sent through a conduit.
     NetMessages automatically serialize and deserialize themselves when
     sent through ReliableConduit and LightweightConduit.  An application
     contains many subclasses of NetMessage, one for each kind of message
     (e.g. SignOnMessage, CreatePlayer, ChatMessage, PlaySoundMessage, 
     SignOffMessage).  Because the specific messages needed depend on the
     application, they are not part of G3D.

     @deprecated Send and receive methods now directly accept serializable
       objects.
     */
    public abstract class NetMessage
    {
        /** This must return a value even for an uninitalized instance.
           Create an enumeration for your message types and return
           one of those values.  It will be checked on both send and
           receive as a form of runtime type checking. 
        
           Values less than 1000 are reserved for the system.*/
        public abstract uint type();
        public abstract void serialize(BinaryOutput b);
        public abstract void deserialize(BinaryInput b);
    }

    internal static class NetSerialization
    {
        private static readonly Dictionary<Type, MethodInfo> serializers = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> deserializers = new Dictionary<Type, MethodInfo>();

        private static MethodInfo find(Type type, string name, Type argument)
        {
            MethodInfo method = type.GetMethod(name, new Type[] { argument });
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }
            return method;
        }

        public static void serialize(object message, BinaryOutput b)
        {
            Type type = message.GetType();
            MethodInfo method;
            if (!serializers.TryGetValue(type, out method))
            {
                method = find(type, "serialize", typeof(BinaryOutput));
                serializers[type] = method;
            }
            method.Invoke(message, new object[] { b });
        }

        public static void deserialize(object message, BinaryInput b)
        {
            Type type = message.GetType();
            MethodInfo method;
            if (!deserializers.TryGetValue(type, out method))
            {
                method = find(type, "deserialize", typeof(BinaryInput));
                deserializers[type] = method;
            }
            method.Invoke(message, new object[] { b });
        }
    }

    public abstract class Conduit : ReferenceCountedObject
    {
        protected ulong mSent;
        protected ulong mReceived;
        protected ulong bSent;
        protected ulong bReceived;

        protected NetworkDevice nd;
        protected Socket sock;

        /**
         Used for serialization.  One per socket
         to make this threadsafe.
         */
        protected BinaryOutput binaryOutput;

        protected Conduit(NetworkDevice _nd)
        {
            mSent = 0;
            mReceived = 0;
            bSent = 0;
            bReceived = 0;
            nd = _nd;
            sock = null;
            binaryOutput = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
        }

        protected override void dispose()
        {
            nd.closesocket(ref sock);
        }

        public ulong bytesSent()
        {
            return bSent;
        }

        public ulong messagesSent()
        {
            return mSent;
        }

        public ulong bytesReceived()
        {
            return bReceived;
        }

        public ulong messagesReceived()
        {
            return mReceived;
        }

        /**
         If true, receive will return true.
         */
        public virtual bool messageWaiting()
        {
            return nd.readWaiting(sock);
        }

        /**
         Returns the type of the waiting message (i.e. the type supplied
         with send).  The return value is zero when there is no message
         waiting.
         */
        public abstract uint waitingMessageType();

        /** Returns true if the connection is ok. */
        public bool ok()
        {
            return sock != null;
        }
    }

    /**
     A conduit that guarantees messages will arrive, intact and in order.
     Create on the client using NetworkDevice::createReliableConduit and
     on the server using NetListener::waitForConnection.  Set the reference
     counted pointer to NULL to disconnect.
     */
    public class ReliableConduit : Conduit
    {
        private enum State { RECEIVING, HOLDING, NO_MESSAGE }

        private State state;

        private NetAddress addr;

        /**
         Type of the incoming message.
         */
        private uint messageType;

        /** 
         Total size of the incoming message (read from the header).
         */
        private uint messageSize;

        /** Shared buffer for receiving messages. */
        private byte[] receiveBuffer;

        /** Total size of the receiveBuffer. */
        private int receiveBufferTotalSize;

        /** Size occupied by the current message... so far.  This will be
            equal to messageSize when the whole message has arrived. 
          */
        private int receiveBufferUsedSize;

        internal ReliableConduit(NetworkDevice _nd, NetAddress _addr)
            : base(_nd)
        {
            state = State.NO_MESSAGE;
            receiveBuffer = null;
            receiveBufferTotalSize = 0;
            receiveBufferUsedSize = 0;
            messageType = 0;

            addr = _addr;
            if (nd.log() != null) { nd.log().print("Creating a TCP socket       "); }
            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception)
            {
                sock = null;
            }

            if (sock == null)
            {
                if (nd.log() != null)
                {
                    nd.log().println("FAIL");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
                return;
            }

            if (nd.log() != null) { nd.log().println("Ok"); }

            // Setup socket options (both constructors should set the same options)

            // Disable Nagle's algorithm (we send lots of small packets)
            try
            {
                sock.NoDelay = true;
                if (nd.log() != null) { nd.log().println("Disabled Nagel's algorithm."); }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("WARNING: Disabling Nagel's algorithm failed.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
            }

            // Set the NO LINGER option so the socket doesn't hang around if
            // there is unsent data in the queue when it closes.
            try
            {
                sock.LingerState = new LingerOption(false, 0);
                if (nd.log() != null) { nd.log().println("Set socket option no_linger."); }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("WARNING: Setting socket no linger failed.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
            }

            // Set reuse address so that a new server can start up soon after
            // an old one has closed.
            try
            {
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                if (nd.log() != null) { nd.log().println("Set socket option reuseaddr."); }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("WARNING: Setting socket reuseaddr failed.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
            }

            // Ideally, we'd like to specify IPTOS_LOWDELAY as well.

            if (nd.log() != null)
            {
                NetworkDevice.logSocketInfo(nd.log(), sock);
            }

            NetworkDevice.increaseBufferSize(sock, nd.log());

            if (nd.log() != null) { nd.log().printf("Created TCP socket %d\n", sock.Handle.ToInt64()); }

            string x = addr.toString();
            if (nd.log() != null)
            {
                nd.log().printf("Connecting to %s on TCP socket %d   ",
                                x, sock.Handle.ToInt64());
            }

            try
            {
                sock.Connect(NetworkDevice.toEndPoint(addr));

                if (nd.log() != null) { nd.log().println("Ok"); }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("FAIL");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
                return;
            }
        }

        internal ReliableConduit(NetworkDevice _nd, Socket _sock, NetAddress _addr)
            : base(_nd)
        {
            state = State.NO_MESSAGE;
            receiveBuffer = null;
            receiveBufferTotalSize = 0;
            receiveBufferUsedSize = 0;
            messageType = 0;

            sock = _sock;
            addr = _addr;

            // Setup socket options (both constructors should set the same options)

            // Disable Nagle's algorithm (we send lots of small packets)
            try
            {
                sock.NoDelay = true;
                if (nd.log() != null) { nd.log().println("Disabled Nagel's algorithm."); }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("WARNING: Disabling Nagel's algorithm failed.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
            }

            // Set the NO LINGER option so the socket doesn't hang around if
            // there is unsent data in the queue when it closes.
            try
            {
                sock.LingerState = new LingerOption(false, 0);
                if (nd.log() != null) { nd.log().println("Set socket option no_linger."); }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("WARNING: Setting socket no linger failed.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
            }

            // Set reuse address so that a new server can start up soon after
            // an old one has closed.
            try
            {
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                if (nd.log() != null) { nd.log().println("Set socket option reuseaddr."); }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("WARNING: Setting socket reuseaddr failed.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
            }

            // Ideally, we'd like to specify IPTOS_LOWDELAY as well.

            if (nd.log() != null)
            {
                NetworkDevice.logSocketInfo(nd.log(), sock);
            }
        }

        protected override void dispose()
        {
            receiveBuffer = null;
            receiveBufferTotalSize = 0;
            receiveBufferUsedSize = 0;
            base.dispose();
        }

        private void serializeMessage<T>(uint t, T m, BinaryOutput b)
        {
            b.writeUInt32(t);

            // Reserve space for the 4 byte size header
            b.writeUInt32(0);

            int L = b.length();
            NetSerialization.serialize(m, b);
            if (b.length() == L)
            {
                // No data was created by serialization.
                // We need to send at least one byte because receive assumes that
                // a zero length message is an error.
                b.writeUInt8(0xFF);
            }

            uint len = (uint)(b.getLength() - 8);

            // We send the length first to tell recv how much data to read.
            // Here we abuse BinaryOutput a bit and write directly into
            // its buffer, violating the abstraction.
            NetworkDevice.writeBigEndianUInt32(b.getCArray(), 4, len);
        }

        private void sendBuffer(BinaryOutput b)
        {
            int total = 0;
            int length = b.getLength();
            try
            {
                while (total < length)
                {
                    int ret = sock.Send(b.getCArray(), total, length - total, SocketFlags.None);
                    if (ret <= 0)
                    {
                        throw new SocketException((int)SocketError.ConnectionReset);
                    }
                    total += ret;
                }
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("Error occured while sending message.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
                return;
            }

            ++mSent;
            bSent += (ulong)length;

            // Verify the packet was actually sent
            // Conversion to unsigned is safe because -1 is caught earlier
            Debug.debugAssert(total == length);
        }

        /**
         Serializes the message and schedules it to be sent as soon as possible,
         then returns immediately.

         The actual data sent across the network is preceeded by the
         message type and the size of the serialized message as a 32-bit
         integer.  The size is sent because TCP is a stream protocol and
         doesn't have a concept of discrete messages.

         @deprecated Use send(type, message)
         */
        public void send(NetMessage m)
        {
            if (m == null)
            {
                send(unchecked((uint)-1));
            }
            else
            {
                send(m.type(), m);
            }
        }

        // The message is actually copied from the socket to an internal buffer during
        // this call.  Receive only deserializes.
        public override bool messageWaiting()
        {
            switch (state)
            {
            case State.HOLDING:
                // We've already read the message and are waiting
                // for a receive call.
                return true;

            case State.RECEIVING:

                if (!ok())
                {
                    return false;
                }
                // We're currently receiving the message.  Read a little more.
                receiveIntoBuffer();

                if (messageSize == receiveBufferUsedSize)
                {
                    // We've read the whole mesage.  Switch to holding state 
                    // and return true.
                    state = State.HOLDING;
                    return true;
                }
                else
                {
                    // There are more bytes left to read.  We'll read them on
                    // the next call.  Because the *entire* message is not ready,
                    // return false.
                    return false;
                }

            case State.NO_MESSAGE:
                if (base.messageWaiting())
                {
                    // Message incoming.  Read the header.

                    state = State.RECEIVING;
                    receiveHeader();

                    // Loop back around now that we're in the receive state; we
                    // may be able to read the whole message before returning 
                    // to the caller.
                    return messageWaiting();
                }
                else
                {
                    // No message incoming.
                    return false;
                }
            }

            Debug.debugAssertM(false, "Should not reach this point");
            return false;
        }

        /**
         Serializes the message and schedules it to be sent as soon as possible,
         and then returns immediately.  The message can be any <B>class</B> with
         a serialize and deserialize method.  On the receiving side,
         use G3D::ReliableConduit::waitingMessageType() to detect the incoming
         message and then invoke G3D::ReliableConduit::receive(msg) where msg
         is of the same class as the message that was sent.
         */
        public void send<T>(uint type, T message)
        {
            binaryOutput.reset();
            serializeMessage(type, message, binaryOutput);
            sendBuffer(binaryOutput);
        }

        /** Sends an empty message with the given type.  Useful for sending 
            commands that have no parameters. */
        public void send(uint type)
        {
            binaryOutput.reset();
            binaryOutput.writeUInt32(type);

            // Reserve space for the 4 byte size header
            binaryOutput.writeUInt32(0);

            // No data was created by serialization.
            // We need to send at least one byte because receive assumes that
            // a zero length message is an error.
            binaryOutput.writeUInt8(0xFF);

            uint len = (uint)(binaryOutput.getLength() - 8);
            NetworkDevice.writeBigEndianUInt32(binaryOutput.getCArray(), 4, len);

            sendBuffer(binaryOutput);
        }

        /** @deprecated Use multisend(Array<>, uint32, T)*/
        public static void multisend(Array<ReferenceCountedPointer<ReliableConduit>> array, NetMessage m)
        {
            Debug.debugAssert(m != null);
            multisend(array, m.type(), m);
        }

        /** Send the same message to a number of conduits.  Useful for sending
            data from a server to many clients (only serializes once). */
        public static void multisend<T>(Array<ReferenceCountedPointer<ReliableConduit>> array, uint type, T m)
        {
            if (array.size() > 0)
            {
                array[0].pointer().binaryOutput.reset();
                array[0].pointer().serializeMessage(type, m, array[0].pointer().binaryOutput);

                for (int i = 0; i < array.size(); ++i)
                {
                    array[i].pointer().sendBuffer(array[0].pointer().binaryOutput);
                }
            }
        }

        public override uint waitingMessageType()
        {
            // The messageWaiting call is what actually receives the message.
            if (messageWaiting())
            {
                return messageType;
            }
            else
            {
                return 0;
            }
        }

        /** If a message is waiting, deserializes the waiting message into
            m and returns true, otherwise returns false.
            
            If m == NULL, the message is pulled from the conduit and discarded.
            */
        public bool receive(NetMessage m)
        {
            if (m == null)
            {
                receive();
                return true;
            }
            else
            {
                return receive<NetMessage>(m);
            }
        }

        /** 
            If a message is waiting, deserializes the waiting message into
            message and returns true, otherwise returns false.  You can
            determine the type of the message (and therefore, the class
            of message) using G3D::ReliableConduit::waitingMessageType().
         */
        public bool receive<T>(T message) where T : class
        {
            if (!messageWaiting())
            {
                return false;
            }

            Debug.debugAssert(state == State.HOLDING);
            // Deserialize
            BinaryInput b = new BinaryInput(receiveBuffer, receiveBufferUsedSize,
                                            G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            NetSerialization.deserialize(message, b);

            // Don't let anyone read this message again.  We leave the buffer
            // allocated for the next caller, however.
            receiveBufferUsedSize = 0;
            state = State.NO_MESSAGE;
            messageType = 0;
            messageSize = 0;

            // Potentially read the next message.
            messageWaiting();

            return true;
        }

        public bool receive<T>(ref T message)
        {
            object boxed = message;
            if (!messageWaiting())
            {
                return false;
            }

            Debug.debugAssert(state == State.HOLDING);
            BinaryInput b = new BinaryInput(receiveBuffer, receiveBufferUsedSize,
                                            G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            NetSerialization.deserialize(boxed, b);
            message = (T)boxed;

            receiveBufferUsedSize = 0;
            state = State.NO_MESSAGE;
            messageType = 0;
            messageSize = 0;

            messageWaiting();

            return true;
        }

        /** Removes the current message from the queue. */
        public void receive()
        {
            if (!messageWaiting())
            {
                return;
            }
            receiveBufferUsedSize = 0;
            state = State.NO_MESSAGE;
            messageType = 0;
            messageSize = 0;

            // Potentially read the next message.
            messageWaiting();
        }

        private void receiveHeader()
        {
            Debug.debugAssert(state == State.RECEIVING);

            // Read the type
            byte[] tmp = new byte[4];
            int ret = NetworkDevice.receiveBytes(sock, tmp, 4);

            // The type is the first four bytes.  It is little endian.
            messageType = NetworkDevice.readLittleEndianUInt32(tmp, 0);

            if (ret != 4)
            {
                if (nd.log() != null)
                {
                    nd.log().printf("Call to recv failed.  ret = %d, sizeof(messageType) = %d\n",
                                    ret, 4);
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
                messageType = 0;
                return;
            }

            // Read the size
            ret = NetworkDevice.receiveBytes(sock, tmp, 4);
            messageSize = NetworkDevice.readBigEndianUInt32(tmp, 0);

            if (ret != 4)
            {
                if (nd.log() != null)
                {
                    nd.log().printf("Call to recv failed.  ret = %d, sizeof(len) = %d\n",
                                    ret, 4);
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
                messageType = 0;
                return;
            }

            Debug.debugAssert(messageSize < 6e6);

            Debug.debugAssert(receiveBufferUsedSize == 0);

            // Extend the size of the buffer.
            if (messageSize > receiveBufferTotalSize)
            {
                receiveBuffer = new byte[messageSize];
                receiveBufferTotalSize = (int)messageSize;
            }

            if (receiveBuffer == null)
            {
                if (nd.log() != null)
                {
                    nd.log().println("Could not allocate a memory buffer during receivePacket.");
                }
                nd.closesocket(ref sock);
            }

            bReceived += 4;
        }

        private void receiveIntoBuffer()
        {
            Debug.debugAssert(state == State.RECEIVING);
            Debug.debugAssert(messageType != 0);
            Debug.debugAssertM(receiveBufferUsedSize < messageSize, "Message already received.");
            Debug.debugAssertM(messageSize >= receiveBufferUsedSize, "Message size overflow.");

            if (!nd.readWaiting(sock))
            {
                return;
            }

            int left = (int)(messageSize - receiveBufferUsedSize);
            int ret = 0;
            try
            {
                if (sock.Available > 0)
                {
                    ret = sock.Receive(receiveBuffer, receiveBufferUsedSize,
                                       Math.Min(left, sock.Available), SocketFlags.None);
                }
            }
            catch (Exception)
            {
                ret = -1;
            }

            if (ret > 0)
            {
                receiveBufferUsedSize += ret;
                bReceived += (ulong)ret;
            }

            if ((ret == 0) || (ret < 0))
            {
                if (nd.log() != null)
                {
                    if (ret < 0)
                    {
                        nd.log().printf("Call to recv failed.  ret = %d, sizeof(messageSize) = %d\n",
                                        ret, messageSize);
                        nd.log().println(NetworkDevice.socketErrorCode());
                    }
                    else
                    {
                        nd.log().printf("recv returned 0\n");
                    }
                }
                nd.closesocket(ref sock);
                return;
            }

            ++mReceived;
        }

        public NetAddress address()
        {
            return addr;
        }
    }

    /**
     Provides fast but unreliable transfer of messages.
     */
    public class LightweightConduit : Conduit
    {
        /**
         True when waitingForMessageType has read the message
         from the network into messageType/messageStream.
         */
        private bool alreadyReadMessage;

        /**
         Origin of the received message.
         */
        private NetAddress messageSender;

        /**
         The type of the last message received.
         */
        private uint messageType;

        /**
         The message received (the type has already been read off).
         */
        private byte[] messageBuffer;

        private int messageBufferSize;

        internal LightweightConduit(NetworkDevice _nd, ushort receivePort,
                                    bool enableReceive, bool enableBroadcast)
            : base(_nd)
        {
            messageBuffer = null;
            messageBufferSize = 0;
            messageType = 0;
            alreadyReadMessage = false;

            if (nd.log() != null) { nd.log().print("Creating a UDP socket        "); }
            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            catch (Exception)
            {
                sock = null;
            }

            if (sock == null)
            {
                sock = null;
                if (nd.log() != null)
                {
                    nd.log().println("FAIL");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                return;
            }
            if (nd.log() != null) { nd.log().println("Ok"); }

            if (enableReceive)
            {
                Debug.debugAssert(receivePort != 0);
                if (!nd.bind(sock, new NetAddress(0, receivePort)))
                {
                    // The socket could not be bound (the port is in use).  Leave
                    // ok() false so callers detect this, but set the MTU so a
                    // later send on the dead conduit reports the real reason
                    // ("socket not ok") instead of a nonsensical negative MTU.
                    sock = null;
                    MTU = 1000;
                    return;
                }
            }

            // Figuring out the MTU seems very complicated, so we just set it to 1000,
            // which is likely to be safe.  See IP_MTU for more information.
            MTU = 1000;

            NetworkDevice.increaseBufferSize(sock, nd.log());

            if (enableBroadcast)
            {
                try
                {
                    sock.EnableBroadcast = true;
                }
                catch (Exception)
                {
                    if (nd.log() != null)
                    {
                        nd.log().println("Call to setsockopt failed");
                        nd.log().println(NetworkDevice.socketErrorCode());
                    }
                    nd.closesocket(ref sock);
                    return;
                }
            }

            if (nd.log() != null)
            {
                nd.log().printf("Done creating UDP socket %d\n", sock.Handle.ToInt64());
            }
        }

        /** Maximum transmission unit (packet size in bytes) for this socket.
            May vary between sockets. */
        private int MTU;

        private void serializeMessage(NetMessage m, BinaryOutput b)
        {
            checkSocketUsable();

            if (m != null)
            {
                Debug.debugAssert(m.type() != 0);
                b.writeUInt32(m.type());
                m.serialize(b);
                b.writeUInt32(1);
            }

            Debug.debugAssertM(b.size() < MTU,
                        Format.format("This LightweightConduit is limited to messages of "
                                      + "%d bytes (Ethernet hardware limit; this is the "
                                      + "'UDP MTU')", maxMessageSize()));

            if (b.size() >= MTU)
            {
                throw new PacketSizeException(
                        Format.format("This LightweightConduit is limited to messages of "
                                      + "%d bytes (Ethernet hardware limit; this is the "
                                      + "'UDP MTU')", maxMessageSize()),
                        b.size() - 4, // Don't count the type header
                        maxMessageSize());
            }
        }

        /**
          When the socket could not be created or bound, a send would otherwise
          fail with a confusing size assertion (the MTU has no meaning on a dead
          socket).  Report the real reason instead.
        */
        private void checkSocketUsable()
        {
            if (sock == null)
            {
                throw new InvalidOperationException(
                    "Cannot send on a LightweightConduit whose socket could not be created or bound; the port is probably already in use.");
            }
        }

        private void serializeMessage<T>(uint type, T m, BinaryOutput b)
        {
            Debug.debugAssert(type != 0);
            b.writeUInt32(type);
            NetSerialization.serialize(m, b);
            b.writeUInt32(1);

            checkSocketUsable();

            Debug.debugAssertM(b.size() < MTU,
                        Format.format("This LightweightConduit is limited to messages of "
                                      + "%d bytes (Ethernet hardware limit; this is the "
                                      + "'UDP MTU')", maxMessageSize()));

            if (b.size() >= MTU)
            {
                throw new PacketSizeException(
                        Format.format("This LightweightConduit is limited to messages of "
                                      + "%d bytes (Ethernet hardware limit; this is the "
                                      + "'UDP MTU')", maxMessageSize()),
                        b.size() - 4, // Don't count the type header
                        maxMessageSize());
            }
        }

        public sealed class PacketSizeException : Exception
        {
            public int serializedPacketSize;
            public int maxMessageSize;

            public PacketSizeException(string m, int s, int b)
                : base(m)
            {
                serializedPacketSize = s;
                maxMessageSize = b;
            }
        }

        /** The maximum length of a message that can be sent
            (G3D places a small header at the front of each UDP packet;
            this is already taken into account by the value returned).
         */
        public int maxMessageSize()
        {
            return MTU - 4;
        }

        private void sendBuffer(NetAddress a, BinaryOutput b)
        {
            try
            {
                int sent = sock.SendTo(b.getCArray(), 0, b.getLength(), SocketFlags.None,
                                       NetworkDevice.toEndPoint(a));
                if (sent != b.getLength())
                {
                    throw new SocketException((int)SocketError.MessageSize);
                }
                ++mSent;
                bSent += (ulong)b.getLength();
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().printf("Error occured while sending packet to %s\n", a.ipString());
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
            }
        }

        /** Serializes and sends the message immediately. Data may not
            arrive and may arrive out of order, but individual messages
            are guaranteed to not be corrupted.  If the message is null,
            an empty message is still sent.

            Throws PacketSizeException if the serialized message exceeds
            maxMessageSize. */
        public void send(NetAddress a, NetMessage m)
        {
            binaryOutput.reset();
            serializeMessage(m, binaryOutput);
            sendBuffer(a, binaryOutput);
        }

        public void send<T>(NetAddress a, uint type, T msg)
        {
            binaryOutput.reset();
            serializeMessage(type, msg, binaryOutput);
            sendBuffer(a, binaryOutput);
        }

        /** Send the same message to multiple addresses (only serializes once).
            Useful when server needs to send to a known list of addresses
            (unlike direct UDP broadcast to all addresses on the subnet) 
            @deprecated*/
        public void send(Array<NetAddress> a, NetMessage m)
        {
            binaryOutput.reset();
            serializeMessage(m, binaryOutput);

            for (int i = 0; i < a.size(); ++i)
            {
                sendBuffer(a[i], binaryOutput);
            }
        }

        /** Send the same message to multiple addresses (only serializes once).
            Useful when server needs to send to a known list of addresses
            (unlike direct UDP broadcast to all addresses on the subnet) */
        public void send<T>(Array<NetAddress> a, uint type, T m)
        {
            binaryOutput.reset();
            serializeMessage(type, m, binaryOutput);

            for (int i = 0; i < a.size(); ++i)
            {
                sendBuffer(a[i], binaryOutput);
            }
        }

        public void send(Array<NetAddress> a)
        {
            send(a, (NetMessage)null);
        }

        public void send(NetAddress a)
        {
            send(a, (NetMessage)null);
        }

        /** If data is waiting, deserializes the waiting message into m,
            puts the sender's address in addr and returns true, otherwise
            returns false.  If m is NULL, the message is consumed but not
            deserialized.
        */
        public bool receive(ref NetAddress sender, NetMessage m)
        {
            // This both checks to ensure that a message was waiting and
            // actively consumes the message from the network stream if
            // it has not been read yet.
            uint t = waitingMessageType();
            if (t == 0)
            {
                return false;
            }

            if (m != null)
            {
                Debug.debugAssert(m.type() == t);
            }

            sender = messageSender;
            alreadyReadMessage = false;

            if (messageBufferSize < 4)
            {
                // Something went wrong
                return false;
            }

            if (m != null)
            {
                byte[] payload = new byte[messageBufferSize - 4];
                global::System.Array.Copy(messageBuffer, 4, payload, 0, payload.Length);
                BinaryInput b = new BinaryInput(payload, payload.Length,
                                                G3DEndian.G3D_LITTLE_ENDIAN, false, false);
                m.deserialize(b);
            }

            return true;
        }

        public bool receive<T>(ref NetAddress sender, T message) where T : class
        {
            // This both checks to ensure that a message was waiting and
            // actively consumes the message from the network stream if
            // it has not been read yet.
            uint t = waitingMessageType();
            if (t == 0)
            {
                return false;
            }

            sender = messageSender;
            alreadyReadMessage = false;

            if (messageBufferSize < 4)
            {
                // Something went wrong
                return false;
            }

            byte[] payload = new byte[messageBufferSize - 4];
            global::System.Array.Copy(messageBuffer, 4, payload, 0, payload.Length);
            BinaryInput b = new BinaryInput(payload, payload.Length,
                                            G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            NetSerialization.deserialize(message, b);

            return true;
        }

        public bool receive<T>(ref NetAddress sender, ref T message)
        {
            uint t = waitingMessageType();
            if (t == 0)
            {
                return false;
            }

            sender = messageSender;
            alreadyReadMessage = false;

            if (messageBufferSize < 4)
            {
                return false;
            }

            byte[] payload = new byte[messageBufferSize - 4];
            global::System.Array.Copy(messageBuffer, 4, payload, 0, payload.Length);
            BinaryInput b = new BinaryInput(payload, payload.Length,
                                            G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            object boxed = message;
            NetSerialization.deserialize(boxed, b);
            message = (T)boxed;

            return true;
        }

        public bool receive(ref NetAddress sender)
        {
            // This both checks to ensure that a message was waiting and
            // actively consumes the message from the network stream if
            // it has not been read yet.
            uint t = waitingMessageType();
            if (t == 0)
            {
                return false;
            }

            sender = messageSender;
            alreadyReadMessage = false;

            if (messageBufferSize < 4)
            {
                // Something went wrong
                return false;
            }

            return true;
        }

        public bool receive()
        {
            NetAddress ignore = new NetAddress();
            return receive(ref ignore);
        }

        public override uint waitingMessageType()
        {
            if (!messageWaiting())
            {
                return 0;
            }

            if (!alreadyReadMessage)
            {
                messageBuffer = new byte[8192];

                EndPoint remote_addr = new IPEndPoint(IPAddress.Any, 0);
                int ret;
                try
                {
                    ret = sock.ReceiveFrom(messageBuffer, 0, messageBuffer.Length,
                                           SocketFlags.None, ref remote_addr);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        messageBuffer = null;
                        messageBufferSize = 0;
                        messageSender = new NetAddress();
                        messageType = 0;
                        return 0;
                    }

                    if (nd.log() != null)
                    {
                        nd.log().println("Error: recvfrom failed in LightweightConduit::waitingMessageType().");
                        nd.log().println(NetworkDevice.socketErrorCode());
                    }
                    nd.closesocket(ref sock);
                    messageBuffer = null;
                    messageBufferSize = 0;
                    messageSender = new NetAddress();
                    messageType = 0;
                    return 0;
                }
                catch (Exception)
                {
                    if (nd.log() != null)
                    {
                        nd.log().println("Error: recvfrom failed in LightweightConduit::waitingMessageType().");
                        nd.log().println(NetworkDevice.socketErrorCode());
                    }
                    nd.closesocket(ref sock);
                    messageBuffer = null;
                    messageBufferSize = 0;
                    messageSender = new NetAddress();
                    messageType = 0;
                    return 0;
                }

                IPEndPoint endpoint = (IPEndPoint)remote_addr;
                messageSender = new NetAddress(endpoint.Address, (ushort)endpoint.Port);

                ++mReceived;
                bReceived += (ulong)ret;

                messageBufferSize = ret;

                // The type is the first four bytes.  It is little endian.
                messageType = NetworkDevice.readLittleEndianUInt32(messageBuffer, 0);

                alreadyReadMessage = true;
            }

            return messageType;
        }

        public override bool messageWaiting()
        {
            // We may have already pulled the message off the network stream
            return alreadyReadMessage || base.messageWaiting();
        }
    }

    /**
     Runs on the server listening for clients trying to make reliable connections.
     */
    public class NetListener : ReferenceCountedObject
    {
        private NetworkDevice nd;
        private Socket sock;

        /** Port is in host byte order. */
        internal NetListener(NetworkDevice _nd, ushort port)
        {
            nd = _nd;
            sock = null;

            // Start the listener socket
            if (nd.log() != null) { nd.log().print("Creating a listener            "); }
            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception)
            {
                sock = null;
            }

            if (sock == null)
            {
                if (nd.log() != null)
                {
                    nd.log().println("FAIL");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                return;
            }
            if (nd.log() != null) { nd.log().println("Ok"); }

            if (!nd.bind(sock, new NetAddress(0, port)))
            {
                if (nd.log() != null) { nd.log().println("Unable to bind!"); }
                nd.closesocket(ref sock);
                return;
            }

            if (nd.log() != null)
            {
                nd.log().printf("Listening on port %5d        ", port);
            }

            // listen is supposed to return 0 when there is no error.
            // The 2nd argument is the number of connections to allow pending
            // at any time.
            try
            {
                sock.Listen(100);
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("FAIL");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
                return;
            }

            if (nd.log() != null)
            {
                nd.log().println("Ok");
                nd.log().printf("Now listening on socket %d.\n\n", sock.Handle.ToInt64());
            }
        }

        protected override void dispose()
        {
            nd.closesocket(ref sock);
        }

        /** Block until a connection is received.  Returns NULL if 
            something went wrong. */
        public ReferenceCountedPointer<ReliableConduit> waitForConnection()
        {
            // The address of the connecting host
            if (nd.log() != null)
            {
                nd.log().println("Blocking in NetListener::waitForConnection().");
            }

            Socket sClient;
            EndPoint remote_addr = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                sClient = sock.Accept();
                remote_addr = sClient.RemoteEndPoint;
            }
            catch (Exception)
            {
                if (nd.log() != null)
                {
                    nd.log().println("Error in NetListener::acceptConnection.");
                    nd.log().println(NetworkDevice.socketErrorCode());
                }
                nd.closesocket(ref sock);
                return null;
            }

            IPEndPoint endpoint = (IPEndPoint)remote_addr;
            if (nd.log() != null)
            {
                nd.log().printf("%s connected, transferred to socket %d.\n",
                                endpoint.Address.ToString(), sClient.Handle.ToInt64());
            }

            return new ReferenceCountedPointer<ReliableConduit>(
                new ReliableConduit(nd, sClient,
                    new NetAddress(endpoint.Address, (ushort)endpoint.Port)));
        }

        public bool ok()
        {
            return sock != null;
        }

        public bool clientWaiting()
        {
            return nd.readWaiting(sock);
        }
    }

    /**
     An abstraction over sockets that provides a message based network
     infrastructure optimized for sending many small (>500 byte) messages.
     All functions always return immediately.
     <P>
     Create only one NetworkDevice per-process (WinSock restriction).
     <P>
     NetworkDevice is technically not thread safe.  However, as long as you
     use different conduits on different threads (or lock conduits before sending), you will encounter no
     problems sharing the single NetworkDevice across multiple threads.  That is,
     do not invoke a Conduit's send or receive method on two threads at once.

     This assumes that the underlying WinSock/BSD sockets implementation
     is thread safe.  That is not guaranteed, but in practice seems
     to always be true (e.g.
     http://tangentsoft.net/wskfaq/intermediate.html#threadsafety)

     */
    public class NetworkDevice
    {
        private Log debugLog;
        private bool initialized;

        public NetworkDevice()
        {
            debugLog = null;
            initialized = false;
        }

        public Log log()
        {
            return debugLog;
        }

        internal static string socketErrorCode()
        {
            return "CODE " + global::System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        }

        internal static string socketErrorCode(int code)
        {
            return "CODE " + code.ToString();
        }

        internal void closesocket(ref Socket sock)
        {
            if (sock != null)
            {
                try
                {
                    sock.Close();
                }
                catch (Exception)
                {
                }

                if (debugLog != null) { debugLog.printf("Closed socket %d\n", sock.Handle.ToInt64()); }
                sock = null;
            }
        }

        internal bool bind(Socket sock, NetAddress addr)
        {
            if (debugLog != null)
            {
                debugLog.printf("Binding socket %d on port %d  ",
                                sock.Handle.ToInt64(), addr.port());
            }

            try
            {
                sock.Bind(toEndPoint(addr));
            }
            catch (Exception)
            {
                if (debugLog != null)
                {
                    debugLog.println("FAIL");
                    debugLog.println(socketErrorCode());
                }
                closesocket(ref sock);
                return false;
            }

            if (debugLog != null) { debugLog.println("Ok"); }
            return true;
        }

        internal static IPEndPoint toEndPoint(NetAddress a)
        {
            uint ip = a.ip();
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(ip >> 24);
            bytes[1] = (byte)(ip >> 16);
            bytes[2] = (byte)(ip >> 8);
            bytes[3] = (byte)ip;
            return new IPEndPoint(new IPAddress(bytes), a.port());
        }

        internal static void writeBigEndianUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        internal static uint readBigEndianUInt32(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24) |
                   ((uint)buffer[offset + 1] << 16) |
                   ((uint)buffer[offset + 2] << 8) |
                   buffer[offset + 3];
        }

        internal static uint readLittleEndianUInt32(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset + 3] << 24) |
                   ((uint)buffer[offset + 2] << 16) |
                   ((uint)buffer[offset + 1] << 8) |
                   buffer[offset];
        }

        internal static int receiveBytes(Socket sock, byte[] buffer, int count)
        {
            int total = 0;
            try
            {
                while (total < count)
                {
                    int ret = sock.Receive(buffer, total, count - total, SocketFlags.None);
                    if (ret <= 0)
                    {
                        return total;
                    }
                    total += ret;
                }
            }
            catch (Exception)
            {
                return -1;
            }
            return total;
        }

        internal static void logSocketInfo(Log debugLog, Socket sock)
        {
            try
            {
                debugLog.printf("SOL_SOCKET/SO_RCVBUF = %d\n", sock.ReceiveBufferSize);
                debugLog.printf("SOL_SOCKET/SO_SNDBUF = %d\n", sock.SendBufferSize);
                debugLog.printf("SOL_SOCKET/SO_RCVTIMEO = %d\n", sock.ReceiveTimeout);
                debugLog.printf("SOL_SOCKET/SO_SNDTIMEO = %d\n", sock.SendTimeout);
            }
            catch (Exception)
            {
            }
        }

        internal static void increaseBufferSize(Socket sock, Log debugLog)
        {
            // Increase the buffer size; the default (8192) is too easy to
            // overflow when the network latency is high.
            {
                int val = 1024 * 1024 * 2;
                try
                {
                    sock.ReceiveBufferSize = val;
                }
                catch (Exception)
                {
                    if (debugLog != null)
                    {
                        debugLog.printf("WARNING: Increasing socket receive buffer to %d failed.\n", val);
                        debugLog.println(socketErrorCode());
                    }
                }

                try
                {
                    sock.SendBufferSize = val;
                }
                catch (Exception)
                {
                    if (debugLog != null)
                    {
                        debugLog.printf("WARNING: Increasing socket send buffer to %d failed.\n", val);
                        debugLog.println(socketErrorCode());
                    }
                }
            }
        }

        internal bool readWaiting(Socket sock)
        {
            if (sock == null)
            {
                return false;
            }

            try
            {
                return sock.Poll(0, SelectMode.SelectRead);
            }
            catch (Exception)
            {
                if (debugLog != null)
                {
                    debugLog.println("ERROR: selectOneReadSocket returned SOCKET_ERROR in readWaiting().");
                    debugLog.println(socketErrorCode());
                }
                // Return true so that we'll force an error on read and close
                // the socket.
                return true;
            }
        }

        internal static bool selectOneWriteSocket(Socket sock)
        {
            try
            {
                return sock.Poll(0, SelectMode.SelectWrite);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /**
         Returns false if there was a problem initializing the network.
         */
        public bool init(Log log)
        {
            Debug.debugAssert(!initialized);
            debugLog = log;

            if (debugLog != null) { debugLog.section("Testing Network"); }

            Socket sock = null;
            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            catch (Exception)
            {
                sock = null;
            }

            if (debugLog != null) { debugLog.print("Open Socket                  "); }
            if (sock == null)
            {
                if (debugLog != null)
                {
                    debugLog.println("FAIL");
                    debugLog.println(socketErrorCode());
                }
                return false;
            }
            if (debugLog != null) { debugLog.println("Ok"); }

            bool ret = true;
            try
            {
                sock.EnableBroadcast = true;
            }
            catch (Exception)
            {
                ret = false;
            }

            if (debugLog != null) { debugLog.print("Enable UDP Broadcast         "); }
            if (!ret)
            {
                if (debugLog != null)
                {
                    debugLog.println("FAIL");
                    debugLog.println(socketErrorCode());
                }
                closesocket(ref sock);
                return false;
            }
            if (debugLog != null) { debugLog.println("Ok"); }

            if (debugLog != null) { debugLog.print("Testing UDP Broadcast        "); }
            try
            {
                byte[] x = new byte[4];
                sock.SendTo(x, 0, x.Length, SocketFlags.None,
                            toEndPoint(new NetAddress(0xFFFFFFFF, 23)));
            }
            catch (Exception)
            {
                ret = false;
            }

            if (debugLog != null)
            {
                if (!ret)
                {
                    debugLog.println("FAIL");
                    debugLog.println(socketErrorCode());
                }
                else
                {
                    debugLog.println("Ok");
                }
            }

            closesocket(ref sock);

            if (!ret)
            {
                return false;
            }

            if (debugLog != null) { debugLog.section(""); }
            initialized = true;

            return true;
        }

        public bool init()
        {
            return init(null);
        }

        /**
         Shuts down the network device.
         */
        public void cleanup()
        {
            Debug.debugAssert(initialized);

            if (debugLog != null) { debugLog.section("Network Cleanup"); }
            if (debugLog != null) { debugLog.println("Network cleaned up."); }
            initialized = false;
        }

        /**
         Prints a human-readable description of this machine
         to the text output stream.  Either argument may be NULL.
         */
        public void describeSystem(TextOutput t)
        {
            t.writeSymbol("Network");
            t.writeSymbol("{");
            t.writeNewline();
            t.pushIndent();
            t.popIndent();
            t.writeSymbol("}");
            t.writeNewline();
            t.writeNewline();
        }

        public void describeSystem(ref string s)
        {
            TextOutput t = new TextOutput(new TextOutput.Options());
            describeSystem(t);
            s = t.commitString();
        }

        /**
         Returns the name of this computer
         */
        public string localHostName()
        {
            try
            {
                return Dns.GetHostEntry(Dns.GetHostName()).HostName;
            }
            catch (Exception)
            {
                if (debugLog != null)
                {
                    debugLog.printf("Error while getting local host name\n");
                }
                return "localhost";
            }
        }

        /** There is often more than one address for the local host. This
            returns all of them. */
        public void localHostAddresses(Array<NetAddress> array)
        {
            array.resize(0);

            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(Dns.GetHostName());
            }
            catch (Exception)
            {
                if (debugLog != null)
                {
                    debugLog.printf("Error while getting local host address\n");
                }
                return;
            }

            for (int i = 0; i < addresses.Length; ++i)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    array.append(new NetAddress(addresses[i], 0));
                }
            }
        }

        /**
         If receivePort is specified and enableReceive is true, the conduit can 
         receive as well as send.
         @param receivePort host byte order
         */
        public ReferenceCountedPointer<LightweightConduit> createLightweightConduit(
            ushort receivePort, bool enableReceive, bool enableBroadcast)
        {
            return new ReferenceCountedPointer<LightweightConduit>(
                new LightweightConduit(this, receivePort, enableReceive, enableBroadcast));
        }

        public ReferenceCountedPointer<LightweightConduit> createLightweightConduit()
        {
            return createLightweightConduit(0, false, false);
        }

        /**
         Client invokes this to connect to a server.  The call blocks until the 
         conduit is opened.  The conduit will not be ok() if it fails.
         */
        public ReferenceCountedPointer<ReliableConduit> createReliableConduit(NetAddress address)
        {
            return new ReferenceCountedPointer<ReliableConduit>(
                new ReliableConduit(this, address));
        }

        /**
         Call this on the server side to create an object that listens for
         connections.
         */
        public ReferenceCountedPointer<NetListener> createListener(ushort port)
        {
            return new ReferenceCountedPointer<NetListener>(new NetListener(this, port));
        }
    }
}
