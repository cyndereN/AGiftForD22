using UnityEngine;

namespace D22
{
    // Scene-authored interaction anchors keep story logic independent of room coordinates.
    public sealed class D22RecordShop : MonoBehaviour
    {
        public Transform shopkeeper;
        public Transform bottleSpawn;
        public GameObject bottlePrefab;
        public float talkRadius = 1.9f;
        public bool CanTalk(Transform viewer) => shopkeeper &&
            Vector3.Distance(viewer.position, shopkeeper.position) <= talkRadius &&
            Vector3.Dot(viewer.forward, (shopkeeper.position-viewer.position).normalized) > .55f;
        void OnDrawGizmosSelected()
        {
            if (!shopkeeper) return;
            Gizmos.color = new Color(.9f,.65f,.3f);
            Gizmos.DrawWireSphere(shopkeeper.position, talkRadius);
        }
    }
}
