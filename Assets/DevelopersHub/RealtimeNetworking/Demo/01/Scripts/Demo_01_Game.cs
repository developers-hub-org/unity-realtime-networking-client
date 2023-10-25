namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.SceneManagement;

    public class Demo_01_Game : MonoBehaviour
    {

        [SerializeField] private Button buttonQuit = null;
        [SerializeField] private string menuSceneName = "Demo_01_1_Menu";

        private void Start()
        {
            RealtimeNetworking.OnLeaveGame += OnLeaveGame;
            if (RealtimeNetworking.isGameStarted && !Demo_01_Manager.isSpawnedPlayer)
            {
                RealtimeNetworking.InstantiatePrefab(0, new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f)), Quaternion.identity, true, true);
            }
            buttonQuit.onClick.AddListener(OnQuitClicked);
        }

        private void OnDestroy()
        {
            RealtimeNetworking.OnLeaveGame -= OnLeaveGame;
        }

        private void OnQuitClicked()
        {
            buttonQuit.interactable = false;
            RealtimeNetworking.LeaveGame();
        }

        private void OnLeaveGame(RealtimeNetworking.LeaveGameResponse response)
        {
            if(response == RealtimeNetworking.LeaveGameResponse.SUCCESSFULL)
            {
                if (SceneUtility.GetBuildIndexByScenePath(menuSceneName) >= 0)
                {
                    SceneManager.LoadScene(menuSceneName);
                }
                else
                {
                    Debug.LogError(menuSceneName + " scene is not in the build list !!!");
                }
            }
            else
            {
                buttonQuit.interactable = true;
            }
        }

    }
}