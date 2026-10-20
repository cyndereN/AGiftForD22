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
    public static class D22RecordShopV3Builder
    {
        const string Art = "Assets/D22/Art/RecordShopV3";
        const string ScenePath = "Assets/D22/Scenes/D22_RecordShopV3.unity";
        static Vector3 V(JToken v) => new Vector3((float)v[0],(float)v[1],(float)v[2]);
        static Vector3 FromBlender(JToken v) { var p=V(v);return new Vector3(p.x,p.z,p.y); }

        [MenuItem("D22/Publish/Record Shop V3 Meshy Only")]
        public static void Publish()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before publishing.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Art+"/Materials");
            AssetDatabase.Refresh();
            var data=JObject.Parse(File.ReadAllText(Art+"/Data/recordshop-v3-export.json"));
            var layout=JObject.Parse(File.ReadAllText(Art+"/Data/layout.json"));
            var materials=D22SceneBuilder.BuildMaterials(data,Art+"/Materials");
            foreach(var material in materials.Values)
            {
                material.SetFloat("_BumpScale",.35f);
                EditorUtility.SetDirty(material);
            }
            foreach(var group in data["groups"].Values<string>())
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/Models/RS3_"+group+".fbx");
                if(importer==null) throw new InvalidOperationException("Missing FBX "+group);
                foreach(var pair in materials) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
                importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var environment=new GameObject("Record Shop V3 - Meshy Assets Only");
            foreach(var group in data["groups"].Values<string>())
            {
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/RS3_"+group+".fbx");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(model);
                go.transform.SetParent(environment.transform,false);
                // A single-mesh FBX carries its offset on the root, unlike a multi-mesh FBX.
                var conversion=Quaternion.Euler(0,180,0);
                go.transform.position=conversion*go.transform.position;
                go.transform.rotation=conversion*go.transform.rotation;
            }
            var collisions=new GameObject("Invisible gameplay collisions").transform;
            Box(collisions,"Floor",new Vector3(0,-.08f,4.1f),new Vector3(4.3f,.16f,8.2f));
            Box(collisions,"Left boundary",new Vector3(-2.2f,1.4f,4.1f),new Vector3(.2f,2.8f,8.4f));
            Box(collisions,"Right boundary",new Vector3(2.2f,1.4f,4.1f),new Vector3(.2f,2.8f,8.4f));
            Box(collisions,"Rear boundary",new Vector3(0,1.4f,8.3f),new Vector3(4.5f,2.8f,.2f));
            Box(collisions,"Front boundary",new Vector3(0,1.4f,-.15f),new Vector3(4.5f,2.8f,.15f));
            foreach(var a in layout["assets"])
                if((bool)a["collision"])
                {
                    var lo=FromBlender(a["bounds_min"]);var hi=FromBlender(a["bounds_max"]);
                    Box(collisions,(string)a["label"],(lo+hi)*.5f,hi-lo);
                }
            var view=layout["camera_views"][0];var cameraPos=FromBlender(view["position"]);
            var player=new GameObject("Record Shop Player");player.transform.position=new Vector3(cameraPos.x,.02f,cameraPos.z);
            var cc=player.AddComponent<CharacterController>();cc.height=1.65f;cc.radius=.20f;cc.center=new Vector3(0,.825f,0);cc.stepOffset=.18f;
            var camObj=new GameObject("Record Shop Camera");camObj.tag="MainCamera";camObj.transform.SetParent(player.transform,false);camObj.transform.localPosition=new Vector3(0,cameraPos.y-.02f,0);
            var cam=camObj.AddComponent<Camera>();cam.nearClipPlane=.03f;cam.farClipPlane=70;cam.fieldOfView=2*Mathf.Atan(12f/(float)view["lens"])*Mathf.Rad2Deg;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.05f,.05f);
            cam.transform.rotation=Quaternion.LookRotation(FromBlender(view["direction"]));cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;camObj.AddComponent<AudioListener>();player.AddComponent<D22Walkthrough>().SetView(cam.transform);
            cam.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cam.GetUniversalAdditionalCameraData().antialiasingQuality=AntialiasingQuality.High;
            var lamps=new GameObject("Record Shop Lighting").transform;
            foreach(var item in data["lights"])
            {
                var go=new GameObject((string)item["name"]);go.transform.SetParent(lamps);go.transform.position=V(item["position"]);go.transform.rotation=Quaternion.LookRotation(V(item["direction"]));
                var l=go.AddComponent<Light>();l.type=LightType.Spot;l.spotAngle=140;l.innerSpotAngle=100;l.range=10;var c=V(item["color"]);l.color=new Color(c.x,c.y,c.z);l.intensity=(float)item["power"]/25f;l.shadows=LightShadows.Soft;l.shadowBias=.025f;l.shadowNormalBias=.025f;l.shadowStrength=.65f;
                if(go.name=="Listening amber lamp")
                {l.type=LightType.Point;l.intensity=1.35f;l.range=3.1f;l.shadows=LightShadows.None;}
                if(go.name=="Front ceiling bounce"||go.name=="Shelf bounce")
                {l.type=LightType.Point;l.shadows=LightShadows.None;}
            }
            var sun=new GameObject("Main light - faint courtyard ambient").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.03f;sun.color=new Color(.65f,.76f,1);sun.transform.rotation=Quaternion.Euler(60,20,0);sun.shadows=LightShadows.None;
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.63f,.59f,.52f);RenderSettings.ambientEquatorColor=new Color(.48f,.445f,.39f);RenderSettings.ambientGroundColor=new Color(.23f,.21f,.18f);RenderSettings.ambientIntensity=1;DynamicGI.UpdateEnvironment();
            var volume=new GameObject("Record Shop V3 tone").AddComponent<Volume>();volume.isGlobal=true;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art+"/Materials/RecordShopV3Volume.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;var color=profile.Add<ColorAdjustments>(true);color.postExposure.value=.25f;color.saturation.value=-8;profile.Add<Bloom>(true).intensity.value=.08f;AssetDatabase.CreateAsset(profile,Art+"/Materials/RecordShopV3Volume.asset");}
            volume.sharedProfile=profile;
            var story=new GameObject("Record Shop story anchors");var anchors=story.AddComponent<D22RecordShop>();
            Transform Point(string n,Vector3 p){var o=new GameObject(n).transform;o.SetParent(story.transform);o.position=p;return o;}
            anchors.shopkeeper=Point("Shopkeeper",new Vector3(-.62f,1.4f,7.55f));anchors.bottleSpawn=Point("Bottle on counter",new Vector3(-.40f,.94f,6.88f));
            anchors.bottlePrefab=BuildMeshyBottle();
            var exit=Point("Door to hutong",new Vector3(1.48f,1.4f,7.84f)).gameObject.AddComponent<D22Exit>();exit.nextScene="D22_Hutong";exit.radius=1;exit.label="走进胡同 / ENTER HUTONG";
            EditorSceneManager.SaveScene(scene,ScenePath);
            string original="Assets/D22/Scenes/D22_RecordShop.unity", backup="Assets/D22/Scenes/D22_RecordShop_PreV3.unity";
            if(File.Exists(original)&&!File.Exists(backup)) AssetDatabase.CopyAsset(original,backup);
            EditorSceneManager.SaveScene(scene,original,true);
            AssetDatabase.SaveAssets();D22ProjectSetup.ConfigureBuildScenes();
            Selection.activeGameObject=environment;
            Debug.Log("D22_RECORDSHOP_V3_PUBLISHED "+environment.GetComponentsInChildren<Renderer>().Length+" Meshy renderers");
        }
        static void Box(Transform parent,string name,Vector3 pos,Vector3 size)
        {var o=new GameObject(name);o.transform.SetParent(parent);o.transform.position=pos;o.AddComponent<BoxCollider>().size=size;}

        static GameObject BuildMeshyBottle()
        {
            var folder=Art+"/Wine";
            var manifest=folder+"/Data/wine-export.json";
            if(!File.Exists(manifest))throw new InvalidOperationException("Export the Meshy bottle before publishing.");
            Directory.CreateDirectory(folder+"/Materials");Directory.CreateDirectory(folder+"/Prefabs");AssetDatabase.Refresh();
            var data=JObject.Parse(File.ReadAllText(manifest));
            var materials=D22SceneBuilder.BuildMaterials(data,folder+"/Materials");
            var modelPath=folder+"/Models/RS3_Wine_Architecture.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            foreach(var pair in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
            importer.SaveAndReimport();
            var root=new GameObject("Meshy record shop beer bottle");
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
                model.transform.SetParent(root.transform,false);
                var conversion=Quaternion.Euler(0,180,0);
                model.transform.localPosition=conversion*model.transform.localPosition;
                model.transform.localRotation=conversion*model.transform.localRotation;
                return PrefabUtility.SaveAsPrefabAsset(root,folder+"/Prefabs/MeshyBeerBottle.prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [MenuItem("D22/Publish/Validate Record Shop V3")]
        public static void Validate()
        {
            var root=GameObject.Find("Record Shop V3 - Meshy Assets Only");
            if(!root)throw new InvalidOperationException("Open the Record Shop V3 scene first.");
            var data=JObject.Parse(File.ReadAllText(Art+"/Data/recordshop-v3-export.json"));
            var layout=JObject.Parse(File.ReadAllText(Art+"/Data/layout.json"));
            var errors=new JArray();var renderers=root.GetComponentsInChildren<Renderer>();
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)errors.Add("Missing script: "+t.name);
            foreach(var r in renderers)
            {
                foreach(var m in r.sharedMaterials)
                    if(!m||!m.shader||!m.shader.isSupported||!m.GetTexture("_BaseMap"))errors.Add("Missing material/texture: "+r.name);
                var mesh=r.GetComponent<MeshFilter>();
                var source=mesh&&mesh.sharedMesh?AssetDatabase.GetAssetPath(mesh.sharedMesh):"";
                if(!source.StartsWith(Art+"/Models/",StringComparison.Ordinal))errors.Add("Untracked mesh: "+r.name);
            }
            // Compare imported bounds with the source, including FBX files containing one mesh.
            foreach(var item in data["objects"])
            {
                var r=renderers.FirstOrDefault(x=>x.name==(string)item["id"]);
                if(!r&&(string)item["group"]=="Fixtures")r=renderers.FirstOrDefault(x=>x.name=="RS3_Fixtures");
                if(!r){errors.Add("Missing imported object: "+item["id"]);continue;}
                if(Vector3.Distance(r.bounds.min,FromBlender(item["bounds_min"]))>.025f||Vector3.Distance(r.bounds.max,FromBlender(item["bounds_max"]))>.025f)
                    errors.Add("Imported bounds differ: "+item["id"]);
            }
            Physics.SyncTransforms();var route=layout["visitor_route_xy"].Select(p=>new Vector3((float)p[0],0,(float)p[1])).ToArray();
            int samples=0;
            for(int i=0;i<route.Length-1;i++)for(int j=0;j<=40;j++)
            {
                var p=Vector3.Lerp(route[i],route[i+1],j/40f);samples++;
                foreach(var collider in Physics.OverlapCapsule(p+Vector3.up*.24f,p+Vector3.up*1.43f,.21f))
                    if(!(collider is CharacterController))errors.Add("Blocked visitor route: "+collider.name);
            }
            var report=new JObject{["scene"]=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,["mesh_renderers"]=renderers.Length,["meshy_source_assets"]=layout["sources"].Count(),["route_capsule_radius_m"]=.21f,["route_samples"]=samples,["errors"]=errors,["passed"]=errors.Count==0};
            var reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../design/recordshop-v3/unity-qa.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            if(errors.Count>0)throw new InvalidOperationException(report.ToString());
            Debug.Log("D22_RECORDSHOP_V3_VALIDATION_PASS "+report.ToString(Newtonsoft.Json.Formatting.None));
        }

        [MenuItem("D22/Publish/Capture Record Shop V3")]
        public static void Capture()
        {
            var cam=Camera.main;if(!cam)throw new InvalidOperationException("No main camera");
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../blender/verification/recordshop-v3/unity.png"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            var old=cam.targetTexture;var oldActive=RenderTexture.active;
            var rt=new RenderTexture(1440,960,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(1440,960,TextureFormat.RGB24,false);
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1440,960),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Debug.Log("D22_V3_CAPTURE "+path);}
            finally{cam.targetTexture=old;RenderTexture.active=oldActive;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
