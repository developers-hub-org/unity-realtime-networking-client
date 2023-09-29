namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Demo_01_Projectile : MonoBehaviour
    {

        private int _damage = 1;
        private Demo_01_Controller _shooter = null;
        private Rigidbody _rigidbody = null;
        private Collider _collider = null;
        // private NetworkObject _object = null;
        private float _timer = 0;
        private bool _initialized = false;

        private void Awake()
        {
            _Initialize();
        }

        private void _Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            // _object = GetComponent<NetworkObject>();
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            if(_rigidbody == null )
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            _collider = gameObject.GetComponent<SphereCollider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<SphereCollider>();
            }
            _rigidbody.useGravity = true;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _collider.isTrigger = false;
        }

        public void Initialize(int damage, Vector3 force, Demo_01_Controller shooter)
        {
            _Initialize();
            _shooter = shooter;
            _damage = damage;
            _rigidbody.velocity = force;
        }

        private void Update()
        {
            /*
            if (_object == null || !_object.isOwner)
            {
                return;
            }
            */
            _timer += Time.deltaTime;
            if(_timer > 20)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Demo_01_Controller controller = collision.transform.root.GetComponent<Demo_01_Controller>();
            if (controller != null)
            {
                if(controller == _shooter)
                {
                    Physics.IgnoreCollision(collision.collider, _collider);
                }
                else
                {
                    controller.ApplyDamage(_damage);
                }
            }
            else
            {
                /*
                if (_object != null && _object.isOwner)
                {
                    Destroy(gameObject);
                }
                */
            }
        }

    }
}