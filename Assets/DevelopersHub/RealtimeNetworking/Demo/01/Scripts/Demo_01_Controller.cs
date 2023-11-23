namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Demo_01_Controller : MonoBehaviour
    {

        [SyncVariable(whoCanChange = SyncVariable.WhoCanChange.Host)] public int health = 100;

        [SerializeField] private int _damage = 5;
        [SerializeField] private float _fireRate = 0.5f;
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private Demo_01_HealthBar _healthPrefab = null;
        [SerializeField] public Transform _head = null;
        [SerializeField] private int _bulletPrefabIndex = 1;
        [SerializeField] public Transform _gunMuzzle = null;

        private float _fireTimer = 0;
        private CharacterController _controller = null;
        private NetworkObject _object = null;
        private Vector3 _velocity = Vector3.zero;
        private Demo_01_HealthBar _healthBar = null;
        private Canvas _canvas = null;
        private Camera _camera = null;

        private void Start()
        {
            health = 100;
            _canvas = FindFirstObjectByType<Canvas>();
            _camera = Camera.main;
            _object = GetComponent<NetworkObject>();
            if (_object != null && _object.isOwner)
            {
                _controller = GetComponent<CharacterController>();
                if(_camera != null)
                {
                    _camera.transform.SetParent(transform, false);
                    _camera.transform.localPosition = new Vector3(0, 6, -4);
                    _camera.transform.localEulerAngles = new Vector3(45, 0, 0);
                }
            }
            if(_healthPrefab != null)
            {
                _healthBar = Instantiate(_healthPrefab, _canvas.transform);
                _healthBar.bar.fillAmount = health / 100f;
            }
        }

        private void Update()
        {
            if(_healthBar != null)
            {
                if(_head != null && _canvas != null && _camera != null)
                {
                    Vector2 position = _camera.WorldToScreenPoint(_head.position) / _canvas.scaleFactor;
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
            if(_fireTimer > 0)
            {
                _fireTimer -= Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.F) && _gunMuzzle != null && _fireTimer <= 0)
            {
                _fireTimer = _fireRate;
                Demo_01_Projectile bullet = RealtimeNetworking.InstantiatePrefab(_bulletPrefabIndex, _gunMuzzle.position, Quaternion.LookRotation(_gunMuzzle.forward), false, false).GetComponent<Demo_01_Projectile>();
                bullet.Initialize(_damage, _gunMuzzle.forward.normalized * 100f, this);
            }
        }

        private void OnDestroy()
        {
            if (_healthBar != null)
            {
                Destroy(_healthBar.gameObject);
            }
            if(_object != null && _object.isOwner)
            {
                if (_camera != null)
                {
                    _camera.transform.SetParent(null);
                    _camera.enabled = true;
                }
            }
        }

        public void ApplyDamage(int damage)
        {
            if (!RealtimeNetworking.isSceneHost)
            {
                return;
            }
            health -= damage;
            if(health < 0)
            {
                Destroy(gameObject);
            }
        }

    }
}