using UnityEngine;

namespace D22
{
    public enum D22MarkKind { Cricket, Grind, Pigeons, Drum, Guitar, Bass }

    public sealed class D22WorldMark : MonoBehaviour
    {
        public D22MarkKind kind;
        public float radius = 1.4f;
        void OnDrawGizmos()
        {
            Gizmos.color = kind == D22MarkKind.Cricket ? Color.green
                : kind == D22MarkKind.Pigeons ? Color.cyan
                : kind == D22MarkKind.Drum ? new Color(.7f, .2f, .15f)
                : kind == D22MarkKind.Guitar ? new Color(.8f, .55f, .25f)
                : kind == D22MarkKind.Bass ? new Color(.35f, .5f, .85f)
                : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
