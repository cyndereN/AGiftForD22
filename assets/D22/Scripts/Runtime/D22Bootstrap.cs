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
        public float Progress { get; private set; }

        static readonly string[] Parts = { "D22_Environment", "D22_Lighting", "D22_Gameplay" };

        IEnumerator Start()
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                var scene = SceneManager.GetSceneByName(Parts[i]);
                if (!scene.isLoaded)
                {
                    var op = SceneManager.LoadSceneAsync(Parts[i], LoadSceneMode.Additive);
                    while (op != null && !op.isDone)
                    {
                        Progress = (i + Mathf.Clamp01(op.progress / 0.9f)) / (Parts.Length + 2f);
                        yield return null;
                    }
                }
                Progress = (i + 1f) / (Parts.Length + 2f);
            }
            var lighting = SceneManager.GetSceneByName("D22_Lighting");
            if (lighting.IsValid() && lighting.isLoaded) SceneManager.SetActiveScene(lighting);
            bool probes = false;
            void Done() => probes = true;
            LightProbes.tetrahedralizationCompleted += Done;
            LightProbes.TetrahedralizeAsync();
            float waited = 0f;
            while (!probes && waited < 8f)
            {
                waited += Time.unscaledDeltaTime;
                Progress = Mathf.Lerp(0.6f, 0.8f, Mathf.Clamp01(waited / 2.5f));
                yield return null;
            }
            LightProbes.tetrahedralizationCompleted -= Done;
            int last = -1;
            int stable = 0;
            waited = 0f;
            while (stable < 10 && waited < 6f)
            {
                waited += Time.unscaledDeltaTime;
                int count = CountRenderers("D22_Environment");
                bool lit = LightmapSettings.lightmaps != null && LightmapSettings.lightmaps.Length > 0;
                stable = count > 0 && count == last && lit ? stable + 1 : 0;
                last = count;
                Progress = Mathf.Lerp(0.8f, 0.98f, Mathf.Clamp01(waited / 1.5f));
                yield return null;
            }
            Progress = 1f;
            yield return new WaitForEndOfFrame();
            Ready = true;
        }

        static int CountRenderers(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded) return 0;
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                if (root) count += root.GetComponentsInChildren<Renderer>(true).Length;
            return count;
        }
    }
}
