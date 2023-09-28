namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    public class NetworkObject : MonoBehaviour
    {

        [Space] [ReadOnly] [SerializeField] private string _id = ""; public string id { get { return _id; } set { _id = value; } }
        private bool _destroyOnLeave = false; public bool destroyOnLeave { get { return _destroyOnLeave; } }
        [SerializeField] private bool _syncTransform = true;
        private float _positionLerpTime = 0.05f;
        private float _rotationLerpTime = 0.05f;
        [Tooltip("Sync animation is srill experimental and only syncs the animator parameters except trigger.")]
        [SerializeField] private bool _syncAnimation = true;

        private long _ownerID = -1; public bool isOwner { get { return (_ownerID >= 0 && _ownerID == RealtimeNetworking.accountID) || (_ownerID < 0 && RealtimeNetworking.isSceneHost); } }
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
        private bool _destroying = false; public bool isDestroying { get { return _destroying; } }

        MonoBehaviour[] mono = null;

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
            if(gameObject.isStatic)
            {
                _syncTransform = false;
            }
            if (_animator == null)
            {
                _syncAnimation = false;
            }
            mono = GetComponents<MonoBehaviour>();
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

        private void OnDestroy()
        {
            _destroying = true;
            RealtimeNetworking.instance._DestroyObject(this);
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
                if(_animator.parameters.Length > 0)
                {
                    data.animation = new AnimationData();
                    for (int i = 0; i < _animator.parameters.Length; i++)
                    {
                        Parameter parameter = new Parameter();
                        parameter.name = _animator.parameters[i].name;
                        parameter.type = (int)_animator.parameters[i].type;
                        switch (_animator.parameters[i].type)
                        {
                            case AnimatorControllerParameterType.Float:
                                parameter.value = _animator.parameters[i].defaultFloat;
                                break;
                            case AnimatorControllerParameterType.Int:
                                parameter.value = _animator.parameters[i].defaultInt;
                                break;
                            case AnimatorControllerParameterType.Bool:
                                parameter.value = _animator.parameters[i].defaultBool;
                                break;
                            case AnimatorControllerParameterType.Trigger:
                                parameter.value = 0;
                                break;
                        }
                        data.animation.parameters.Add(parameter);
                    }
                }
            }
            if (mono != null && mono.Length > 0)
            {
                for (int m = 0; m < mono.Length; m++)
                {
                    if (mono[m] == null || mono[m] == this) { continue; }
                    FieldInfo[] objectFields = mono[m].GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    ScriptData vd = new ScriptData();
                    vd.name = mono[m].name;
                    for (int f = 0; f < objectFields.Length; f++)
                    {
                        if (PropertyAttribute.GetCustomAttribute(objectFields[f], typeof(SyncVariable)) != null)
                        {
                            if(data.scripts == null)
                            {
                                data.scripts = new List<ScriptData>();
                            }
                            Variable variable = new Variable();
                            variable.name = objectFields[f].Name;
                            variable.value = objectFields[f].GetValue(mono[m]);
                            vd.variables.Add(variable);
                        }
                    }
                    if(vd.variables.Count > 0)
                    {
                        data.scripts.Add(vd);
                    }
                }
            }
            return data;
        }

        public void _ApplyData(Data data)
        {
            _timeDelay = Time.realtimeSinceStartup - _time;
            if(data.scripts != null && mono != null && mono.Length > 0)
            {
                int o = 0;
                for (int m = 0; m < mono.Length; m++)
                {
                    if (mono[m] == null || mono[m] == this) { continue; }
                    for (int i = o; i < data.scripts.Count; i++)
                    {
                        if (mono[m].name == data.scripts[i].name)
                        {
                            ScriptData taragetData = data.scripts[i];
                            for (int k = i; k > o; k--)
                            {
                                data.scripts[k] = data.scripts[k - 1];
                            }
                            data.scripts[o] = taragetData;
                            o++;
                            FieldInfo[] objectFields = mono[m].GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            for (int v = 0; v < taragetData.variables.Count; v++)
                            {
                                for (int f = 0; f < objectFields.Length; f++)
                                {
                                    if (objectFields[f].Name == taragetData.variables[v].name)
                                    {
                                        objectFields[f].SetValue(mono[m], taragetData.variables[v].value);
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            if (_syncTransform && data.transform != null)
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
            if(_syncAnimation && data.animation != null && _animator != null) 
            {
                for (int i = 0; i < data.animation.parameters.Count; i++)
                {
                    switch ((AnimatorControllerParameterType)data.animation.parameters[i].type)
                    {
                        case AnimatorControllerParameterType.Float:
                            _animator.SetFloat(data.animation.parameters[i].name, (float)data.animation.parameters[i].value);
                            break;
                        case AnimatorControllerParameterType.Int:
                            _animator.SetInteger(data.animation.parameters[i].name, (int)data.animation.parameters[i].value);
                            break;
                        case AnimatorControllerParameterType.Bool:
                            _animator.SetBool(data.animation.parameters[i].name, (bool)data.animation.parameters[i].value);
                            break;
                        case AnimatorControllerParameterType.Trigger:

                            break;
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
            public List<ScriptData> scripts = null;
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
            public List<Parameter> parameters = new List<Parameter>();
        }

        public class Parameter
        {
            public string name = string.Empty;
            public int type = 1;
            public object value = null;
        }

        public class ScriptData
        {
            public string name = string.Empty;
            public List<Variable> variables = new List<Variable>();
        }

        public class Variable
        {
            public string name = string.Empty;
            public object value = null;
        }

    }
}