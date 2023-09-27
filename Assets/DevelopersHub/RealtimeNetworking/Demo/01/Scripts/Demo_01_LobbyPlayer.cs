namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using UnityEngine;
    using UnityEngine.UI;

    public class Demo_01_LobbyPlayer : MonoBehaviour
    {

        [SerializeField] private Button buttonKick = null;
        [SerializeField] private Button buttonReady = null;
        [SerializeField] private Text textUsername = null;
        [SerializeField] private Text textStatus = null;
        private long _id = -1; public long id { get { return _id; } }
        private bool _ready = false; public bool ready { get { return _ready; } }

        private void Start()
        {
            buttonKick.onClick.AddListener(OnKickClicked);
            buttonReady.onClick.AddListener(OnReadyClicked);
        }

        public void Initialize(Data.Player player, bool canKick)
        {
            buttonKick.interactable = canKick;
            if (player != null)
            {
                _id = player.id;
                textUsername.text = player.username;
                SetStatus(player.ready);
            }
        }

        private void OnKickClicked()
        {
            buttonKick.interactable = false;
            Demo_01_Manager manager = FindObjectOfType<Demo_01_Manager>();
            if (manager != null)
            {
                manager.KickPlayer(_id);
            }
        }

        private void OnReadyClicked()
        {
            buttonReady.interactable = false;
            Demo_01_Manager manager = FindObjectOfType<Demo_01_Manager>();
            if (manager != null)
            {
                manager.ChangePlayerStatus(!_ready);
            }
        }

        public void SetStatus(bool status)
        {
            buttonReady.interactable = _id == RealtimeNetworking.accountID;
            _ready = status;
            if (status)
            {
                textStatus.text = "Ready";
                textStatus.color = Color.green;
            }
            else
            {
                textStatus.text = "Not Ready";
                textStatus.color = Color.red;
            }
        }

    }
}