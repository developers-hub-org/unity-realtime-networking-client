namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class Demo_01_Manager : MonoBehaviour
    {

        [SerializeField] private RectTransform panelMenu = null;
        [SerializeField] private RectTransform panelRooms = null;
        [SerializeField] private RectTransform panelLobby = null;
        [SerializeField] private Text textLog = null;
        [SerializeField] private Text textLobbyError = null;
        [SerializeField] private Button buttonConnect = null;
        [SerializeField] private Button buttonAuth = null;
        [SerializeField] private Button buttonCreate = null;
        [SerializeField] private Button buttonRefresh = null;
        [SerializeField] private Button buttonLeave = null;
        [SerializeField] private Button buttonStart = null;
        [SerializeField] private Demo_01_Room roomPrefab = null;
        [SerializeField] private RectTransform roomsContainer = null;
        [SerializeField] private Demo_01_LobbyPlayer playerPrefab = null;
        [SerializeField] private RectTransform playerContainer = null;
        [SerializeField] private string gameSceneName = "Demo_01_2_Game";

        private List<Demo_01_Room> _rooms = new List<Demo_01_Room>();
        private List<Demo_01_LobbyPlayer> _players = new List<Demo_01_LobbyPlayer>();
        
        public static bool isSpawnedPlayer = false;

        private void Start()
        {
            // Creating event listeners
            RealtimeNetworking.OnDisconnectedFromServer += Disconnected;
            RealtimeNetworking.OnConnectingToServerResult += ConnectResult;
            RealtimeNetworking.OnPacketReceived += PacketReceived;
            RealtimeNetworking.OnAuthentication += RealtimeNetworking_OnAuthenticationResponse;
            RealtimeNetworking.OnGetRooms += OnGetRooms;
            RealtimeNetworking.OnCreateRoom += OnCreateRoom;
            RealtimeNetworking.OnJoinRoom += OnJoinRoom;
            RealtimeNetworking.OnRoomUpdated += OnRoomUpdated;
            RealtimeNetworking.OnGameStarted += OnGameStarted;

            buttonConnect.onClick.AddListener(ConnectClicked);
            buttonAuth.onClick.AddListener(AuthClicked);
            buttonCreate.onClick.AddListener(CreateRoomClicked);
            buttonLeave.onClick.AddListener(LeaveRoomClicked);
            buttonStart.onClick.AddListener(StartRoomClicked);
            buttonRefresh.onClick.AddListener(OpenRooms);
            buttonConnect.interactable = true;
            buttonAuth.interactable = false;
            panelMenu.gameObject.SetActive(true);
            panelRooms.gameObject.SetActive(false);
            panelLobby.gameObject.SetActive(false);
            textLog.fontSize = (int)(Screen.height * 0.05f);

            if(RealtimeNetworking.isAuthenticated)
            {
                OpenRooms();
            }
            else if(RealtimeNetworking.isConnected)
            {
                buttonConnect.interactable = false;
                buttonAuth.interactable = true;
            }
        }

        private void OnGameStarted()
        {
            isSpawnedPlayer = false;
            LoadGameScene();
        }

        private void OnDestroy()
        {
            // Remove event listeners
            RealtimeNetworking.OnDisconnectedFromServer -= Disconnected;
            RealtimeNetworking.OnConnectingToServerResult -= ConnectResult;
            RealtimeNetworking.OnPacketReceived -= PacketReceived;
            RealtimeNetworking.OnAuthentication -= RealtimeNetworking_OnAuthenticationResponse;
            RealtimeNetworking.OnGetRooms -= OnGetRooms;
            RealtimeNetworking.OnCreateRoom -= OnCreateRoom;
            RealtimeNetworking.OnJoinRoom -= OnJoinRoom;
            RealtimeNetworking.OnRoomUpdated -= OnRoomUpdated;
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
            panelMenu.gameObject.SetActive(true);
            panelRooms.gameObject.SetActive(false);
            panelLobby.gameObject.SetActive(false);
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
                OpenRooms();
            }
            else
            {
                buttonAuth.interactable = true;
                textLog.text = "Failed to authenticate the player. Code: " + response;
            }
        }

        private void OpenRooms()
        {
            ClearRoomsList();
            buttonRefresh.interactable = false;
            buttonCreate.interactable = false;
            panelMenu.gameObject.SetActive(false);
            panelRooms.gameObject.SetActive(true);
            panelLobby.gameObject.SetActive(false);
            RealtimeNetworking.GetRooms();
        }

        private void OnGetRooms(RealtimeNetworking.GetRoomsResponse response, List<Data.Room> rooms)
        {
            if(response == RealtimeNetworking.GetRoomsResponse.SUCCESSFULL && rooms != null)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    Demo_01_Room room = Instantiate(roomPrefab, roomsContainer);
                    room.Initialize(rooms[i]);
                    _rooms.Add(room);
                }
            }
            buttonCreate.interactable = true;
            buttonRefresh.interactable = true;
        }

        private void CreateRoomClicked()
        {
            buttonCreate.interactable = false;
            RealtimeNetworking.CreateRoom(0, 0, 0);
        }

        private void ClearRoomsList()
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i] != null)
                {
                    Destroy(_rooms[i].gameObject);
                }
            }
            _rooms.Clear();
        }

        private void ClearLobbyList()
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i] != null)
                {
                    Destroy(_players[i].gameObject);
                }
            }
            _players.Clear();
        }

        private void LeaveRoomClicked()
        {
            buttonLeave.interactable = false;
            RealtimeNetworking.LeaveRoom();
        }

        public void JoinRoom(string id)
        {
            RealtimeNetworking.JoinRoom(id, 0);
        }

        private void OnJoinRoom(RealtimeNetworking.JoinRoomResponse response, Data.Room room)
        {
            if(response != RealtimeNetworking.JoinRoomResponse.SUCCESSFULL)
            {
                OpenRooms();
            }
        }

        private void OnCreateRoom(RealtimeNetworking.CreateRoomResponse response, Data.Room room)
        {
            if(response == RealtimeNetworking.CreateRoomResponse.SUCCESSFULL)
            {
                OpenLobby(room);
            }
            else
            {
                buttonCreate.interactable = true;
            }
        }

        private void OpenLobby(Data.Room room)
        {
            textLobbyError.text = "";
            ClearLobbyList();
            buttonLeave.interactable = true;
            for (int i = 0; i < room.players.Count; i++)
            {
                Demo_01_LobbyPlayer player = Instantiate(playerPrefab, playerContainer);
                player.Initialize(room.players[i], room.hostID == RealtimeNetworking.accountID && RealtimeNetworking.accountID != room.players[i].id);
                _players.Add(player);
            }
            if (room.hostID == RealtimeNetworking.accountID)
            {
                buttonStart.gameObject.SetActive(true);
                CheckCanStart();
            }
            else
            {
                buttonStart.gameObject.SetActive(false);
            }
            panelMenu.gameObject.SetActive(false);
            panelRooms.gameObject.SetActive(false);
            panelLobby.gameObject.SetActive(true);
        }

        public void KickPlayer(long id)
        {
            RealtimeNetworking.KickFromRoom(id);
        }

        public void ChangePlayerStatus(bool status)
        {
            RealtimeNetworking.ChangeRoomStatus(status);
        }

        private void OnRoomUpdated(RealtimeNetworking.RoomUpdateType response, Data.Room room, Data.Player player, Data.Player targetPlayer)
        {
            if (response == RealtimeNetworking.RoomUpdateType.ROOM_DELETED)
            {
                OpenRooms();
            }
            else if (response == RealtimeNetworking.RoomUpdateType.PLAYER_KICKED)
            {
                if (targetPlayer.id == RealtimeNetworking.accountID)
                {
                    OpenRooms();
                }
                else
                {
                    for (int i = 0; i < _players.Count; i++)
                    {
                        if (_players[i] != null && _players[i].id == targetPlayer.id)
                        {
                            Destroy(_players[i].gameObject);
                            _players.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            else if (response == RealtimeNetworking.RoomUpdateType.PLAYER_LEFT)
            {
                if (player.id != RealtimeNetworking.accountID)
                {
                    for (int i = 0; i < _players.Count; i++)
                    {
                        if (_players[i] != null && _players[i].id == player.id)
                        {
                            Destroy(_players[i].gameObject);
                            _players.RemoveAt(i);
                            break;
                        }
                    }
                }
                else
                {
                    OpenRooms();
                }
            }
            else if (response == RealtimeNetworking.RoomUpdateType.PLAYER_STATUS_CHANGED)
            {
                for (int i = 0; i < _players.Count; i++)
                {
                    if (_players[i] != null && _players[i].id == player.id)
                    {
                        _players[i].SetStatus(player.ready);
                        break;
                    }
                }
            }
            else if (response == RealtimeNetworking.RoomUpdateType.PLAYER_JOINED)
            {
                if(room.hostID != RealtimeNetworking.accountID)
                {
                    OpenLobby(room);
                }
                else
                {
                    Demo_01_LobbyPlayer p = Instantiate(playerPrefab, playerContainer);
                    p.Initialize(player, room.hostID == RealtimeNetworking.accountID && RealtimeNetworking.accountID != player.id);
                    _players.Add(p);
                }
            }
            if (room.hostID == RealtimeNetworking.accountID && response != RealtimeNetworking.RoomUpdateType.ROOM_DELETED)
            {
                CheckCanStart();
            }
        }

        private void StartRoomClicked()
        {
            RealtimeNetworking.StartRoomGame();
        }

        private void CheckCanStart()
        {
            bool ready = true;
            for (int i = 0; i < _players.Count; i++)
            {
                if (!_players[i].ready)
                {
                    ready = false;
                    break;
                }
            }
            buttonStart.interactable = ready;
        }

        private void LoadGameScene()
        {
            if(SceneUtility.GetBuildIndexByScenePath(gameSceneName) >= 0)
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                textLobbyError.text = gameSceneName + " scene is not in the build list !!!";
            }
        }

    }
}
