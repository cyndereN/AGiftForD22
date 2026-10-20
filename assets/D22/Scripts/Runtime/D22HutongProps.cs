using UnityEngine;

namespace D22
{
    public sealed class D22HutongProps : MonoBehaviour
    {
        bool seated;

        public void Show() => gameObject.SetActive(true);

        void OnEnable() => seated = false;

        void LateUpdate()
        {
            if (seated) return;
            seated = true;
            SeatCricket();
        }

        void SeatCricket()
        {
            var cricket = transform.Find("Cricket");
            if (!cricket) return;
            var hits = Physics.RaycastAll(new Vector3(cricket.position.x, 40f, cricket.position.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
            float startY = cricket.position.y;
            float ground = startY;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].normal.y < .65f) continue;
                if (hits[i].transform && hits[i].transform.IsChildOf(transform)) continue;
                float dy = startY - hits[i].point.y;
                if (dy < -.05f) continue;
                if (dy >= best) continue;
                best = dy;
                ground = hits[i].point.y;
                found = true;
            }
            if (!found) return;
            var renderers = cricket.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                cricket.position = new Vector3(cricket.position.x, ground, cricket.position.z);
                return;
            }
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            cricket.position += Vector3.up * (ground - bounds.min.y + .02f);
        }
    }
}
