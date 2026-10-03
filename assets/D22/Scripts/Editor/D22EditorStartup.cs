using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace D22.Editor
{
    [InitializeOnLoad]
    public static class D22EditorStartup
    {
        const string MenuPath = "Assets/D22/Scenes/D22_Menu.unity";
        const string StartMenu = "D22/Game/Start Play From Main Menu";
        static string PreferenceKey => "D22.StartFromMenu." + Application.dataPath;

        static D22EditorStartup()
        {
            EditorApplication.playModeStateChanged += BindSfxOnPlay;
            if (!Application.isBatchMode) EditorApplication.delayCall += Initialize;
        }

        static void Initialize()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Initialize;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplyPlayEntry();
            var scene = SceneManager.GetActiveScene();
            // A fresh checkout has an untitled default scene. Keep authored/unsaved work intact.
            if (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(scene.path) && !scene.isDirty)
                D22ProjectSetup.OpenGame();
        }

        static void BindSfxOnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.delayCall += () =>
            {
                var flow = D22GameFlow.Instance;
                if (!flow) return;
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/D22/Music", "Assets/D22/Audio" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (!clip) continue;
                    string name = clip.name.ToLowerInvariant();
                    if (name.Contains("beer")) flow.beerClip = clip;
                    if (name.Contains("cricket")) flow.cricketClip = clip;
                    if (name.Contains("pigeon")) flow.pigeonClip = clip;
                    if (name.Contains("knife") || name.Contains("grind") || name.Contains("scissor")) flow.grindClip = clip;
                }
                flow.BindSfx();
            };
        }

        static void ApplyPlayEntry()
        {
            bool useMenu = EditorPrefs.GetBool(PreferenceKey, true);
            EditorSceneManager.playModeStartScene = useMenu
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) : null;
            Menu.SetChecked(StartMenu, useMenu);
        }

        [MenuItem(StartMenu)]
        static void TogglePlayEntry()
        {
            EditorPrefs.SetBool(PreferenceKey, !EditorPrefs.GetBool(PreferenceKey, true));
            ApplyPlayEntry();
        }

        [MenuItem("D22/Game/Show Scene Files")]
        public static void ShowScenes()
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/D22/Scenes");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }
}
