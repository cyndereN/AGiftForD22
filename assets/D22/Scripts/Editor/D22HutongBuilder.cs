using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace D22.Editor
{
    public static class D22HutongBuilder
    {
        const string Art = "Assets/D22/Art/Hutong";
        const string ScenePath = "Assets/D22/Scenes/D22_Hutong.unity";
        const string ScanPath = "Assets/D22/Scenes/D22_HutongScan.unity";

        static Vector3 Vec(JToken value) => new Vector3((float)value[0], (float)value[1], (float)value[2]);

        [MenuItem("D22/Publish/Hutong V4 From Blender")]
        public static void Publish()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before publishing the hutong.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(Art + "/Materials");
            AssetDatabase.Refresh();
            var data = JObject.Parse(File.ReadAllText(Art + "/Data/hutong-export.json"));
            var materials = D22SceneBuilder.BuildMaterials(data, Art + "/Materials");
            TileGroundMaterial();
            foreach (string group in data["groups"].Values<string>())
            {
                string path = Art + "/Models/HT_" + group + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Missing Hutong FBX: " + path);
                foreach (var pair in materials)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
                importer.SaveAndReimport();
            }

            // Keep the original scan playable without changing the story scene's GUID.
            if (!File.Exists(ScanPath) && File.Exists(ScenePath) && File.ReadAllText(ScenePath).Contains("GaussianSplatRenderer"))
            {
                if (!AssetDatabase.CopyAsset(ScenePath, ScanPath))
                    throw new InvalidOperationException("Could not preserve the original hutong scan.");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = new GameObject("Hutong V4 Environment");
            var sourceNames = data["objects"].ToDictionary(o => (string)o["id"], o => (string)o["source_name"]);
            foreach (string group in data["groups"].Values<string>())
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Models/HT_" + group + ".fbx");
                if (!model) throw new InvalidOperationException("Hutong model did not import: " + group);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "HT_" + group;
                instance.transform.SetParent(environment.transform, false);
                var conversion = Quaternion.Euler(0, 180, 0);
                instance.transform.localPosition = conversion * instance.transform.localPosition;
                instance.transform.localRotation = conversion * instance.transform.localRotation;
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    if (!sourceNames.TryGetValue(renderer.name, out string sourceName)) continue;
                    string name = sourceName.ToLowerInvariant();
                    bool solid = name.Contains("ground substrate") || name.Contains("wall") || name.Contains("pier") ||
                                 name.Contains("street face") || name.Contains("return") || name.Contains("back") ||
                                 name.Contains("gable") || name.Contains("step") || name.Contains("door timber") ||
                                 name.Contains("jamb") || name.Contains("gate") || name.Contains("fence") ||
                                 name.Contains("bicycle") || name.Contains("table") || name.Contains("cart") ||
                                 name.Contains("bucket") || name.Contains("air conditioner") || name.Contains("meter cabinet");
                    if (solid && renderer.GetComponent<MeshFilter>() && !renderer.GetComponent<Collider>())
                        renderer.gameObject.AddComponent<MeshCollider>();
                }
            }

            var player = new GameObject("Hutong Player");
            player.transform.position = new Vector3(0, .03f, -2.7f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.75f; controller.radius = .24f;
            controller.center = new Vector3(0, .875f, 0); controller.stepOffset = .22f;
            var cameraObject = new GameObject("Hutong Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.62f, 0);
            player.transform.rotation = Quaternion.LookRotation(new Vector3(.37f, 0, 10.4f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .04f; camera.farClipPlane = 160; camera.fieldOfView = 60;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.48f, .56f, .65f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cameraObject.AddComponent<AudioListener>();
            player.AddComponent<D22Walkthrough>().SetView(cameraObject.transform);
            SetWalkArea(player);

            var exit = new GameObject("Livehouse Entrance").AddComponent<D22Exit>();
            exit.transform.position = new Vector3(5.5f, 1.4f, 19.1f);
            exit.radius = 1.25f;
            exit.nextScene = "D22_Bootstrap";
            exit.label = "走进现场 / ENTER LIVE HOUSE";

            SealPlayablePerimeter();

            var timeOfDay = new GameObject("Hutong Time Of Day").AddComponent<D22HutongLighting>();
            var practicals = new List<Light>();

            foreach (var item in data["lights"])
            {
                var lightObject = new GameObject((string)item["name"]);
                lightObject.transform.position = Vec(item["position"]);
                lightObject.transform.rotation = Quaternion.LookRotation(Vec(item["direction"]));
                var light = lightObject.AddComponent<Light>();
                string type = (string)item["type"];
                light.type = type == "SUN" ? LightType.Directional : type == "SPOT" ? LightType.Spot : LightType.Point;
                var color = Vec(item["color"]);
                light.color = new Color(color.x, color.y, color.z);
                light.intensity = type == "SUN" ? 1.1f : (float)item["power"] / 180f;
                light.range = type == "SUN" ? 0 : 9;
                light.shadows = type == "SUN" ? LightShadows.Soft : LightShadows.None;
                if (type == "SPOT") light.spotAngle = (float)item["angle"];
                if(type=="SUN")timeOfDay.sun=light;
                else if(lightObject.name.Contains("threshold") || lightObject.name.Contains("bulb") || lightObject.name.Contains("doorway"))practicals.Add(light);
            }
            timeOfDay.practicals=practicals.ToArray();
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.46f, .52f, .60f);
            RenderSettings.ambientEquatorColor = new Color(.32f, .34f, .36f);
            RenderSettings.ambientGroundColor = new Color(.17f, .17f, .16f);
            RenderSettings.ambientIntensity = 1;

            EditorSceneManager.SaveScene(scene, ScenePath);
            D22ProjectSetup.ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("D22_HUTONG_PUBLISHED " + environment.GetComponentsInChildren<MeshRenderer>().Length + " renderers");
        }

        [MenuItem("D22/Publish/Seal Hutong Perimeter")]
        public static void SealPlayablePerimeterInScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing the hutong.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SealPlayablePerimeter();
            TileGroundMaterial();
            AssetDatabase.SaveAssets();
            var player = GameObject.Find("Hutong Player");
            if (!player) throw new InvalidOperationException("Hutong Player is missing.");
            SetWalkArea(player);
            EditorSceneManager.SaveScene(scene);
            Validate();
        }

        static void SetWalkArea(GameObject player)
        {
            var area = player.GetComponent<D22WalkArea>();
            if (!area) area = player.AddComponent<D22WalkArea>();
            area.perimeter = new[]
            {
                new Vector2(-5.6f, -4.35f), new Vector2(6.5f, -4.35f),
                new Vector2(6.5f, 16.1f), new Vector2(13.1f, 16.1f),
                new Vector2(13.1f, 18.75f), new Vector2(14.8f, 18.75f),
                new Vector2(14.8f, 26.15f), new Vector2(10.65f, 26.15f),
                new Vector2(10.65f, 22.85f), new Vector2(-5.6f, 22.85f)
            };
        }

        static void SealPlayablePerimeter()
        {
            // The Blender ground extends beyond the dressed lane. These joined edges
            // enclose the street and the eastward turn without closing the venue door.
            AddBoundary("South end", new Vector3(.45f, 1.5f, -4.35f), new Vector3(12.3f, 3, .3f));
            AddBoundary("West back", new Vector3(-5.6f, 1.5f, 9.25f), new Vector3(.3f, 3, 27.5f));
            AddBoundary("East back", new Vector3(6.5f, 1.5f, 5.9f), new Vector3(.3f, 3, 20.5f));
            AddBoundary("Workshop back", new Vector3(9.8f, 1.5f, 16.1f), new Vector3(6.9f, 3, .3f));
            AddBoundary("Workshop side", new Vector3(13.1f, 1.5f, 17.45f), new Vector3(.3f, 3, 2.9f));
            AddBoundary("Turn return", new Vector3(13.95f, 1.5f, 18.75f), new Vector3(2.1f, 3, .3f));
            AddBoundary("North back", new Vector3(2.45f, 1.5f, 22.85f), new Vector3(16.4f, 3, .3f));
            AddBoundary("North end", new Vector3(12.8f, 1.5f, 26.15f), new Vector3(4.3f, 3, .3f));
            AddBoundary("East end", new Vector3(14.8f, 1.5f, 22.45f), new Vector3(.3f, 3, 7.7f));
            AddBoundary("Livehouse door", new Vector3(5.5f, 1.05f, 19.44f), new Vector3(1.08f, 2.1f, .15f));

            // Visible masonry closes the two gaps where a player could see bare ground
            // behind the authored buildings, even before reaching a collision edge.
            AddEndWall("West end wall return", new Vector3(-3.7f, 1.42f, 22.8f), new Vector3(3.9f, 2.84f, .28f),
                "Grey_handmade_masonry_scanned_PBR_2f5f94df");
            AddEndWall("East turn wall return", new Vector3(13.8f, 1.25f, 18.75f), new Vector3(1.8f, 2.5f, .28f),
                "Repair_render_scanned_PBR_064830ca");
            AddEndWall("North east corner return", new Vector3(14.7f, 1.43f, 25.65f), new Vector3(.34f, 2.86f, 1.1f),
                "Old_lime_plaster_scanned_PBR_1016b979");
            AddEndWall("South end wall", new Vector3(.45f, 1.42f, -6.1f), new Vector3(12.3f, 2.84f, .28f),
                "South_end_wall_plaster");
        }

        static void TileGroundMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Concrete_lane_scanned_PBR_1dccb5f3.mat");
            if (!material) throw new InvalidOperationException("Missing hutong ground material.");
            var scale = new Vector2(16, 20);
            foreach (var property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap" })
                material.SetTextureScale(property, scale);
            EditorUtility.SetDirty(material);
        }

        static Material SouthEndWallMaterial()
        {
            const string path = Art + "/Materials/South_end_wall_plaster.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Old_lime_plaster_scanned_PBR_1016b979.mat");
                if (!source) throw new InvalidOperationException("Missing hutong masonry material.");
                material = new Material(source) { name = "South_end_wall_plaster" };
                AssetDatabase.CreateAsset(material, path);
            }
            foreach (var property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap" })
                material.SetTextureScale(property, new Vector2(3, 1));
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(.28f, .29f, .30f));
            EditorUtility.SetDirty(material);
            return material;
        }

        static void AddBoundary(string name, Vector3 center, Vector3 size)
        {
            var boundary=GameObject.Find("Hutong Boundary / "+name);
            if (!boundary) boundary = new GameObject("Hutong Boundary / "+name);
            boundary.transform.position=center;
            var collider=boundary.GetComponent<BoxCollider>();
            if (!collider) collider = boundary.AddComponent<BoxCollider>();
            collider.size=size;
            collider.isTrigger=false;
        }

        static void AddEndWall(string name, Vector3 center, Vector3 size, string materialName)
        {
            var wall=GameObject.Find("Hutong Closure / "+name) ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name="Hutong Closure / "+name;
            wall.transform.position=center;
            wall.transform.localScale=size;
            var material=materialName=="South_end_wall_plaster"
                ? SouthEndWallMaterial()
                : AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+materialName+".mat");
            if (!material) throw new InvalidOperationException("Missing hutong wall material: "+materialName);
            wall.GetComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        [MenuItem("D22/Publish/Validate Hutong V4")]
        public static void Validate()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the 3D hutong scene before validation.");
            var data = JObject.Parse(File.ReadAllText(Art + "/Data/hutong-export.json"));
            var renderers = Object.FindObjectsByType<MeshRenderer>();
            var exit = Object.FindAnyObjectByType<D22Exit>();
            var camera = Camera.main;
            var walkArea = Object.FindAnyObjectByType<D22WalkArea>();
            var boundaries = Object.FindObjectsByType<BoxCollider>().Where(c => c.name.StartsWith("Hutong Boundary / ")).ToArray();
            var closures = renderers.Where(r => r.name.StartsWith("Hutong Closure / ")).ToArray();
            var expectedGeometry = new HashSet<string>(data["objects"].Select(o => (string)o["id"]));
            var environment = GameObject.Find("Hutong V4 Environment");
            var importedGeometry = environment ? environment.GetComponentsInChildren<MeshRenderer>() : new MeshRenderer[0];
            bool geometry = expectedGeometry.All(id => importedGeometry.Any(r => r.name == id));
            bool perimeter = walkArea && boundaries.Length == 10 && closures.Length == 4 &&
                             walkArea.Contains(new Vector3(0, 0, -2.7f)) &&
                             walkArea.Contains(new Vector3(5.5f, 0, 20.2f)) &&
                             walkArea.Contains(new Vector3(13.5f, 0, 24.6f)) &&
                             !walkArea.Contains(new Vector3(0, 0, 27f)) &&
                             !walkArea.Contains(new Vector3(18f, 0, 21f)) &&
                             !walkArea.Contains(new Vector3(0, 0, -6f));
            Physics.SyncTransforms();
            var floorHits = Physics.RaycastAll(new Vector3(0, 1, -2.7f), Vector3.down, 2)
                .Where(h => h.collider.gameObject.scene == scene && !(h.collider is CharacterController))
                .OrderBy(h => h.distance).ToArray();
            bool floor = floorHits.Length > 0;
            float floorHeight = floor ? floorHits[0].point.y : float.NaN;
            var door = GameObject.Find("Hutong Boundary / Livehouse door");
            bool doorBlocked = door && door.GetComponent<BoxCollider>() &&
                               Physics.Raycast(new Vector3(5.5f, 1.2f, 18.2f), Vector3.forward, out var doorHit, 2) &&
                               doorHit.collider.gameObject == door;
            bool valid = geometry && floor && perimeter && doorBlocked &&
                         Mathf.Abs(floorHeight) < .3f &&
                         camera && camera.GetComponent<AudioListener>() && camera.GetComponentInParent<D22Walkthrough>() &&
                         exit && exit.nextScene == "D22_Bootstrap" &&
                         !Object.FindAnyObjectByType<GaussianSplatting.Runtime.GaussianSplatRenderer>();
            if (!valid)
                throw new InvalidOperationException($"Hutong validation failed: geometry={geometry}, renderers={renderers.Length}, perimeter={perimeter}, doorBlocked={doorBlocked}, boundaries={boundaries.Length}, closures={closures.Length}, floor={floor}, floorHeight={floorHeight:F3}, camera={camera}, exit={exit}");
            Debug.Log($"D22_HUTONG_VALIDATION_PASS geometry={expectedGeometry.Count} renderers={renderers.Length} boundaries={boundaries.Length} closures={closures.Length} colliders={Object.FindObjectsByType<Collider>().Length} floor={floorHeight:F2} exit={exit.nextScene}");
        }
    }
}
