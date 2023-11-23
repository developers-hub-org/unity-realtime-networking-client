namespace DevelopersHub.RealtimeNetworking.Client.NetcodeForGameObjects
{/*
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;

    public class NetcodeDemo : MonoBehaviour
    {

        [SerializeField] GameObject playerPrefab = null;
        [SerializeField] private Role _role = Role.Client; public Role role { get { return _role; } }
        [SerializeField] private float destroyServerAfterSecondsIfNoClientConnected = 300;
        [SerializeField] private float destroyServerAfterSecondsWithoutAnyClient = 10;
        private float timer = 0;
        private int clientsCount = 0;
        private bool atLeastOneClientConnected = false;
        private bool closingServer = false;
        private NetworkManagerHook _singleton = null;
        public static int port = 7777;

        public enum Role
        {
            Server = 1, Client = 2
        }

        private void Awake()
        {
            _singleton = this;
            RealtimeNetworking.OnNetcodeServerReady += OnNetcodeServerReady;
        }

        private void OnNetcodeServerReady(int serverPort)
        {
            if (_role == Role.Client)
            {
                port = serverPort;
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.ConnectionData.Address = DevelopersHub.RealtimeNetworking.Client.Client.instance.settings.ip;
                transport.ConnectionData.Port = (ushort)serverPort;
                NetworkManager.Singleton.StartClient();
            }
        }

        private void DisconnectClient()
        {
            if (_role == Role.Client)
            {
                NetworkManager.Singleton.StopClient();
            }
        }

        private void Start()
        {
            if (_role == Role.Server)
            {
                port = Tools.FindFreeTcpPort();
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.ConnectionData.Port = (ushort)port;

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                NetworkManager.Singleton.OnServerStopped += OnServerStopped;

                NetworkManager.Singleton.StartServer();
            }
        }

        private void OnServerStarted()
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            RealtimeNetworking.NetcodeServerIsReady(port);
        }

        private void OnServerStopped(bool obj)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }

        private void OnClientDisconnect(ulong obj)
        {
            clientsCount--;
        }

        private void OnClientConnected(ulong clientId)
        {
            Vector3 position = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            Quaternion rotation = Quaternion.identity;
            GameObject controller = Instantiate(playerPrefab, position, rotation);
            controller.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            clientsCount++;
            atLeastOneClientConnected = true;
        }

        private void Update()
        {
            if (_role == Role.Server)
            {
                if (closingServer)
                {
                    return;
                }
                if (atLeastOneClientConnected)
                {
                    if (clientsCount > 0)
                    {
                        timer = 0;
                    }
                    else
                    {
                        timer += Time.deltaTime;
                        if (timer >= destroyServerAfterSecondsWithoutAnyClient)
                        {
                            CloseServer();
                        }
                    }
                }
                else
                {
                    destroyServerAfterSecondsIfNoClientConnected -= Time.deltaTime;
                    if (destroyServerAfterSecondsIfNoClientConnected <= 0)
                    {
                        CloseServer();
                    }
                }
            }
        }

        private void CloseServer()
        {
            if (_role == Role.Server)
            {
                closingServer = true;
                Application.Quit();
            }
        }

    }*/
}