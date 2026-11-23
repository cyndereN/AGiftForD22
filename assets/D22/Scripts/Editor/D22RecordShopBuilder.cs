using UnityEditor;
using UnityEditor.SceneManagement;

namespace D22.Editor
{
    public static class D22RecordShopBuilder
    {
        const string ScenePath = "Assets/D22/Scenes/D22_RecordShop.unity";

        [MenuItem("D22/Game/Open Record Shop")]
        public static void OpenRecordShop()
        {
            if (EditorApplication.isPlaying)
            {
                D22GameFlow.Instance.LoadSpace("D22_RecordShop");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            var camera = UnityEngine.Camera.main;
            if (camera && SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.LookAt(camera.transform.position + camera.transform.forward * 4, camera.transform.rotation, 4);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }
    }
}
