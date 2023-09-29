namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Functions : MonoBehaviour
    {

        public static float LerpFloat(float source, float target, float speed)
        {
            if (speed <= 0 || source == target) { return source; }
            float delta = Mathf.Abs(source - target);
            float t = speed * Time.deltaTime;
            if (t > delta) { t = delta; }
            return Mathf.Lerp(source, target, delta == 0 ? 1f : t / delta);
        }

    }
}