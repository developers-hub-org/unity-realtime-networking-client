namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class NetworkObject : MonoBehaviour
    {

        [Space] [ReadOnly] [SerializeField] private string _id = ""; public string id { get { return _id; } set { _id = value; } }
        private bool _destroyOnLeave = false; public bool destroyOnLeave { get { return _destroyOnLeave; } }
        [SerializeField] private bool _syncTransform = true;
        private float _positionLerpTime = 0.05f;
        private float _rotationLerpTime = 0.05f;
        [SerializeField] private bool _syncAnimation = true;

        private long _ownerID = -1; public bool isOwner { get { return (_ownerID >= 0 && _ownerID == RealtimeNetworking.accountID); } }
        public long ownerID { get { return _ownerID; } }

        private Vector3 _origionPosition = Vector3.zero;
        private Quaternion _origionRotation = Quaternion.identity;
        private Vector3 _origionScale = Vector3.one;
        private Vector3 _targetPosition = Vector3.zero;
        private Quaternion _targetRotation = Quaternion.identity;
        private Vector3 _targetScale = Vector3.one;
        private Rigidbody _rigidbody = null;
        private Animator _animator = null;
        private float _time = 0;
        private float _timeDelay = 0;
        private Vector3 _moveVelocity = Vector3.one;
        private float _rotateVelocity = 0;
        private int _prefabIndex = -1; public int prefabIndex { get { return _prefabIndex; } set { _prefabIndex = value; } }

        private void Start()
        {
            _origionPosition = transform.position;
            _origionRotation = transform.rotation;
            _origionScale = transform.localScale;
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _targetScale = transform.localScale;
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (_syncTransform && RealtimeNetworking.isGameStarted && !isOwner)
            {
                Vector3 position = _targetPosition;
                Quaternion rotation = _targetRotation;
                float distance = Vector3.Distance(transform.position, position);
                float angle = Quaternion.Angle(transform.rotation, rotation);
                if (transform.position != position)
                {
                    position = Vector3.SmoothDamp(transform.position, position, ref _moveVelocity, _positionLerpTime);
                }
                if (transform.rotation != rotation)
                {
                    float delta = Quaternion.Angle(transform.rotation, rotation);
                    if (delta > 0f)
                    {
                        float t = Mathf.SmoothDampAngle(delta, 0.0f, ref _rotateVelocity, _rotationLerpTime);
                        t = 1.0f - (t / delta);
                        rotation = Quaternion.Slerp(transform.rotation, rotation, t);
                    }
                }
                transform.position = position;
                transform.rotation = rotation;
                if (transform.localScale != _targetScale)
                {
                    transform.localScale = _targetScale;
                }
            }
        }

        public Data GetData()
        {
            if(!_syncTransform && (!_syncAnimation || _animator == null))
            {
                return null;
            }
            Data data = new Data();
            data.id = _id;
            data.prefab = _prefabIndex;
            data.destroy = _destroyOnLeave;
            if (_syncTransform)
            {
                data.transform = new TransformData();
                data.transform.position = transform.position;
                data.transform.rotation = transform.rotation;
                data.transform.scale = transform.localScale;
                if(_rigidbody != null)
                {
                    data.transform.velocity = _rigidbody.velocity;
                }
            }
            if (_syncAnimation && _animator != null)
            {
                data.animation = new AnimationData();

            }
            return data;
        }

        public void _ApplyData(Data data)
        {
            _timeDelay = Time.realtimeSinceStartup - _time;
            if (_syncTransform)
            {
                _targetPosition = data.transform.position;
                _targetRotation = data.transform.rotation;
                _targetScale = data.transform.scale;
                if (_time == 0)
                {
                    transform.position = _targetPosition;
                    transform.rotation = _targetRotation;
                    transform.localScale = _targetScale;
                    if (_rigidbody != null)
                    {
                        _rigidbody.velocity = data.transform.velocity;
                        _moveVelocity = data.transform.velocity;
                    }
                }
                else
                {
                    if (_rigidbody != null)
                    {
                        _rigidbody.velocity = data.transform.velocity;
                        _moveVelocity = data.transform.velocity;
                        _targetPosition = _targetPosition + data.transform.velocity * (_timeDelay);
                    }
                }
            }
            _time = Time.realtimeSinceStartup;
        }

        public void _Initialize(long owner, bool destroyOnLeave = true)
        {
            _destroyOnLeave = destroyOnLeave;
            _ownerID = owner; 
        }

        public class Data
        {
            public string id = string.Empty;
            public int prefab = -1;
            public bool destroy = false;
            public TransformData transform = null;
            public AnimationData animation = null;
        }

        public class TransformData
        {
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
            public Vector3 velocity = Vector3.one;
        }

        public class AnimationData
        {
            
        }

    }
}