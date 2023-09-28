namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Demo_01_Platform : MonoBehaviour
    {

        [SerializeField] private float speed = 2f;
        [SerializeField] private Vector3 point1 = Vector3.zero;
        [SerializeField] private Vector3 point2 = Vector3.zero;

        private bool stage = true;

        private NetworkObject _object = null;

        private void Start()
        {
            _object = GetComponent<NetworkObject>();
        }

        private void Update()
        {

            if (_object == null || _object.isOwner == false)
            {
                return;
            }

            Vector3 target = transform.position;

            if (stage)
            {
                target = point1;
            }
            else
            {
                target = point2;
            }

            transform.position = Vector3.Lerp(transform.position, target, speed * Time.deltaTime);

            if(Vector3.Distance(transform.position, target) <= 0.1f)
            {
                stage = !stage;
            }

        }

    }
}