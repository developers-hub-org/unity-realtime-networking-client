namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Demo_01_Controller : MonoBehaviour
    {

        [SerializeField] private float _moveSpeed = 6f;

        private CharacterController _controller = null;
        private NetworkObject _object = null;

        private void Start()
        {
            _object = GetComponent<NetworkObject>();
            if(_object != null && _object.isOwner == false)
            {
                Destroy(this);
            }
            else
            {
                _controller = GetComponent<CharacterController>();
                Camera.main.transform.SetParent(transform, false);
                Camera.main.transform.localPosition = new Vector3(0, 6, -4);
                Camera.main.transform.localEulerAngles = new Vector3(45, 0, 0);
            }
        }

        private void Update()
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) { input.y = 1; }
            else if (Input.GetKey(KeyCode.S)) { input.y = -1; }
            if (Input.GetKey(KeyCode.D)) { input.x = 1; }
            else if (Input.GetKey(KeyCode.A)) { input.x = -1; }
            Vector3 moveDirection = Vector3.Normalize((input.x * transform.right) + (input.y * transform.forward));
            moveDirection.y = 0;
            _controller.Move(moveDirection * Time.deltaTime * _moveSpeed);
            if (Input.GetKey(KeyCode.Q))
            {
                _controller.transform.Rotate(Vector3.up, -50 * Time.deltaTime, Space.World);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                _controller.transform.Rotate(Vector3.up, 50 * Time.deltaTime, Space.World);
            }
        }

    }
}