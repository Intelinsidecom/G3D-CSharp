/**
  @file Network_Demo/ServerAd.cs

  Port of Network_Demo/ServerAd.h and ServerAd.cpp.

  An advertisement used by servers to tell potential clients
  about themselves.
 */

using System;
using G3D;

namespace Network_Demo
{
    public class ServerAd : DiscoveryAdvertisement
    {
        /** Name of the hosting server */
        public string name;

        // Can add other information here (e.g. current score,
        // number of players) to broadcast other information
        // about a server.

        public ServerAd()
        {
            name = "";
        }

        public override void serialize(BinaryOutput b)
        {
            base.serialize(b);
            b.writeString(name);
        }

        public override void deserialize(BinaryInput b)
        {
            base.deserialize(b);
            name = b.readString();
        }
    }
}

namespace Network_Demo
{
    public enum MessageType
    {
        NO_MSG = 0,
        SignOnMessage_MSG = 2000,
        CreateEntityMessage_MSG = 2001,
        EntityStateMessage_MSG = 2002,
    }
}

namespace Network_Demo
{
    /** The port the game traffic runs on; matchmaking uses the discovery ports. */
    public static class GamePort
    {
        public const int GAME_PORT = 1424;
    }
}
