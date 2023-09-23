namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class RealtimeNetworking : MonoBehaviour
    {

        #region Events
        public static event NoCallback OnDisconnectedFromServer;
        public static event ActionCallback OnConnectingToServerResult;
        public static event PacketCallback OnPacketReceived;
        public static event AuthCallback OnAuthentication;
        public static event CreateRoomCallback OnCreateRoom;
        public static event GetRoomsCallback OnGetRooms;
        public static event JoinRoomCallback OnJoinRoom;
        public static event LeaveRoomCallback OnLeaveRoom;
        public static event DeleteRoomCallback OnDeleteRoom;
        public static event RoomUpdateCallback OnRoomUpdated;
        public static event KickFromRoomCallback OnKickFromRoom;
        public static event RoomStatusCallback OnChangeRoomStatus;
        public static event StartRoomCallback OnRoomStartGame;
        #endregion

        #region Callbacks
        public delegate void ActionCallback(bool successful);
        public delegate void NoCallback();
        public delegate void PacketCallback(Packet packet);
        public delegate void AuthCallback(AuthenticationResponse response);
        public delegate void CreateRoomCallback(CreateRoomResponse response, Data.Room room);
        public delegate void GetRoomsCallback(GetRoomsResponse response, List<Data.Room> rooms);
        public delegate void JoinRoomCallback(JoinRoomResponse response, Data.Room room);
        public delegate void LeaveRoomCallback(LeaveRoomResponse response);
        public delegate void DeleteRoomCallback(DeleteRoomResponse response);
        public delegate void RoomUpdateCallback(RoomUpdateType response, Data.Room room, Data.Player player, Data.Player targetPlayer);
        public delegate void KickFromRoomCallback(KickFromRoomResponse response);
        public delegate void RoomStatusCallback(RoomStatusResponse response, bool ready);
        public delegate void StartRoomCallback(StartRoomResponse response);
        #endregion

        private bool _initialized = false;
        private bool _authenticated = false;
        private bool _connected = false;
        private long _accountID = -1; public static long accountID { get { return instance._accountID; } }

        private string _usernameKey = "username";
        private string _passwordKey = "password";

        private Scene _scene = default;
        private NetworkObject[] _objects = null;

        private string password
        {
            get
            {
                return PlayerPrefs.HasKey(_passwordKey) ? PlayerPrefs.GetString(_passwordKey) : "";
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    PlayerPrefs.DeleteKey(_passwordKey);
                }
                else
                {
                    PlayerPrefs.SetString(_passwordKey, value);
                }
            }
        }

        private string username
        {
            get
            {
                return PlayerPrefs.HasKey(_usernameKey) ? PlayerPrefs.GetString(_usernameKey) : "";
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    PlayerPrefs.DeleteKey(_usernameKey);
                }
                else
                {
                    PlayerPrefs.SetString(_usernameKey, value);
                }
            }
        }

        private static RealtimeNetworking _instance = null; public static RealtimeNetworking instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RealtimeNetworking>();
                    if (_instance == null)
                    {
                        _instance = Client.instance.gameObject.AddComponent<RealtimeNetworking>();
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            _scene = SceneManager.GetActiveScene();
            _objects = FindObjectsOfType<NetworkObject>();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _scene = scene;
            _objects = FindObjectsOfType<NetworkObject>();
        }

        public static void Connect()
        {
            Client.instance.ConnectToServer();
        }

        public void _Connection(bool result)
        {
            _connected = result;
            if (OnConnectingToServerResult != null)
            {
                OnConnectingToServerResult.Invoke(result);
            }
        }

        public void _Disconnected()
        {
            _connected = false;
            _accountID = -1;
            _authenticated = false;
            if (OnDisconnectedFromServer != null)
            {
                OnDisconnectedFromServer.Invoke();
            }
        }

        public void _ReceivePacket(Packet packet)
        {
            if (OnPacketReceived != null)
            {
                OnPacketReceived.Invoke(packet);
            }
        }

        private enum InternalID
        {
            AUTH = 1, GET_ROOMS = 2, CREATE_ROOM = 3, JOIN_ROOM = 4, LEAVE_ROOM = 5, DELETE_ROOM = 6, ROOM_UPDATED = 7, KICK_FROM_ROOM = 8, STATUS_IN_ROOM = 9, START_ROOM = 10, SYNC_PLAYER = 11
        }

        private static void SendTCPDataInternal(Packet _packet)
        {
            if (_packet == null)
            {
                return;
            }
            _packet.SetID((int)Packet.ID.INTERNAL);
            _packet.WriteLength();
            Client.instance.tcp.SendData(_packet);
        }

        private static void SendUDPDataInternal(Packet _packet)
        {
            if (_packet == null)
            {
                return;
            }
            _packet.SetID((int)Packet.ID.INTERNAL);
            _packet.WriteLength();
            Client.instance.udp.SendData(_packet);
        }

        public void _ReceiveInternal(Packet packet)
        {
            int id = packet.ReadInt();
            switch ((InternalID)id)
            {
                case InternalID.AUTH:
                    int authRes = packet.ReadInt();
                    long authID = packet.ReadLong();
                    int authBanned = packet.ReadInt();
                    string authUser = packet.ReadString();
                    string authPass = packet.ReadString();
                    packet.Dispose();
                    if(authRes == (int)AuthenticationResponse.SUCCESSFULL)
                    {
                        _accountID = authID;
                        _authenticated = true;
                        username = authUser;
                        password = authPass;
                    }
                    if (OnAuthentication != null)
                    {
                        OnAuthentication.Invoke((AuthenticationResponse)authRes);
                    }
                    break;
                case InternalID.CREATE_ROOM:
                    int crRoomRes = packet.ReadInt();
                    Data.Room crRoom = null;
                    if (crRoomRes == (int)CreateRoomResponse.SUCCESSFULL)
                    {
                        int crRoomBytesLen = packet.ReadInt();
                        byte[] crRoomBytes = packet.ReadBytes(crRoomBytesLen);
                        crRoom = Tools.Desrialize<Data.Room>(Tools.Decompress(crRoomBytes));
                    }
                    packet.Dispose();
                    if (OnCreateRoom != null)
                    {
                        OnCreateRoom.Invoke((CreateRoomResponse)crRoomRes, crRoom);
                    }
                    break;
                case InternalID.GET_ROOMS:
                    int gtRoomsBytesLen = packet.ReadInt();
                    byte[] gtRoomsBytes = packet.ReadBytes(gtRoomsBytesLen);
                    List<Data.Room> gtRooms = Tools.Desrialize< List<Data.Room>>(Tools.Decompress(gtRoomsBytes));
                    packet.Dispose();
                    if (OnGetRooms != null)
                    {
                        OnGetRooms.Invoke(GetRoomsResponse.SUCCESSFULL, gtRooms);
                    }
                    break;
                case InternalID.JOIN_ROOM:
                    int jnRoomRes = packet.ReadInt();
                    Data.Room jnRoom = null;
                    if (jnRoomRes == (int)JoinRoomResponse.SUCCESSFULL)
                    {
                        int jnRoomBytesLen = packet.ReadInt();
                        byte[] jnRoomBytes = packet.ReadBytes(jnRoomBytesLen);
                        jnRoom = Tools.Desrialize<Data.Room>(Tools.Decompress(jnRoomBytes));
                    }
                    packet.Dispose();
                    if (OnJoinRoom != null)
                    {
                        OnJoinRoom.Invoke((JoinRoomResponse)jnRoomRes, jnRoom);
                    }
                    break;
                case InternalID.LEAVE_ROOM:
                    int lvRoomRes = packet.ReadInt();
                    packet.Dispose();
                    if (OnLeaveRoom != null)
                    {
                        OnLeaveRoom.Invoke((LeaveRoomResponse)lvRoomRes);
                    }
                    break;
                case InternalID.DELETE_ROOM:
                    int deRoomRes = packet.ReadInt();
                    packet.Dispose();
                    if (OnDeleteRoom != null)
                    {
                        OnDeleteRoom.Invoke((DeleteRoomResponse)deRoomRes);
                    }
                    break;
                case InternalID.ROOM_UPDATED:
                    int upRoomTyp = packet.ReadInt();
                    Data.Room upRoom = null;
                    Data.Player upPlayer = null;
                    Data.Player upTargetPlayer = null;

                    int upBytesLen = packet.ReadInt();
                    byte[] upBytes = packet.ReadBytes(upBytesLen);
                    upRoom = Tools.Desrialize<Data.Room>(Tools.Decompress(upBytes));
                    upBytesLen = packet.ReadInt();
                    upBytes = packet.ReadBytes(upBytesLen);
                    upPlayer = Tools.Desrialize<Data.Player>(Tools.Decompress(upBytes));
                    if (upRoomTyp == (int)RoomUpdateType.PLAYER_KICKED)
                    {
                        upBytesLen = packet.ReadInt();
                        upBytes = packet.ReadBytes(upBytesLen);
                        upTargetPlayer = Tools.Desrialize<Data.Player>(Tools.Decompress(upBytes));
                    }

                    packet.Dispose();
                    if (OnRoomUpdated != null)
                    {
                        OnRoomUpdated.Invoke((RoomUpdateType)upRoomTyp, upRoom, upPlayer, upTargetPlayer);
                    }
                    break;
                case InternalID.KICK_FROM_ROOM:
                    int kcRoomRes = packet.ReadInt();
                    packet.Dispose();
                    if (OnKickFromRoom != null)
                    {
                        OnKickFromRoom.Invoke((KickFromRoomResponse)kcRoomRes);
                    }
                    break;
                case InternalID.STATUS_IN_ROOM:
                    int csRoomRes = packet.ReadInt();
                    bool csRoomRdy = packet.ReadBool();
                    packet.Dispose();
                    if (OnChangeRoomStatus != null)
                    {
                        OnChangeRoomStatus.Invoke((RoomStatusResponse)csRoomRes, csRoomRdy);
                    }
                    break;
                case InternalID.START_ROOM:
                    int stRoomRes = packet.ReadInt();
                    packet.Dispose();
                    if (OnRoomStartGame != null)
                    {
                        OnRoomStartGame.Invoke((StartRoomResponse)stRoomRes);
                    }
                    break;
            }
        }

        public enum AuthenticationResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, ALREADY_AUTHENTICATED = 4, USERNAME_TAKEN = 5, WRONG_CREDS = 6, BANNED = 7, INVALID_INPUT = 8
        }

        public enum CreateRoomResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, ALREADY_IN_ANOTHER_ROOM = 4, INVALID_SCENE = 5
        }

        public enum GetRoomsResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3
        }

        public enum JoinRoomResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, ALREADY_IN_ANOTHER_ROOM = 4, WRONG_PASSWORD = 5, AT_FULL_CAPACITY = 6, ALREADY_GAME_STARTED = 7
        }

        public enum LeaveRoomResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, NOT_IN_ANY_ROOM = 4
        }

        public enum DeleteRoomResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, NOT_IN_ANY_ROOM = 4, DONT_HAVE_PERMISSION = 5
        }

        public enum RoomUpdateType
        {
            UNKNOWN = 0, ROOM_DELETED = 1, PLAYER_JOINED = 2, PLAYER_LEFT = 3, PLAYER_STATUS_CHANGED = 4, PLAYER_KICKED = 5, GAME_STARTED = 6
        }

        public enum KickFromRoomResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, NOT_IN_ANY_ROOM = 4, DONT_HAVE_PERMISSION = 5, TARGET_NOT_FOUND = 6
        }

        public enum RoomStatusResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, NOT_IN_ANY_ROOM = 4, ALREADY_IN_THAT_STATUS = 5
        }

        public enum StartRoomResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, NOT_IN_ANY_ROOM = 4, DONT_HAVE_PERMISSION = 5, ALREADY_STARTED = 6
        }

        public static void Authenticate()
        {
            _Authenticate(instance.username, instance.password);
        }

        public static void Authenticate(string username, string password, bool createIfNotExist)
        {
            if (string.IsNullOrEmpty(username))
            {
                if (OnAuthentication != null)
                {
                    OnAuthentication.Invoke(AuthenticationResponse.INVALID_INPUT);
                }
            }
            else if (string.IsNullOrEmpty(password))
            {
                if (OnAuthentication != null)
                {
                    OnAuthentication.Invoke(AuthenticationResponse.INVALID_INPUT);
                }
            }
            else
            {
                _Authenticate(username, Tools.EncrypteToMD5(password), createIfNotExist);
            }
        }

        private static void _Authenticate(string username, string password, bool createIfNotExist = true)
        {
            if (!instance._connected)
            {
                if (OnAuthentication != null)
                {
                    OnAuthentication.Invoke(AuthenticationResponse.NOT_CONNECTED);
                }
            }
            else if (instance._authenticated)
            {
                if (OnAuthentication != null)
                {
                    OnAuthentication.Invoke(AuthenticationResponse.ALREADY_AUTHENTICATED);
                }
            }
            else
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.AUTH);
                packet.Write(SystemInfo.deviceUniqueIdentifier);
                packet.Write(createIfNotExist);
                packet.Write(username);
                packet.Write(password);
                SendTCPDataInternal(packet);
            }
        }

        public static void CreateRoom(int sceneIndex)
        {
            CreateRoom(sceneIndex, "");
        }

        public static void CreateRoom(int sceneIndex, string password)
        {
            if (!instance._connected)
            {
                if (OnCreateRoom != null)
                {
                    OnCreateRoom.Invoke(CreateRoomResponse.NOT_CONNECTED, null);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnCreateRoom != null)
                {
                    OnCreateRoom.Invoke(CreateRoomResponse.NOT_AUTHENTICATED, null);
                }
            }
            else
            {
                if (sceneIndex >= SceneManager.sceneCountInBuildSettings)
                {
                    if (OnCreateRoom != null)
                    {
                        OnCreateRoom.Invoke(CreateRoomResponse.INVALID_SCENE, null);
                    }
                    return;
                }
                if (!string.IsNullOrEmpty(password))
                {
                    password = Tools.EncrypteToMD5(password);
                }
                Packet packet = new Packet();
                packet.Write((int)InternalID.CREATE_ROOM);
                packet.Write(password);
                packet.Write(sceneIndex);
                SendTCPDataInternal(packet);
            }
        }

        public static void GetRooms()
        {
            if (!instance._connected)
            {
                if (OnGetRooms != null)
                {
                    OnGetRooms.Invoke(GetRoomsResponse.NOT_CONNECTED, null);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnGetRooms != null)
                {
                    OnGetRooms.Invoke(GetRoomsResponse.NOT_AUTHENTICATED, null);
                }
            }
            else
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.GET_ROOMS);
                SendTCPDataInternal(packet);
            }
        }

        public static void JoinRoom(string roomID)
        {
            JoinRoom(roomID, "");
        }

        public static void JoinRoom(string roomID, string password)
        {
            if (!instance._connected)
            {
                if (OnJoinRoom != null)
                {
                    OnJoinRoom.Invoke(JoinRoomResponse.NOT_CONNECTED, null);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnJoinRoom != null)
                {
                    OnJoinRoom.Invoke(JoinRoomResponse.NOT_AUTHENTICATED, null);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(password))
                {
                    password = Tools.EncrypteToMD5(password);
                }
                Packet packet = new Packet();
                packet.Write((int)InternalID.JOIN_ROOM);
                packet.Write(roomID);
                packet.Write(password);
                SendTCPDataInternal(packet);
            }
        }

        public static void LeaveRoom()
        {
            if (!instance._connected)
            {
                if (OnLeaveRoom != null)
                {
                    OnLeaveRoom.Invoke(LeaveRoomResponse.NOT_CONNECTED);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnLeaveRoom != null)
                {
                    OnLeaveRoom.Invoke(LeaveRoomResponse.NOT_AUTHENTICATED);
                }
            }
            else
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.LEAVE_ROOM);
                SendTCPDataInternal(packet);
            }
        }

        public static void DeleteRoom()
        {
            if (!instance._connected)
            {
                if (OnDeleteRoom != null)
                {
                    OnDeleteRoom.Invoke(DeleteRoomResponse.NOT_CONNECTED);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnDeleteRoom != null)
                {
                    OnDeleteRoom.Invoke(DeleteRoomResponse.NOT_AUTHENTICATED);
                }
            }
            else
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.DELETE_ROOM);
                SendTCPDataInternal(packet);
            }
        }

        public static void KickFromRoom(long targetID)
        {
            if (!instance._connected)
            {
                if (OnKickFromRoom != null)
                {
                    OnKickFromRoom.Invoke(KickFromRoomResponse.NOT_CONNECTED);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnKickFromRoom != null)
                {
                    OnKickFromRoom.Invoke(KickFromRoomResponse.NOT_AUTHENTICATED);
                }
            }
            else
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.KICK_FROM_ROOM);
                packet.Write(targetID);
                SendTCPDataInternal(packet);
            }
        }

        public static void ChangeRoomStatus(bool ready)
        {
            if (!instance._connected)
            {
                if (OnChangeRoomStatus != null)
                {
                    OnChangeRoomStatus.Invoke(RoomStatusResponse.NOT_CONNECTED, false);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnChangeRoomStatus != null)
                {
                    OnChangeRoomStatus.Invoke(RoomStatusResponse.NOT_AUTHENTICATED, false);
                }
            }
            else
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.STATUS_IN_ROOM);
                packet.Write(ready);
                SendTCPDataInternal(packet);
            }
        }

        public static void StartRoomGame()
        {
            if (!instance._connected)
            {
                if (OnRoomStartGame != null)
                {
                    OnRoomStartGame.Invoke(StartRoomResponse.NOT_CONNECTED);
                }
            }
            else if (!instance._authenticated)
            {
                if (OnRoomStartGame != null)
                {
                    OnRoomStartGame.Invoke(StartRoomResponse.NOT_AUTHENTICATED);
                }
            }
            else
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.START_ROOM);
                SendTCPDataInternal(packet);
            }
        }

    }
}