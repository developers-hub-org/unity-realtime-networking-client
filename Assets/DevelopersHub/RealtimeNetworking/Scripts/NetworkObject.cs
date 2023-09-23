namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using UnityEngine;

    public class NetworkObject : MonoBehaviour
    {

        [ReadOnly] [SerializeField] private string _id = ""; public string id { get { return _id; } set { _id = value; } }
        [SerializeField] private bool _syncTransform = true;
        
    }
}