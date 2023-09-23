namespace DevelopersHub.RealtimeNetworking.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(NetworkObject))] public class EditorNetworkObject : Editor
    {

        private SerializedProperty id = null;

        private void OnEnable()
        {
            id = serializedObject.FindProperty("_id");
        }

        public override void OnInspectorGUI()
        {
            NetworkObject networkObject = target as NetworkObject;
            if (string.IsNullOrEmpty(id.stringValue))
            {
                GenerateID();
            }
            NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
            if (networkObjects != null && networkObjects.Length > 0)
            {
                for (int i = 0; i < networkObjects.Length; i++)
                {
                    if (networkObjects[i] == networkObject) { continue; }
                    if (networkObjects[i].id == networkObject.id)
                    {
                        GenerateID();
                        break;
                    }
                }
            }
            base.OnInspectorGUI();
        }

        private void GenerateID()
        {
            id.stringValue = Guid.NewGuid().ToString();
            serializedObject.ApplyModifiedProperties();
        }

    }
}