namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Demo_01_Controller : MonoBehaviour
    {

        [SyncVariable] public int health = 100;

        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private Demo_01_HealthBar _healthPrefab = null;
        [SerializeField] public Transform _head = null;

        private CharacterController _controller = null;
        private NetworkObject _object = null;
        private Vector3 _velocity = Vector3.zero;
        private Demo_01_HealthBar _healthBar = null;

        private void Start()
        {
            health = 100;
            _object = GetComponent<NetworkObject>();
            if (_object != null && _object.isOwner)
            {
                _controller = GetComponent<CharacterController>();
                Camera.main.transform.SetParent(transform, false);
                Camera.main.transform.localPosition = new Vector3(0, 6, -4);
                Camera.main.transform.localEulerAngles = new Vector3(45, 0, 0);
            }
            if(_healthPrefab != null)
            {
                _healthBar = Instantiate(_healthPrefab, FindObjectOfType<Canvas>().transform);
                _healthBar.bar.fillAmount = health / 100f;
            }
        }

        private void Update()
        {
            if(_healthBar != null)
            {
                if(_head  != null)
                {
                    Vector2 position = Camera.main.WorldToScreenPoint(_head.position) / FindObjectOfType<Canvas>().scaleFactor;
                    _healthBar.rect.anchoredPosition = position;
                }
                _healthBar.bar.fillAmount = health / 100f;
            }
            if (_object == null || _object.isOwner == false)
            {
                return;
            }
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) { input.y = 1; }
            else if (Input.GetKey(KeyCode.S)) { input.y = -1; }
            if (Input.GetKey(KeyCode.D)) { input.x = 1; }
            else if (Input.GetKey(KeyCode.A)) { input.x = -1; }
            Vector3 moveDirection = Vector3.Normalize((input.x * transform.right) + (input.y * transform.forward));
            moveDirection.y = 0;
            _controller.Move(moveDirection * Time.deltaTime * _moveSpeed);
            if (_controller.isGrounded && _velocity.y < 0.2f)
            {
                _velocity.y = 0f;
            }
            _velocity.y += Physics.gravity.y * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
            if (Input.GetKey(KeyCode.Q))
            {
                _controller.transform.Rotate(Vector3.up, -50 * Time.deltaTime, Space.World);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                _controller.transform.Rotate(Vector3.up, 50 * Time.deltaTime, Space.World);
            }
        }

        private void OnDestroy()
        {
            if (_healthBar != null)
            {
                Destroy(_healthBar.gameObject);
            }
        }

    }
}