namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class Demo : MonoBehaviour
    {

        [SerializeField] private Text textLog = null;
        [SerializeField] private Button buttonConnect = null;
        [SerializeField] private Button buttonAuth = null;

        private void Start()
        {
            // Creating event listeners
            RealtimeNetworking.OnDisconnectedFromServer += Disconnected;
            RealtimeNetworking.OnConnectingToServerResult += ConnectResult;
            RealtimeNetworking.OnPacketReceived += PacketReceived;
            RealtimeNetworking.OnAuthentication += RealtimeNetworking_OnAuthenticationResponse;        

            buttonConnect.onClick.AddListener(ConnectClicked);
            buttonAuth.onClick.AddListener(AuthClicked);
            buttonConnect.interactable = true;
            buttonAuth.interactable = false;
            buttonConnect.gameObject.SetActive(true);
            buttonAuth.gameObject.SetActive(true);

            textLog.fontSize = (int)(Screen.height * 0.05f);
        }

        private void OnDestroy()
        {
            // Remove event listeners
            RealtimeNetworking.OnDisconnectedFromServer -= Disconnected;
            RealtimeNetworking.OnConnectingToServerResult -= ConnectResult;
            RealtimeNetworking.OnPacketReceived -= PacketReceived;
            RealtimeNetworking.OnAuthentication -= RealtimeNetworking_OnAuthenticationResponse;
        }

        private void ConnectClicked()
        {
            // Try to connect the server
            buttonConnect.interactable = false;
            RealtimeNetworking.Connect();
        }

        private void AuthClicked()
        {
            // Try to authenticate the player
            buttonAuth.interactable = false;
            RealtimeNetworking.Authenticate();
        }

        private void Disconnected()
        {
            buttonConnect.interactable = true;
            buttonAuth.interactable = false;
            buttonConnect.gameObject.SetActive(true);
            buttonAuth.gameObject.SetActive(true);
            textLog.text = "Disconnected from server.";
        }

        private void ConnectResult(bool successful)
        {
            if (successful)
            {
                buttonAuth.interactable = true;
                textLog.text = "Connected to server successfully.";
            }
            else
            {
                buttonConnect.interactable = true;
                textLog.text = "Failed to connect the server.";
            }
        }

        private void PacketReceived(Packet packet)
        {
            textLog.text = "Packet received from the server.";
        }

        private void RealtimeNetworking_OnAuthenticationResponse(RealtimeNetworking.AuthenticationResponse response)
        {
            if(response == RealtimeNetworking.AuthenticationResponse.SUCCESSFULL)
            {
                textLog.text = "Authenticated the player successfully. Account id: " + RealtimeNetworking.accountID.ToString();
            }
            else
            {
                buttonAuth.interactable = true;
                textLog.text = "Failed to authenticate the player. Code: " + response;
            }
        }

    }
}
