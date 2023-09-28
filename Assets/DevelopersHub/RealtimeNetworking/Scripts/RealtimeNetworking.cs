namespace DevelopersHub.RealtimeNetworking.Client
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xml.Linq;
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
        private bool _authenticated = false; public static bool isAuthenticated { get { return instance._authenticated; } }
        private bool _connected = false; public static bool isConnected { get { return instance._connected; } }
        private bool _inGame = false; public static bool isGameStarted { get { return instance._inGame; } }
        private long _accountID = -1; public static long accountID { get { return instance._accountID; } }
        private long _sceneHostID = -1; public static bool isSceneHost { get { return instance._sceneHostID >= 0 && instance._sceneHostID == instance._accountID; } }

        private string _usernameKey = "username";
        private string _passwordKey = "password";

        private Scene _scene = default; public static int sceneIndex { get { return instance._scene.buildIndex; } }
        private List<NetworkObject> _sceneObjects = new List<NetworkObject>();
        private List<ObjectsData> _globalObjects = new List<ObjectsData>();
        private HashSet<long> _disconnected = new HashSet<long>();
        private Data.Room _gameRoom = null;
        private int _ticksPerSecond = 10;
        private int _ticksCalled = 0;
        private float _ticksTimer = 0;

        private class ObjectsData
        {
            public int sceneIndex = 0;
            public List<SceneObjectsData> accounts = new List<SceneObjectsData>();
        }

        private class SceneObjectsData
        {
            public long accountID = -1;
            public List<NetworkObject.Data> objects = new List<NetworkObject.Data>();
        }

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
            _sceneObjects.Clear();
            NetworkObject[] obj = FindObjectsOfType<NetworkObject>(true);
            if(obj != null)
            {
                _sceneObjects.AddRange(obj);
            }
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

        private void Update()
        {
            if(_connected && _authenticated && _inGame)
            {
                _ticksTimer += Time.deltaTime;
                int ticks = Mathf.FloorToInt(_ticksTimer * _ticksPerSecond);
                if(ticks > _ticksCalled)
                {
                    _ticksCalled++;
                    Tick();
                }
                if (_ticksCalled >= _ticksPerSecond)
                {
                    _ticksCalled = 0;
                    _ticksTimer = 0;
                }
            }
        }

        private void Tick()
        {
            if (_sceneObjects != null && _sceneObjects.Count > 0)
            {
                List<NetworkObject.Data> syncObjects = new List<NetworkObject.Data>();
                List<NetworkObject.Data> unownedSyncObjects = new List<NetworkObject.Data>();
                for (int i = _sceneObjects.Count - 1; i >= 0; i--)
                {
                    if(_sceneObjects[i] != null && _sceneObjects[i])
                    {
                        if (_sceneObjects[i].isOwner)
                        {
                            NetworkObject.Data data = _sceneObjects[i].GetData();
                            if(data != null)
                            {
                                if (_sceneObjects[i].ownerID <= 0 && _sceneHostID >= 0 && _sceneHostID == accountID)
                                {
                                    unownedSyncObjects.Add(data);
                                }
                                else
                                {
                                    syncObjects.Add(data);
                                }
                            }
                        }
                    }
                    else
                    {
                        _sceneObjects.RemoveAt(i);
                    }
                }
                if (syncObjects.Count > 0)
                {
                    byte[] data = Tools.Compress(Tools.Serialize<List<NetworkObject.Data>>(syncObjects));
                    Packet packet = new Packet();
                    packet.Write((int)InternalID.SYNC_ROOM_PLAYER);
                    packet.Write(sceneIndex);
                    packet.Write(unownedSyncObjects.Count > 0);
                    packet.Write(data.Length);
                    packet.Write(data);
                    if (unownedSyncObjects.Count > 0)
                    {
                        byte[] data2 = Tools.Compress(Tools.Serialize<List<NetworkObject.Data>>(unownedSyncObjects));
                        packet.Write(data2.Length);
                        packet.Write(data2);
                    }
                    SendUDPDataInternal(packet);
                }

                int s = -1;
                for (int i = 0; i < _globalObjects.Count; i++)
                {
                    if (_globalObjects[i].sceneIndex == sceneIndex)
                    {
                        s = i;
                        break;
                    }
                }
                if (s < 0)
                {
                    ObjectsData newData = new ObjectsData();
                    newData.accounts = new List<SceneObjectsData>();
                    newData.sceneIndex = sceneIndex;
                    s = _globalObjects.Count;
                    _globalObjects.Add(newData);
                }
                if (syncObjects.Count > 0)
                {
                    int a = -1;
                    for (int i = 0; i < _globalObjects[s].accounts.Count; i++)
                    {
                        if (_globalObjects[s].accounts[i].accountID == accountID)
                        {
                            a = i;
                            break;
                        }
                    }
                    if (a < 0)
                    {
                        SceneObjectsData newData = new SceneObjectsData();
                        newData.objects = new List<NetworkObject.Data>();
                        newData.accountID = accountID;
                        a = _globalObjects[s].accounts.Count;
                        _globalObjects[s].accounts.Add(newData);
                    }
                    _globalObjects[s].accounts[a].objects = syncObjects;
                }
                if (unownedSyncObjects.Count > 0)
                {
                    int u = -1;
                    for (int i = 0; i < _globalObjects[s].accounts.Count; i++)
                    {
                        if (_globalObjects[s].accounts[i].accountID <= 0)
                        {
                            u = i;
                            break;
                        }
                    }
                    if (u < 0)
                    {
                        SceneObjectsData newData = new SceneObjectsData();
                        newData.objects = new List<NetworkObject.Data>();
                        newData.accountID = -1;
                        u = _globalObjects[s].accounts.Count;
                        _globalObjects[s].accounts.Add(newData);
                    }
                    _globalObjects[s].accounts[u].objects = unownedSyncObjects;
                }
            }
        }

        private void TickReceived(byte[] data, int scene, long account, long host, bool includeUnowned, byte[] unownedData)
        {
            bool disconnected = _disconnected.Contains(account);
            if (scene == sceneIndex)
            {
                _sceneHostID = host;
            }
            if (disconnected)
            {
                return;
            }
            List<NetworkObject.Data> syncObjects = Tools.Desrialize<List<NetworkObject.Data>>(Tools.Decompress(data));
            List<NetworkObject.Data> unownedSyncObjects = new List<NetworkObject.Data>();
            if (includeUnowned)
            {
                unownedSyncObjects = Tools.Desrialize<List<NetworkObject.Data>>(Tools.Decompress(unownedData));
            }
            int s = -1;
            for (int i = 0; i < _globalObjects.Count; i++)
            {
                if (_globalObjects[i].sceneIndex == scene)
                {
                    s = i;
                    break;
                }
            }
            if(s < 0)
            {
                ObjectsData newData = new ObjectsData();
                newData.accounts = new List<SceneObjectsData>();
                newData.sceneIndex = scene;
                s = _globalObjects.Count;
                _globalObjects.Add(newData);
            }
            int a = -1;
            int u = -1;
            for (int i = 0; i < _globalObjects[s].accounts.Count; i++)
            {
                if (_globalObjects[s].accounts[i].accountID == account)
                {
                    a = i;
                    break;
                }
            }
            if (a < 0)
            {
                SceneObjectsData newData = new SceneObjectsData();
                newData.objects = new List<NetworkObject.Data>();
                newData.accountID = account;
                a = _globalObjects[s].accounts.Count;
                _globalObjects[s].accounts.Add(newData);
            }
            if (includeUnowned)
            {
                for (int i = 0; i < _globalObjects[s].accounts.Count; i++)
                {
                    if (_globalObjects[s].accounts[i].accountID <= 0)
                    {
                        u = i;
                        break;
                    }
                }
                if (u < 0)
                {
                    SceneObjectsData newData = new SceneObjectsData();
                    newData.objects = new List<NetworkObject.Data>();
                    newData.accountID = -1;
                    u = _globalObjects[s].accounts.Count;
                    _globalObjects[s].accounts.Add(newData);
                }
            }
            if (scene == sceneIndex)
            {
                int o = 0;
                for (int i = 0; i < _sceneObjects.Count; i++)
                {
                    for (int j = o; j < syncObjects.Count; j++)
                    {
                        if (syncObjects[j].id == _sceneObjects[i].id)
                        {
                            NetworkObject.Data taragetData = syncObjects[j];

                            for (int k = j; k > o; k--)
                            {
                                syncObjects[k] = syncObjects[k - 1];
                            }
                            syncObjects[o] = taragetData;

                            o++;
                            _sceneObjects[i]._ApplyData(taragetData);
                            break;
                        }
                    }
                }
                if (o < syncObjects.Count)
                {
                    for (int i = o; i < syncObjects.Count; i++)
                    {
                        // Instantiate the data without object
                        if (syncObjects[i].prefab >= 0 && syncObjects[i].prefab < Client.instance.settings.prefabs.Length && Client.instance.settings.prefabs[syncObjects[i].prefab] != null)
                        {
                            NetworkObject networkObject = Instantiate(Client.instance.settings.prefabs[syncObjects[i].prefab], syncObjects[i].transform.position, syncObjects[i].transform.rotation);
                            networkObject.id = syncObjects[i].id;
                            networkObject.prefabIndex = syncObjects[i].prefab;
                            networkObject.transform.localScale = syncObjects[i].transform.scale;
                            networkObject._Initialize(account, syncObjects[i].destroy);
                            Rigidbody rb = networkObject.GetComponent<Rigidbody>();
                            if(rb != null)
                            {
                                rb.velocity = syncObjects[i].transform.velocity;
                            }
                            // Keep track of networkObject in a list and instantiate it after leaving and coming back to scene
                            _sceneObjects.Add(networkObject);
                        }
                    }
                }

                // Unowned
                if (includeUnowned)
                {
                    o = 0;
                    for (int i = 0; i < _sceneObjects.Count; i++)
                    {
                        for (int j = o; j < unownedSyncObjects.Count; j++)
                        {
                            if (unownedSyncObjects[j].id == _sceneObjects[i].id)
                            {
                                NetworkObject.Data taragetData = unownedSyncObjects[j];

                                for (int k = j; k > o; k--)
                                {
                                    unownedSyncObjects[k] = unownedSyncObjects[k - 1];
                                }
                                unownedSyncObjects[o] = taragetData;

                                o++;
                                _sceneObjects[i]._ApplyData(taragetData);
                                break;
                            }
                        }
                    }
                }

                for (int i = _sceneObjects.Count - 1; i >= 0; i--)
                {
                    if (_sceneObjects[i] != null) { continue; }
                    _sceneObjects.RemoveAt(i);
                }
            }
            _globalObjects[s].accounts[a].objects = syncObjects;
            if (includeUnowned)
            {
                _globalObjects[s].accounts[u].objects = unownedSyncObjects;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _sceneHostID = -1;
            _scene = scene;
            _sceneObjects.Clear();
            NetworkObject[] obj = FindObjectsOfType<NetworkObject>(true);
            if (obj != null)
            {
                _sceneObjects.AddRange(obj);
            }
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
            _globalObjects.Clear();
            _inGame = false;
            _gameRoom = null;
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
                    if (OnGetRooms != null)
                    {
                        int gtRoomsBytesLen = packet.ReadInt();
                        byte[] gtRoomsBytes = packet.ReadBytes(gtRoomsBytesLen);
                        List<Data.Room> gtRooms = Tools.Desrialize<List<Data.Room>>(Tools.Decompress(gtRoomsBytes));
                        OnGetRooms.Invoke(GetRoomsResponse.SUCCESSFULL, gtRooms);
                    }
                    packet.Dispose();
                    break;
                case InternalID.JOIN_ROOM:
                    if (OnJoinRoom != null)
                    {
                        int jnRoomRes = packet.ReadInt();
                        Data.Room jnRoom = null;
                        if (jnRoomRes == (int)JoinRoomResponse.SUCCESSFULL)
                        {
                            int jnRoomBytesLen = packet.ReadInt();
                            byte[] jnRoomBytes = packet.ReadBytes(jnRoomBytesLen);
                            jnRoom = Tools.Desrialize<Data.Room>(Tools.Decompress(jnRoomBytes));
                        }
                        OnJoinRoom.Invoke((JoinRoomResponse)jnRoomRes, jnRoom);
                    }
                    packet.Dispose();
                    break;
                case InternalID.LEAVE_ROOM:
                    if (OnLeaveRoom != null)
                    {
                        int lvRoomRes = packet.ReadInt();
                        OnLeaveRoom.Invoke((LeaveRoomResponse)lvRoomRes);
                    }
                    packet.Dispose();
                    break;
                case InternalID.DELETE_ROOM:
                    if (OnDeleteRoom != null)
                    {
                        int deRoomRes = packet.ReadInt();
                        OnDeleteRoom.Invoke((DeleteRoomResponse)deRoomRes);
                    }
                    packet.Dispose();
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

                    if (upRoomTyp == (int)RoomUpdateType.PLAYER_KICKED)
                    {
                        if(upTargetPlayer.id == accountID)
                        {
                            _inGame = false;
                        }
                        PlayerLeftGame(upTargetPlayer.id);
                    }
                    else if (upRoomTyp == (int)RoomUpdateType.PLAYER_LEFT)
                    {
                        if (upPlayer.id == accountID)
                        {
                            _inGame = false;
                        }
                        PlayerLeftGame(upPlayer.id);
                    }
                    else if (upRoomTyp == (int)RoomUpdateType.ROOM_DELETED)
                    {
                        _inGame = false;
                    }
                    else if (upRoomTyp == (int)RoomUpdateType.GAME_STARTED)
                    {
                        _globalObjects.Clear();
                        _inGame = true;
                        _ticksTimer = 0;
                        _ticksCalled = 0;
                        _disconnected.Clear();
                    }

                    if (_gameRoom != null && _gameRoom.hostID == upRoom.hostID)
                    {
                        // OnHostChanged
                    }
                    _gameRoom = upRoom;

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
                case InternalID.SYNC_ROOM_PLAYER:
                    int syScene = packet.ReadInt();
                    long syAccount = packet.ReadLong();
                    long syHost = packet.ReadLong();
                    bool syUnowned = packet.ReadBool();
                    int syDataLen = packet.ReadInt();
                    byte[] syData = packet.ReadBytes(syDataLen);
                    byte[] syData2 = null;
                    if (syUnowned)
                    {
                        syDataLen = packet.ReadInt();
                        syData2 = packet.ReadBytes(syDataLen);
                    }
                    packet.Dispose();
                    TickReceived(syData, syScene, syAccount, syHost, syUnowned, syData2);
                    break;
                case InternalID.SET_HOST:
                    int stScene = packet.ReadInt();
                    long stHost = packet.ReadLong();
                    packet.Dispose();
                    if(sceneIndex == stScene)
                    {
                        _sceneHostID = stHost;
                    }
                    break;
                case InternalID.DESTROY_OBJECT:
                    int dsScene = packet.ReadInt();
                    long dsAccount = packet.ReadLong();
                    string dsId = packet.ReadString();
                    packet.Dispose();
                    _DestroyObject(dsScene, dsId, dsAccount);
                    break;
            }
        }

        private void PlayerLeftGame(long id)
        {
            _disconnected.Add(id);
            for (int i = 0; i < _globalObjects.Count; i++)
            {
                for (int j = _globalObjects[i].accounts.Count - 1; j >= 0; j--)
                {
                    if (_globalObjects[i].accounts[j].accountID == id)
                    {
                        for (int k = _globalObjects[i].accounts[j].objects.Count - 1; k >= 0; k--)
                        {
                            if (_globalObjects[i].accounts[j].objects[k].destroy)
                            {
                                _globalObjects[i].accounts[j].objects.RemoveAt(k);
                            }
                        }
                        if (_globalObjects[i].accounts.Count <= 0)
                        {
                            _globalObjects[i].accounts.RemoveAt(j);
                        }
                        break;
                    }
                }
            }
            for (int i = _sceneObjects.Count - 1; i >= 0; i--)
            {
                if (_sceneObjects[i].ownerID == id && _sceneObjects[i].destroyOnLeave)
                {
                    Destroy(_sceneObjects[i].gameObject);
                    _sceneObjects.RemoveAt(i);
                }
            }
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

        public static void CreateRoom(int gameID, int team)
        {
            CreateRoom(gameID, team, "");
        }

        public static void CreateRoom(int gameID, int team, string password)
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
                if (!string.IsNullOrEmpty(password))
                {
                    password = Tools.EncrypteToMD5(password);
                }
                Packet packet = new Packet();
                packet.Write((int)InternalID.CREATE_ROOM);
                packet.Write(password);
                packet.Write(gameID);
                packet.Write(team);
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

        public static void JoinRoom(string roomID, int team)
        {
            JoinRoom(roomID, team, "");
        }

        public static void JoinRoom(string roomID, int team, string password)
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
                packet.Write(team);
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

        public static NetworkObject Instantiate(int prefabIndex, Vector3 position, Quaternion rotation, bool own = true, bool destroyOnLeave = false)
        {
            return instance._Instantiate(prefabIndex, position, rotation, own, destroyOnLeave);
        }

        private NetworkObject _Instantiate(int prefabIndex, Vector3 position, Quaternion rotation, bool own, bool destroyOnLeave)
        {
            NetworkObject _object = null;
            if (prefabIndex >= 0 && prefabIndex < Client.instance.settings.prefabs.Length && Client.instance.settings.prefabs[prefabIndex] != null)
            {
                _object = Instantiate(Client.instance.settings.prefabs[prefabIndex], position, rotation);
                _object.id = Guid.NewGuid().ToString();
                _object.prefabIndex = prefabIndex;
                _object._Initialize(own ? accountID : -1, destroyOnLeave);
                _sceneObjects.Add(_object);
                // Keep track of _object in a list and instantiate it after leaving and coming back to scene
            }
            return _object;
        }

        private enum InternalID
        {
            AUTH = 1, GET_ROOMS = 2, CREATE_ROOM = 3, JOIN_ROOM = 4, LEAVE_ROOM = 5, DELETE_ROOM = 6, ROOM_UPDATED = 7, KICK_FROM_ROOM = 8, STATUS_IN_ROOM = 9, START_ROOM = 10, SYNC_ROOM_PLAYER = 11, SET_HOST = 12, DESTROY_OBJECT = 13
        }

        public enum AuthenticationResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, ALREADY_AUTHENTICATED = 4, USERNAME_TAKEN = 5, WRONG_CREDS = 6, BANNED = 7, INVALID_INPUT = 8
        }

        public enum CreateRoomResponse
        {
            UNKNOWN = 0, SUCCESSFULL = 1, NOT_CONNECTED = 2, NOT_AUTHENTICATED = 3, ALREADY_IN_ANOTHER_ROOM = 4
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

        private void _DestroyObject(int scene, string id, long account)
        {
            if(sceneIndex == scene)
            {
                for (int i = 0; i < _sceneObjects.Count; i++)
                {
                    if (_sceneObjects[i].id == id)
                    {
                        if(_sceneObjects[i].ownerID < 0 || account == _sceneObjects[i].ownerID)
                        {
                            Destroy(_sceneObjects[i].gameObject);
                            _sceneObjects.RemoveAt(i);
                        }
                        break;
                    }
                }
            }
            for (int i = 0; i < _globalObjects.Count; i++)
            {
                if (_globalObjects[i].sceneIndex == scene)
                {
                    bool found = false;
                    int u = -1;
                    int a = -1;
                    for (int j = 0; j < _globalObjects[i].accounts.Count; j++)
                    {
                        if (found)
                        {
                            break;
                        }
                        if (_globalObjects[i].accounts[j].accountID == account)
                        {
                            a = j;
                            for (int k = 0; k < _globalObjects[i].accounts[j].objects.Count; k++)
                            {
                                if (_globalObjects[i].accounts[j].objects[k].id == id)
                                {
                                    _globalObjects[i].accounts[j].objects.RemoveAt(k);
                                    found = true;
                                    break;
                                }
                            }
                            if(u >= 0)
                            {
                                break;
                            }
                        }
                        else if (_globalObjects[i].accounts[j].accountID < 0)
                        {
                            u = j;
                            if (a >= 0)
                            {
                                break;
                            }
                        }
                    }
                    if(!found && u >= 0)
                    {
                        for (int k = 0; k < _globalObjects[i].accounts[u].objects.Count; k++)
                        {
                            if (_globalObjects[i].accounts[u].objects[k].id == id)
                            {
                                _globalObjects[i].accounts[u].objects.RemoveAt(k);
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        public void _DestroyObject(NetworkObject target)
        {
            if (target == null)
            {

            }
            else if (!instance._connected)
            {

            }
            else if (!instance._authenticated)
            {

            }
            else if (isGameStarted && target.isOwner)
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.DESTROY_OBJECT);
                packet.Write(sceneIndex);
                packet.Write(target.id);
                SendTCPDataInternal(packet);
                if (!target.isDestroying)
                {
                    Destroy(target.gameObject);
                }
            }
            else
            {

            }
        }

    }
}