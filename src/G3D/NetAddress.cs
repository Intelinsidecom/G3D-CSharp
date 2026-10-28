/**
 @file NetAddress.h

 @maintainer Morgan McGuire, morgan@cs.brown.edu
 @created 2005-02-06
 @edited  2005-02-06
 */

using System;
using global::System.Net;

namespace G3D
{
    public struct NetAddress
    {
        /** Host byte order */
        private uint host;
        private ushort hostPort;
        private bool valid;

        /** Host byte order */
        private void init(uint host, ushort port)
        {
            if ((host != 0) || (port != 0))
            {
                this.host = host;
                hostPort = port;
                valid = true;
            }
            else
            {
                this.host = 0;
                hostPort = 0;
                valid = false;
            }
        }

        private void init(string hostname, ushort port)
        {
            uint addr = 0xFFFFFFFF;

            if (hostname != "")
            {
                IPAddress parsed;
                if (IPAddress.TryParse(hostname, out parsed) &&
                    parsed.AddressFamily == global::System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    byte[] bytes = parsed.GetAddressBytes();
                    addr = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) |
                           ((uint)bytes[2] << 8) | bytes[3];
                }
            }

            // The address wasn't in numeric form, resolve it
            if (addr == 0xFFFFFFFF)
            {
                // Get the IP address of the server and store it in host
                try
                {
                    IPAddress[] list = Dns.GetHostAddresses(hostname);
                    IPAddress found = null;
                    for (int i = 0; i < list.Length; ++i)
                    {
                        if (list[i].AddressFamily == global::System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            found = list[i];
                            break;
                        }
                    }

                    if (found == null)
                    {
                        init(0, 0);
                        return;
                    }

                    byte[] bytes = found.GetAddressBytes();
                    addr = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) |
                           ((uint)bytes[2] << 8) | bytes[3];
                }
                catch (Exception)
                {
                    init(0, 0);
                    return;
                }
            }

            init(addr, port);
        }

        internal NetAddress(IPAddress address, ushort port)
        {
            byte[] bytes = address.GetAddressBytes();
            uint addr = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) |
                        ((uint)bytes[2] << 8) | bytes[3];
            host = 0;
            hostPort = 0;
            valid = false;
            init(addr, port);
        }

        /**
         In host byte order
         */
        public NetAddress(uint host, ushort port = 0)
        {
            this.host = 0;
            hostPort = 0;
            valid = false;
            init(host, port);
        }

        /**
         Port is in host byte order.
         */
        public NetAddress(string hostname, ushort port)
        {
            host = 0;
            hostPort = 0;
            valid = false;
            init(hostname, port);
        }

        /**
        String must be in the form "hostname:port"
         */
        public NetAddress(string hostnameAndPort)
        {
            Array<string> part = StringUtils.stringSplit(hostnameAndPort, ':');

            Debug.debugAssert(part.length() == 2);
            host = 0;
            hostPort = 0;
            valid = false;
            init(part[0], (ushort)int.Parse(part[1]));
        }

        /**
         For use with a lightweight conduit.
         */
        public static NetAddress broadcastAddress(ushort port)
        {
            return new NetAddress(0xFFFFFFFF, port);
        }

        public void serialize(BinaryOutput b)
        {
            b.writeUInt32(ip());
            b.writeUInt16(port());
        }

        public void deserialize(BinaryInput b)
        {
            uint i;
            ushort p;

            i = b.readUInt32();
            p = b.readUInt16();

            host = 0;
            hostPort = 0;
            valid = false;
            init(i, p);
        }

        /** Returns true if this is not an illegal address. */
        public bool ok()
        {
            return valid;
        }

        /** Returns a value in host format */
        public uint ip()
        {
            return host;
        }

        public ushort port()
        {
            return hostPort;
        }

        public string ipString()
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(host >> 24);
            bytes[1] = (byte)(host >> 16);
            bytes[2] = (byte)(host >> 8);
            bytes[3] = (byte)host;
            return new IPAddress(bytes).ToString();
        }

        public string toString()
        {
            return ipString() + ":" + hostPort.ToString();
        }

        public static bool operator ==(NetAddress a, NetAddress b)
        {
            return (a.ip() == b.ip()) && (a.port() == b.port());
        }

        public static bool operator !=(NetAddress a, NetAddress b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NetAddress))
            {
                return false;
            }
            return this == (NetAddress)obj;
        }

        public override int GetHashCode()
        {
            return unchecked((int)(ip() + ((uint)port() << 16)));
        }
    }
}



