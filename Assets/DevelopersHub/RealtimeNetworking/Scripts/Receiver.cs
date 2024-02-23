namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Net;
    using UnityEngine;

    public class Receiver : MonoBehaviour
    {

        public static void Initialization(Packet packet)
        {
            int id = packet.ReadInt();
            string receiveToken = packet.ReadString();
            string sendToken = Tools.GenerateToken();
            using (Packet response = new Packet((int)Packet.ID.INITIALIZATION))
            {
                response.Write(sendToken);
                response.WriteLength();
                Client.instance.tcp.SendData(response);
            }
            Client.instance.ConnectionResponse(true, id, sendToken, receiveToken);
            Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
        }

        public static void ReceiveCustom(Packet packet)
        {
            if (packet != null)
            {
                RealtimeNetworking.instance._ReceivePacket(packet);
            }
        }

        public static void ReceiveInternal(Packet packet)
        {
            if (packet != null)
            {
                RealtimeNetworking.instance._ReceiveInternal(packet);
            }
        }

    }
}