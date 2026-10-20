using UnityEngine;

namespace D22
{
    public sealed class D22PigeonFlock : MonoBehaviour
    {
        Vector3 home;
        Transform[] birds;
        Vector3[] roost;
        float[] phase;

        public Vector3 Center => transform.position;

        void Awake()
        {
            home = transform.position;
            int n = transform.childCount;
            birds = new Transform[n];
            roost = new Vector3[n];
            phase = new float[n];
            for (int i = 0; i < n; i++)
            {
                birds[i] = transform.GetChild(i);
                roost[i] = birds[i].localPosition;
                phase[i] = i * 1.37f;
            }
        }

        void Update()
        {
            float t = Time.time;
            Vector3 prev = transform.position;
            transform.position = home + new Vector3(Mathf.Sin(t * .32f) * 4.2f, Mathf.Sin(t * .55f) * .55f, Mathf.Cos(t * .27f) * 3.1f);
            Vector3 flight = transform.position - prev;
            if (flight.sqrMagnitude > .000001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flight.normalized, Vector3.up), 2.5f * Time.deltaTime);
            for (int i = 0; i < birds.Length; i++)
            {
                float p = t * 1.6f + phase[i];
                var bird = birds[i];
                Vector3 was = bird.position;
                bird.localPosition = roost[i] + new Vector3(Mathf.Sin(p) * .7f, Mathf.Sin(p * 2.4f) * .22f, Mathf.Cos(p * .85f) * .65f);
                Vector3 step = bird.position - was;
                if (step.sqrMagnitude > .000001f)
                    bird.rotation = Quaternion.Slerp(bird.rotation, Quaternion.LookRotation(step.normalized, Vector3.up), 6f * Time.deltaTime);
            }
        }
    }
}
