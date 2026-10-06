using UnityEngine;

namespace D22
{
    public enum D22MarkKind { Cricket, Grind, Pigeons }

    public sealed class D22WorldMark : MonoBehaviour
    {
        public D22MarkKind kind;
        public float radius = 1.4f;
        void OnDrawGizmos()
        {
            Gizmos.color = kind == D22MarkKind.Cricket ? Color.green : kind == D22MarkKind.Pigeons ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
