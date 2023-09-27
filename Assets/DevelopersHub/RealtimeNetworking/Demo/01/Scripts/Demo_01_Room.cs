namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using UnityEngine;
    using UnityEngine.UI;

    public class Demo_01_Room : MonoBehaviour
    {

        [SerializeField] private Button buttonJoin = null;
        [SerializeField] private Text textUsername = null;
        [SerializeField] private Text textCapacity = null;
        private string _id = string.Empty;

        private void Start ()
        {
            buttonJoin.onClick.AddListener(OnJoinClicked);
        }

        public void Initialize(Data.Room room)
        {
            if(room != null)
            {
                _id = room.id;
                textUsername.text = room.hostUsername;
                textCapacity.text = room.players.Count.ToString() + "/" + room.maxPlayers.ToString();
            }
        }

        private void OnJoinClicked()
        {
            buttonJoin.interactable = false;
            Demo_01_Manager manager = FindObjectOfType<Demo_01_Manager>();
            if(manager != null)
            {
                manager.JoinRoom(_id);
            }
        }

    }
}