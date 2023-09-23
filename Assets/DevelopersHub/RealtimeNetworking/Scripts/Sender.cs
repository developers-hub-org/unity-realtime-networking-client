namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Sender : MonoBehaviour
    {

        #region Core
        private static void SendTCPData(Packet _packet)
        {
            _packet.WriteLength();
            Client.instance.tcp.SendData(_packet);
        }

        private static void SendUDPData(Packet _packet)
        {
            _packet.WriteLength();
            Client.instance.udp.SendData(_packet);
        }
        #endregion

        #region TCP
        public static void TCP_Send(Packet packet)
        {
            if(packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendTCPData(packet);
            }
        }
        #endregion
        
        #region UDP
        public static void UDP_Send(Packet packet)
        {
            if (packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendUDPData(packet);
            }
        }
        #endregion

    }
}