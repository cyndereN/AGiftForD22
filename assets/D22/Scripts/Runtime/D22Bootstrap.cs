using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace D22
{
    public sealed class D22Bootstrap : MonoBehaviour
    {
        public const string SceneName = "D22_Bootstrap";

        [Header("Livehouse stage interaction positions")]
        public Vector3 introSpot = new Vector3(0, 0, 1);
        public float introRadius = 2.2f;
        public Vector3 stageSpot = new Vector3(0, 0, 2.8f);
        public float stageRadius = 1.4f;
        public Vector3 drumSpot = new Vector3(0, 1, 3.9f);
        public Vector3 guitarSpot = new Vector3(1.5f, 1, 3.5f);
        public Vector3 bassSpot = new Vector3(-1.5f, 1, 3.5f);

        public bool Ready { get; private set; }

        private IEnumerator Start()
        {
            foreach (var name in new[] { "D22_Environment", "D22_Lighting", "D22_Gameplay" })
            {
                if (!SceneManager.GetSceneByName(name).isLoaded)
                    yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("D22_Lighting"));
            LightProbes.TetrahedralizeAsync();
            Ready = true;
        }
    }
}
