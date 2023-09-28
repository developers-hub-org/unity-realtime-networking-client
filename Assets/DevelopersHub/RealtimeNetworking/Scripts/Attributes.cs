using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DevelopersHub.RealtimeNetworking.Client
{

    public class ReadOnlyAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field)] public class SyncVariable : PropertyAttribute
    {

        public SyncVariable() 
        { 

        }

    }

}