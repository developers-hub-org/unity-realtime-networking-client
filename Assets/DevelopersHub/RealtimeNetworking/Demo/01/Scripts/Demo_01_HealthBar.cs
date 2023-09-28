namespace DevelopersHub.RealtimeNetworking.Client.Demo
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class Demo_01_HealthBar : MonoBehaviour
    {

        public Image bar = null;
        public RectTransform rect = null;

        private void Awake()
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0);
        }

    }
}