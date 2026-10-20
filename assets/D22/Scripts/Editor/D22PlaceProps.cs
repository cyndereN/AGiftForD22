using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace D22.Editor
{
    [InitializeOnLoad]
    public static class D22PlaceProps
    {
        const string DoneKey = "D22.StoryPropsPlaced.v1.";
        const string CricketPath = "Assets/D22/Art/Props/Cricket/cricket.obj";
        const string WhetstonePath = "Assets/D22/Art/Props/Whetstone/whetstone_on_wall.obj";
        const string PigeonPath = "Assets/D22/Art/Props/Pigeon/tripo_convert_24c8a29c-a4ea-40f5-b13a-fda1d5340065.fbx";
        const string BassPath = "Assets/D22/Art/Props/Bass/bass.obj";
        static double startedAt = -1;

        static D22PlaceProps()
        {
            if (Application.isBatchMode) return;
            EditorApplication.update += TryAuto;
        }

        static void TryAuto()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetBool(DoneKey + Application.dataPath, false))
            {
                EditorApplication.update -= TryAuto;
                return;
            }
            if (startedAt < 0) startedAt = EditorApplication.timeSinceStartup;
            bool ready = AssetDatabase.LoadAssetAtPath<GameObject>(CricketPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(PigeonPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(BassPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(WhetstonePath) != null;
            if (!ready)
            {
                if (EditorApplication.timeSinceStartup - startedAt > 600) EditorApplication.update -= TryAuto;
                return;
            }
            EditorApplication.update -= TryAuto;
            Place();
            EditorPrefs.SetBool(DoneKey + Application.dataPath, true);
            Debug.Log("Story props placed in the hutong and D-22 scenes.");
        }

        public static void PlaceBatch()
        {
            try
            {
                Place();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("D22/Game/Place Story Props")]
        public static void Place()
        {
            Prepare(CricketPath);
            Prepare(WhetstonePath);
            Prepare(PigeonPath);
            Prepare(BassPath);
            string previous = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            PlaceHutong();
            PlaceLive();
            if (!string.IsNullOrEmpty(previous) && previous != "Assets/D22/Scenes/D22_LiveScan.unity")
                EditorSceneManager.OpenScene(previous);
        }

        static void Prepare(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new System.IO.FileNotFoundException(path);
            importer.indexFormat = ModelImporterIndexFormat.UInt32;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();
        }

        static void PlaceHutong()
        {
            var scene = EditorSceneManager.OpenScene("Assets/D22/Scenes/D22_Hutong.unity", OpenSceneMode.Single);
            var root = FreshRoot("Story Props");
            var cricket = Spawn(CricketPath, root.transform, new Vector3(0f, 1f, 2f), new Vector3(0f, 20f, 0f), 0.22f);
            cricket.name = "Cricket";
            var stone = Spawn(WhetstonePath, root.transform, new Vector3(1.5f, 1f, 12.5f), new Vector3(0f, 90f, 0f), 1.1f);
            stone.name = "Whetstone";
            var flock = new GameObject("Pigeon Flock");
            flock.transform.SetParent(root.transform, false);
            flock.transform.position = new Vector3(9f, 3.2f, 21f);
            flock.AddComponent<D22PigeonFlock>();
            Vector3[] offsets =
            {
                new Vector3(0f, .15f, 0f),
                new Vector3(1.15f, .45f, .35f),
                new Vector3(-1f, .05f, .7f),
                new Vector3(.45f, .7f, -1.05f),
                new Vector3(-.55f, -.15f, -.85f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                var bird = Spawn(PigeonPath, flock.transform, offsets[i], new Vector3(0f, i * 47f, 0f), .4f);
                bird.name = "Pigeon " + (i + 1);
            }
            root.AddComponent<D22HutongProps>();
            root.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void PlaceLive()
        {
            var scene = EditorSceneManager.OpenScene("Assets/D22/Scenes/D22_LiveScan.unity", OpenSceneMode.Single);
            var root = FreshRoot("Story Props");
            var bass = Spawn(BassPath, root.transform, new Vector3(1.1f, .7f, -15.6f), new Vector3(0f, 160f, 0f), 1.15f);
            bass.name = "Bass";
            var over = root.AddComponent<D22OverSplat>();
            over.shader = Shader.Find("D22/Over Splat");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static GameObject FreshRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing) Object.DestroyImmediate(existing);
            return new GameObject(name);
        }

        static GameObject Spawn(string path, Transform parent, Vector3 localPos, Vector3 euler, float targetSize)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new System.IO.FileNotFoundException(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (max > .001f) go.transform.localScale = Vector3.one * (targetSize / max);
            }
            return go;
        }
    }
}
