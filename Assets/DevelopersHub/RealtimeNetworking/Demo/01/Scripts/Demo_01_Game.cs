namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.SceneManagement;

    public class Demo_01_Game : MonoBehaviour
    {

        [SerializeField] private Button buttonQuit = null;
        [SerializeField] private string menuSceneName = "Demo_01_1_Menu";

        private void Start()
        {
            RealtimeNetworking.OnLeaveRoom += OnLeaveRoom;
            if (RealtimeNetworking.isGameStarted)
            {
                RealtimeNetworking.InstantiatePrefab(0, new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f)), Quaternion.identity, true, true);
            }
            buttonQuit.onClick.AddListener(OnQuitClicked);
        }

        private void OnDestroy()
        {
            RealtimeNetworking.OnLeaveRoom -= OnLeaveRoom;
        }

        private void OnQuitClicked()
        {
            buttonQuit.interactable = false;
            RealtimeNetworking.LeaveRoom();
        }

        private void OnLeaveRoom(RealtimeNetworking.LeaveRoomResponse response)
        {
            if(response == RealtimeNetworking.LeaveRoomResponse.SUCCESSFULL)
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