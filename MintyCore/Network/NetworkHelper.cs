using System;
using ENet;
using MintyCore.Utils;

namespace MintyCore.Network
{
    internal static class NetworkHelper
    {
        private static IntPtr EnetAllocate(IntPtr size)
        {
            return AllocationHandler.Malloc(size);
            ;
        }

        private static void EnetFree(IntPtr data)
        {
            AllocationHandler.Free(data);
        }

        private static void EnetNoMemory() => throw new OutOfMemoryException("Enet Called Out Of Memory");

        internal static void InitializeEnet()
        {
            Callbacks callbacks = new Callbacks(EnetAllocate, EnetFree, EnetNoMemory);

            if (Library.Initialize())
            {
                Logger.WriteLog("Enet successfully initialized", LogImportance.INFO, "Network");
            }
            else
            {
                throw new Exception("Enet wasnt initialized");
            }
        }

        internal static void FreePacket(Packet packet)
        {
            AllocationHandler.Free(packet.Data);
        }

        private static PacketFreeCallback _freePacketCallBack = FreePacket;

        internal static bool CheckConnected(PeerState state)
        {
            return (int)state > 0 && (int)state < 6;
        }

        internal static byte GetChannel(DeliveryMethod deliveryMethod)
        {
            switch (deliveryMethod)
            {
                case DeliveryMethod.Unreliable: return 0;
                case DeliveryMethod.Reliable: return 1;
                case DeliveryMethod.Unsequenced: return 2;
                case DeliveryMethod.UnreliableFragment: return 3;
                default: return 0;
            }
        }

        public static PacketFlags GetDeliveryPacketFlag(DeliveryMethod deliveryMethod)
        {
            return (PacketFlags)deliveryMethod;
        }
    }

    internal enum DisconnectReasons : uint
    {
        UNKNOWN = 0,
        PLAYER_DISCONNECT,
        KICK,
        BAN,
        SERVER_FULL,
        REJECT,
        SERVER_CLOSING
    }
}