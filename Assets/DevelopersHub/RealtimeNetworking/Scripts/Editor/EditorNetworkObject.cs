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
        //private SerializedProperty syncTransform = null;
        //private SerializedProperty syncAnimation = null;

        private void OnEnable()
        {
            id = serializedObject.FindProperty("_id");
            //syncTransform = serializedObject.FindProperty("_syncTransform");
            //syncAnimation = serializedObject.FindProperty("_syncAnimation");
        }

        public override void OnInspectorGUI()
        {
            NetworkObject networkObject = target as NetworkObject;
            if (string.IsNullOrEmpty(id.stringValue))
            {
                GenerateID();
            }
            if (!Application.isPlaying)
            {
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
                /*
                Animator animator = networkObject.GetComponent<Animator>();
                if (animator == null)
                {
                    if (syncAnimation.boolValue)
                    {
                        syncAnimation.boolValue = false;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                */
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