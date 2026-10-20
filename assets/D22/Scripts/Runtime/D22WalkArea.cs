using UnityEngine;

namespace D22
{
    public sealed class D22WalkArea : MonoBehaviour
    {
        public Vector2[] perimeter;

        public bool Contains(Vector3 position)
        {
            if (perimeter == null || perimeter.Length < 3) return true;
            bool inside = false;
            for (int i = 0, j = perimeter.Length - 1; i < perimeter.Length; j = i++)
            {
                Vector2 a = perimeter[i], b = perimeter[j];
                if ((a.y > position.z) != (b.y > position.z) &&
                    position.x < (b.x - a.x) * (position.z - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
