using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace D22.Editor
{
    public static class D22RecordShopBuilder
    {
        const string Art = "Assets/D22/Art/RecordShop";
        const string ScenePath = "Assets/D22/Scenes/D22_RecordShop.unity";
        const string ScanPath = "Assets/D22/Scenes/D22_RecordShopScan.unity";
        static Vector3 V(JToken a) => new Vector3((float)a[0],(float)a[1],(float)a[2]);

        [MenuItem("D22/Game/Open Record Shop")]
        public static void OpenRecordShop()
        {
            if(EditorApplication.isPlaying) { D22GameFlow.Instance.LoadSpace("D22_RecordShop"); return; }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(ScenePath);
            var camera=Camera.main;
            if(camera&&SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.LookAt(camera.transform.position+camera.transform.forward*4,camera.transform.rotation,4);
            Selection.activeObject=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        [MenuItem("D22/Publish/Record Shop From Blender")]
        public static void Publish()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before publishing art.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Art+"/Materials");
            AssetDatabase.Refresh();
            var data=JObject.Parse(File.ReadAllText(Art+"/Data/recordshop-export.json"));
            var materials=D22SceneBuilder.BuildMaterials(data,Art+"/Materials");
            foreach(var group in data["groups"].Values<string>())
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/Models/RS_"+group+".fbx");
                foreach(var pair in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
                importer.SaveAndReimport();
            }
            // Preserve the original Gaussian scene before replacing its story-facing entry.
            if(!File.Exists(ScanPath)&&File.Exists(ScenePath))AssetDatabase.CopyAsset(ScenePath,ScanPath);
            bool exists=File.Exists(ScenePath)&&File.ReadAllText(ScenePath).Contains("Record Shop Environment");
            var scene=exists?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var environment=GameObject.Find("Record Shop Environment");
            if(environment)Object.DestroyImmediate(environment);
            environment=new GameObject("Record Shop Environment");
            foreach(var group in data["groups"].Values<string>())
            {
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/RS_"+group+".fbx");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(model);go.transform.SetParent(environment.transform,false);
                go.transform.rotation=Quaternion.Euler(0,180,0)*go.transform.rotation;
            }
            if(!exists)
            {
                CreateCollisions();CreatePlayer();CreateGameplay();CreateLighting(data);
                RenderSettings.ambientMode=AmbientMode.Flat;
                RenderSettings.ambientLight=new Color(.36f,.32f,.26f);
                RenderSettings.ambientIntensity=1;
                RenderSettings.skybox=null;
                var volume=new GameObject("Record Shop Color").AddComponent<Volume>();volume.isGlobal=true;
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();
                string path=Art+"/Materials/RecordShopVolume.asset";
                var existing=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if(existing){Object.DestroyImmediate(profile);profile=existing;}
                else
                {
                    profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
                    var c=profile.Add<ColorAdjustments>(true);c.postExposure.value=.5f;c.saturation.value=-7;
                    profile.Add<Bloom>(true).intensity.value=.08f;
                    AssetDatabase.CreateAsset(profile,path);
                }
                volume.sharedProfile=profile;
            }
            EditorSceneManager.SaveScene(scene,ScenePath);
            D22ProjectSetup.ConfigureBuildScenes();AssetDatabase.SaveAssets();
            Debug.Log("D22_RECORDSHOP_PUBLISHED "+environment.GetComponentsInChildren<Renderer>().Length+" renderers");
        }

        static void Box(Transform parent,string name,Vector3 position,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;
            var c=go.AddComponent<BoxCollider>();c.size=size;
        }
        static void CreateCollisions()
        {
            var root=new GameObject("Record Shop Collision").transform;
            Box(root,"Floor",new Vector3(0,-.075f,5),new Vector3(6.4f,.15f,10));
            Box(root,"Left wall",new Vector3(-3.27f,1.5f,5),new Vector3(.14f,3,10));
            Box(root,"Back wall",new Vector3(0,1.5f,10.07f),new Vector3(6.4f,3,.14f));
            Box(root,"Front boundary",new Vector3(0,1.5f,-.04f),new Vector3(6.4f,3,.1f));
            Box(root,"Right wall",new Vector3(3.27f,1.5f,5),new Vector3(.14f,3,10));
            for(int i=0;i<3;i++)Box(root,"Record bin left",new Vector3(-2.03f,.47f,2.2f+i*1.7f),new Vector3(1.15f,.94f,.7f));
            for(int i=0;i<2;i++)Box(root,"Record bin right",new Vector3(2.03f,.47f,2.4f+i*1.8f),new Vector3(1.15f,.94f,.7f));
            Box(root,"Listening desk",new Vector3(2.3f,.5f,5.3f),new Vector3(1.25f,1,1.3f));
            Box(root,"Counter",new Vector3(0,.51f,8.3f),new Vector3(1.8f,1.02f,.77f));
            Box(root,"Stool",new Vector3(1.48f,.35f,5.5f),new Vector3(.55f,.7f,.55f));
            Box(root,"Rear shelves",new Vector3(0,1.2f,9.6f),new Vector3(4.9f,2.4f,.45f));
        }
        static void CreatePlayer()
        {
            var player=new GameObject("Record Shop Player");player.transform.position=new Vector3(.25f,.03f,.7f);
            var controller=player.AddComponent<CharacterController>();controller.height=1.75f;controller.radius=.23f;controller.center=new Vector3(0,.875f,0);controller.stepOffset=.2f;
            var cam=new GameObject("Record Shop Camera");cam.tag="MainCamera";cam.transform.SetParent(player.transform,false);cam.transform.localPosition=new Vector3(0,1.65f,0);
            var camera=cam.AddComponent<Camera>();camera.nearClipPlane=.04f;camera.farClipPlane=80;camera.fieldOfView=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.065f,.055f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;cam.AddComponent<AudioListener>();
            player.AddComponent<D22Walkthrough>().SetView(cam.transform);
        }
        static void CreateGameplay()
        {
            var root=new GameObject("Record Shop Story Anchors");
            var anchors=root.AddComponent<D22RecordShop>();
            Transform Point(string n,Vector3 p){var o=new GameObject(n).transform;o.SetParent(root.transform);o.position=p;return o;}
            anchors.shopkeeper=Point("Shopkeeper at counter",new Vector3(0,1.45f,8.30f));
            anchors.bottleSpawn=Point("Bottle on counter",new Vector3(.45f,1.07f,7.98f));
            var exit=Point("Side door to hutong",new Vector3(2.8f,1.65f,7.2f)).gameObject.AddComponent<D22Exit>();
            exit.nextScene="D22_Hutong";exit.radius=1.2f;exit.label="走进胡同 / ENTER HUTONG";
        }
        static void CreateLighting(JObject data)
        {
            var root=new GameObject("Record Shop Lighting").transform;
            foreach(var item in data["lights"])
            {
                var go=new GameObject((string)item["name"]);go.transform.SetParent(root);go.transform.position=V(item["position"]);
                go.transform.rotation=Quaternion.LookRotation(V(item["direction"]));
                var light=go.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=145;light.innerSpotAngle=105;light.range=12;
                var color=V(item["color"]);light.color=new Color(color.x,color.y,color.z);
                light.intensity=(float)item["power"]/70f;light.shadows=LightShadows.Soft;light.shadowBias=.025f;light.shadowNormalBias=.15f;
            }
        }
    }
}
