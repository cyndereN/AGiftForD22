using System;
using System.Collections.Generic;
using System.IO;
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
    public static class D22SceneBuilder
    {
        const string Root = "Assets/D22";
        const string Scenes = Root + "/Scenes/";
        static readonly string[] SceneNames = { "D22_Bootstrap", "D22_Environment", "D22_Lighting", "D22_Gameplay" };
        static JObject Data => JObject.Parse(File.ReadAllText(Root + "/Art/Data/d22-export.json"));
        static Vector3 Vec(JToken a) => new Vector3((float)a[0], (float)a[1], (float)a[2]);
        static Color Col(JToken a) => new Color((float)a[0], (float)a[1], (float)a[2], a.Count() > 3 ? (float)a[3] : 1);

        [MenuItem("D22/Publish/Update Art From Blender")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var folder in new[] { "Materials", "Prefabs", "Scenes", "Settings", "Validation" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            UnityEditor.VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.companyName = "D22 Team";
            PlayerSettings.productName = "D22 Livehouse";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            ConfigurePipeline();
            var data = Data;
            var materials = BuildMaterials(data);
            foreach (string group in data["groups"].Values<string>())
            {
                string path = Root + "/Art/Models/D22_" + group + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                foreach (var pair in materials)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
                importer.SaveAndReimport();
            }
            BuildEnvironment(data);
            if (!File.Exists(Scenes + "D22_Lighting.unity")) BuildLighting(data);
            // Gameplay is authored independently and never replaced on an art republish.
            if (!File.Exists(Scenes + "D22_Gameplay.unity")) BuildGameplay();
            if (!File.Exists(Scenes + "D22_Bootstrap.unity"))
            {
                var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("D22 Scene Loader").AddComponent<D22Bootstrap>();
                EditorSceneManager.SaveScene(boot, Scenes + "D22_Bootstrap.unity");
            }
            D22ProjectSetup.ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            OpenWorkspace();
            Validate();
            Debug.Log("D22_PUBLISH_COMPLETE");
        }

        static void ConfigurePipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            pipeline.shadowDistance = 22;
            pipeline.maxAdditionalLightsCount = 8;
            pipeline.additionalLightsShadowmapResolution = 4096;
            EditorUtility.SetDirty(pipeline);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var serialized = new SerializedObject(renderer);
            serialized.FindProperty("m_RenderingMode").intValue = 2; // Forward+ in URP 17.6
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Dictionary<string, Material> BuildMaterials(JObject data, string materialFolder = Root + "/Materials")
        {
            var materials = new Dictionary<string, Material>();
            foreach (var item in data["materials"])
            {
                string id = (string)item["id"], path = materialFolder + "/" + id + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
                material.name = id;
                material.shaderKeywords = Array.Empty<string>();
                foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" }) material.SetTexture(property, null);
                material.SetFloat("_Surface", 0); material.SetFloat("_ZWrite", 1); material.SetFloat("_AlphaClip", 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.One); material.SetFloat("_DstBlend", (float)BlendMode.Zero); material.renderQueue = -1;
                material.SetColor("_EmissionColor", Color.black); material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                material.SetColor("_BaseColor", Col(item["base_color"]).gamma);
                material.SetFloat("_Metallic", (float)item["metallic"]);
                material.SetFloat("_Smoothness", 1 - (float)item["roughness"]);
                material.SetFloat("_Cull", 0); // Source contains thin paper and imported single-sided surfaces.
                material.enableInstancing = true;
                SetTexture(material, "_BaseMap", (string)item["base_map"]);
                string normal = (string)item["normal_map"];
                if (!string.IsNullOrEmpty(normal))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(normal);
                    if (importer.textureType != TextureImporterType.NormalMap)
                    { importer.textureType = TextureImporterType.NormalMap; importer.sRGBTexture = false; importer.SaveAndReimport(); }
                    SetTexture(material, "_BumpMap", normal); material.EnableKeyword("_NORMALMAP");
                    material.SetFloat("_BumpScale", (float)item["normal_scale"]);
                }
                string metallic = (string)item["metallic_map"];
                if (!string.IsNullOrEmpty(metallic))
                { SetTexture(material, "_MetallicGlossMap", metallic); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Smoothness", 1); }
                if ((float)item["emission_strength"] > 0)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Col(item["emission"]) * (float)item["emission_strength"]);
                    SetTexture(material, "_EmissionMap", (string)item["emission_map"]);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                if ((bool)item["alpha_clip"])
                { material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .45f); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = 2450; }
                if ((bool)item["transparent"])
                {
                    material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                    Color c=material.GetColor("_BaseColor"); c.a=.26f; material.SetColor("_BaseColor",c);
                }
                EditorUtility.SetDirty(material); materials.Add(id, material);
            }
            AssetDatabase.SaveAssets();
            return materials;
        }

        static void SetTexture(Material material, string property, string path)
        { if (!string.IsNullOrEmpty(path)) material.SetTexture(property, AssetDatabase.LoadAssetAtPath<Texture2D>(path)); }

        static void BuildEnvironment(JObject data)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sources = data["objects"].ToDictionary(o => (string)o["id"]);
            foreach (string group in data["groups"].Values<string>())
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Models/D22_" + group + ".fbx");
                if (!model) throw new InvalidOperationException("Missing model: " + group);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "D22_" + group;
                // Blender -Z-forward FBX imports with X/Z reversed; align to the light manifest.
                instance.transform.rotation = Quaternion.Euler(0, 180, 0) * instance.transform.rotation;
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    renderer.receiveGI = ReceiveGI.Lightmaps;
                    renderer.scaleInLightmap = group == "Architecture" ? .7f : .35f;
                    if (sources.TryGetValue(renderer.gameObject.name, out var source) && group == "Architecture")
                    {
                        string name = ((string)source["source_name"]).ToLowerInvariant();
                        bool solid = new[] { "floor", "deck", "stair", "wall", "pier", "riser", "fascia", "bearing", "tread" }.Any(name.Contains);
                        if (solid && !name.Contains("brick") && !name.Contains("photo") && !name.Contains("notice"))
                            renderer.gameObject.AddComponent<MeshCollider>();
                    }
                }
                string prefabPath = Root + "/Prefabs/D22_" + group + ".prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
            }
            EditorSceneManager.SaveScene(scene, Scenes + "D22_Environment.unity");
        }

        static void BuildLighting(JObject data)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var item in data["lights"])
            {
                var obj = new GameObject((string)item["name"]);
                obj.transform.position = Vec(item["position"]);
                obj.transform.rotation = Quaternion.LookRotation(Vec(item["direction"]), Vector3.up);
                var light = obj.AddComponent<Light>();
                string type = (string)item["type"];
                light.type = type == "SPOT" ? LightType.Spot : type == "AREA" ? LightType.Rectangle : LightType.Point;
                light.color = Col(item["color"]).gamma;
                light.range = ((string)item["name"]).StartsWith("L0") ? 9 : 5;
                light.intensity = type == "SPOT" ? (float)item["power"] / 30f : type == "AREA" ? (float)item["power"] / 12f : (float)item["power"] / 2f;
                light.bounceIntensity = 1.3f;
                light.spotAngle = Mathf.Clamp((float)item["angle"],1,175);
                light.innerSpotAngle = light.spotAngle * (1 - (float)item["blend"]*.65f);
                light.areaSize = new Vector2((float)item["size"], (float)item["size_y"]);
                light.shadowBias = .015f; light.shadowNormalBias = .035f; light.shadowNearPlane = .03f;
                light.shadows = LightShadows.Soft;
                light.lightmapBakeType = type == "AREA" || !((string)item["name"]).StartsWith("L0") ? LightmapBakeType.Baked : LightmapBakeType.Mixed;
            }
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.09f,.095f,.11f);
            RenderSettings.ambientEquatorColor = new Color(.06f,.045f,.035f);
            RenderSettings.ambientGroundColor = new Color(.025f,.022f,.02f);
            RenderSettings.ambientIntensity = 1;
            var settings = new LightingSettings { name = "D22 Lighting Settings", bakedGI = true, realtimeGI = false, lightmapResolution = 12, lightmapMaxSize = 2048, indirectSampleCount = 64, directSampleCount = 32, environmentSampleCount = 32, maxBounces = 3 };
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
            settings.lightmapper = LightingSettings.Lightmapper.UnityComputeGPU;
            string settingsPath = Root + "/Settings/D22Lighting.asset";
            if (File.Exists(settingsPath)) { var old = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath); EditorUtility.CopySerialized(settings,old); Object.DestroyImmediate(settings); settings=old; }
            else AssetDatabase.CreateAsset(settings, settingsPath);
            Lightmapping.lightingSettings = settings;
            var probe = new GameObject("D22 Room Reflections").AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(0,2,0); probe.size = new Vector3(8,6,15); probe.boxProjection = true; probe.resolution=128; probe.mode=ReflectionProbeMode.Baked;
            var probes = new GameObject("D22 Walkable Light Probes").AddComponent<LightProbeGroup>();
            var positions = new List<Vector3>();
            for (float z=-5;z<=6;z+=1.5f) for(float x=-2.5f;x<=2.5f;x+=1.25f) foreach(float y in new[]{.5f,1.7f,3.3f}) positions.Add(new Vector3(x,y,z));
            probes.probePositions=positions.ToArray();
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Settings/D22Volume.asset");
            if (!profile) { profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,Root+"/Settings/D22Volume.asset"); }
            if(!profile.TryGet<Tonemapping>(out var tonemapping)) tonemapping=profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var color)) color=profile.Add<ColorAdjustments>();
            color.postExposure.Override(.7f);
            var volume = new GameObject("D22 Color Grade").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            EditorUtility.SetDirty(profile);
            EditorSceneManager.SaveScene(scene, Scenes+"D22_Lighting.unity");
        }

        static void BuildGameplay()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var player=new GameObject("Walkthrough Player");player.transform.position=new Vector3(.6f,.2f,-2.5f);
            var controller=player.AddComponent<CharacterController>();controller.height=1.75f;controller.radius=.24f;controller.center=new Vector3(0,.9f,0);controller.stepOffset=.22f;controller.skinWidth=.025f;
            var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";cameraObject.transform.SetParent(player.transform,false);cameraObject.transform.localPosition=new Vector3(0,1.6f,0);
            var camera=cameraObject.AddComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.fieldOfView=65;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.03f);
            cameraObject.AddComponent<AudioListener>();cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
            player.AddComponent<D22Walkthrough>().SetView(cameraObject.transform);
            EditorSceneManager.SaveScene(scene,Scenes+"D22_Gameplay.unity");
        }

        [MenuItem("D22/Open Collaborative Workspace")]
        public static void OpenWorkspace()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Scenes+"D22_Bootstrap.unity",OpenSceneMode.Single);
            foreach(string name in SceneNames.Skip(1))EditorSceneManager.OpenScene(Scenes+name+".unity",OpenSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("D22_Lighting"));
            EditorSceneManager.SaveOpenScenes();
            if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1.8f,3),Quaternion.Euler(8,0,0),9);
        }

        [MenuItem("D22/Validate Published Scene")]
        public static void Validate()
        {
            var meshes=Object.FindObjectsByType<MeshRenderer>();
            var missing=meshes.Where(r=>r.sharedMaterials.Any(m=>!m||!m.shader||m.shader.name.Contains("Error"))).Select(r=>r.name).ToArray();
            var report=new JObject { ["unity_version"]=Application.unityVersion,["renderer_count"]=meshes.Length,["missing_materials"]=new JArray(missing),["lights"]=Object.FindObjectsByType<Light>().Length,["colliders"]=Object.FindObjectsByType<Collider>().Length,["lightmaps"]=LightmapSettings.lightmaps.Length,["scene_count"]=SceneManager.sceneCount };
            var sources=Data["objects"].ToDictionary(o=>(string)o["id"]);
            float maxError=0; int matched=0; var placementErrors=new JArray();
            foreach(var renderer in meshes)
            {
                if(!sources.TryGetValue(renderer.name,out var source))continue;
                matched++;
                Vector3 b=(Vec(source["bounds_min"])+Vec(source["bounds_max"]))*.5f;
                Vector3 expected=new Vector3(b.x,b.z,b.y);
                float error=Vector3.Distance(expected,renderer.bounds.center);maxError=Mathf.Max(maxError,error);
                if(error>.08f && placementErrors.Count<12)placementErrors.Add(new JObject { ["object"]=renderer.name,["error_m"]=error,["expected"]=new JArray(expected.x,expected.y,expected.z),["actual"]=new JArray(renderer.bounds.center.x,renderer.bounds.center.y,renderer.bounds.center.z) });
            }
            report["matched_source_objects"]=matched;
            report["max_object_center_error_m"]=maxError;report["placement_errors"]=placementErrors;
            Physics.SyncTransforms();
            var floorHits=Physics.RaycastAll(new Vector3(.6f,1,-2.5f),Vector3.down,2)
                .Where(h=>h.collider.gameObject.scene.name=="D22_Environment").OrderBy(h=>h.distance).ToArray();
            bool floor=floorHits.Length>0;
            var floorHit=floor?floorHits[0]:default;
            report["spawn_floor_hit"]=floor;report["spawn_floor_height_m"]=floor?floorHit.point.y:0;
            File.WriteAllText(Root+"/Validation/migration-report.json",report.ToString());
            if(missing.Length>0 || meshes.Length!= (int)Data["geometry_count"] || matched!=sources.Count || placementErrors.Count>0 || !floor || Mathf.Abs(floorHit.point.y)>.3f)throw new InvalidOperationException("D22 migration validation failed: "+report);
            Debug.Log("D22_VALIDATION "+report.ToString(Newtonsoft.Json.Formatting.None));
        }

        [MenuItem("D22/Bake Lighting")]
        public static void Bake()
        { OpenWorkspace(); Lightmapping.Bake(); EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets(); Validate(); }

        [MenuItem("D22/Publish/Reset Lighting From Blender")]
        public static void ResetLighting()
        {
            if(!Application.isBatchMode && !EditorUtility.DisplayDialog("Reset D22 lighting", "This replaces Unity lighting edits with the Blender export. Commit or back up lighting changes first.", "Reset", "Cancel"))return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildLighting(Data);AssetDatabase.SaveAssets();OpenWorkspace();
        }
    }
}
