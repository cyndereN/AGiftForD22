using UnityEngine;
namespace D22
{
    public sealed class D22Exit : MonoBehaviour
    {
        public string nextScene;
        public string label;
        public float radius=2;
        void OnDrawGizmos(){Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(transform.position,radius);}
    }
}
