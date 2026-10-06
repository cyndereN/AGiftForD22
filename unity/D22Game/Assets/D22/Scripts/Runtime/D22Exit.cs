using UnityEngine;
namespace D22
{
    public sealed class D22Exit : MonoBehaviour
    {
        public string nextScene;
        public string label;
        public float radius=2;
        public bool automatic;
        void OnTriggerEnter(Collider other)
        {
            if(automatic && other is CharacterController && D22GameFlow.Instance)
                D22GameFlow.Instance.LoadSpace(nextScene);
        }
        void OnDrawGizmos(){Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(transform.position,radius);}
    }
}
