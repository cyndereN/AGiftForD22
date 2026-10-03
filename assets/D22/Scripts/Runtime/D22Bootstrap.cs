using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace D22
{
    public sealed class D22Bootstrap : MonoBehaviour
    {
        private IEnumerator Start()
        {
            foreach (var name in new[] { "D22_Environment", "D22_Lighting", "D22_Gameplay" })
            {
                if (!SceneManager.GetSceneByName(name).isLoaded)
                    yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("D22_Lighting"));
            LightProbes.TetrahedralizeAsync();
        }
    }
}
