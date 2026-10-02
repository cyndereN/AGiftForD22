using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GaussianSplatting.Runtime;
using GaussianSplatting.Editor;

namespace D22.Editor
{
    public static class D22ProjectSetup
    {
        const string Root="Assets/D22";
        const string Scenes=Root+"/Scenes/";
        public static readonly string[] GameScenes={"D22_Menu","D22_RecordShop","D22_Hutong","D22_LiveScan","D22_Performance","D22_Bootstrap","D22_Environment","D22_Lighting","D22_Gameplay"};
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] ScanKeys={"recordshop","hutong","live","performance"};
        static readonly string[] ScanScenes={"D22_RecordShop","D22_Hutong","D22_LiveScan","D22_Performance"};

        [MenuItem("D22/Game/Import Scans From Converted PLY")]
        public static void ImportScans()
        {
            string repo=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.."));
            foreach(string key in ScanKeys)
            {
                string input=Path.Combine(repo,$"work/scans/scene_{key}.ply");
                if(!File.Exists(input))throw new FileNotFoundException("Run node scripts/scans/convert.cjs first",input);
                string output=$"{Root}/Scans/{key}";
                if(AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>($"{output}/scene_{key}.asset"))continue;
                var type=typeof(GaussianSplatAssetCreator);
                var window=ScriptableObject.CreateInstance<GaussianSplatAssetCreator>();
                try
                {
                    void Set(string name,object value)=>type.GetField(name,Private).SetValue(window,value);
                    Set("m_InputFile",input);Set("m_OutputFolder",output);Set("m_ImportCameras",false);
                    Set("m_Quality",Enum.Parse(type.GetNestedType("DataQuality",BindingFlags.NonPublic),"High"));
                    type.GetMethod("ApplyQualityLevel",Private).Invoke(window,null);
                    // SOG sources contain only DC color. Do not cluster SH, which would add a costly unnecessary k-means pass.
                    Set("m_FormatColor",GaussianSplatAsset.ColorFormat.Norm8x4);
                    type.GetMethod("CreateAsset",Private).Invoke(window,null);
                    if(!AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>($"{output}/scene_{key}.asset"))throw new InvalidOperationException("Scan import failed: "+key);
                }
                finally{UnityEngine.Object.DestroyImmediate(window);EditorUtility.ClearProgressBar();}
            }
            Setup();
        }
        [MenuItem("D22/Game/Generate Initial Game Scenes")]
        public static void Setup()
        {
            ConfigureRendering();SetupWine();
            for(int i=0;i<ScanKeys.Length;i++)CreateScan(i);
            CreateMenu();
            ConfigureBuildScenes();
            PlayerSettings.productName="A Gift for D22";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
            AssetDatabase.SaveAssets();OpenGame();Debug.Log("D22_GAME_SETUP_COMPLETE");
        }
        static void ConfigureRendering()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D12});
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{GraphicsDeviceType.Metal});
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");pipeline.msaaSampleCount=1;EditorUtility.SetDirty(pipeline);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var type=typeof(GaussianSplatRenderer).Assembly.GetType("GaussianSplatting.Runtime.GaussianSplatURPFeature",true);
            if(!renderer.rendererFeatures.Any(f=>f&&f.GetType()==type))
            {var feature=(ScriptableRendererFeature)ScriptableObject.CreateInstance(type);feature.name="Gaussian Splats";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);feature.Create();renderer.SetDirty();EditorUtility.SetDirty(renderer);}
        }
        static void SetupWine()
        {
            string model=Root+"/Art/Props/Wine/wine.fbx",matPath=Root+"/Materials/Wine.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
            string texture=Root+"/Art/Props/Wine/Textures/glb-Wine_basecolor.png";
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));mat.SetFloat("_Smoothness",.45f);EditorUtility.SetDirty(mat);
            var go=new GameObject("Wine Pickup");
            var modelObject=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
            modelObject.transform.SetParent(go.transform,false);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
            var bounds=go.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            // Author one runtime bottle at ~42 cm, independent of the FBX author's export scale.
            go.transform.localScale*=.42f/Mathf.Max(.01f,bounds.size.y);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/WinePickup.prefab");UnityEngine.Object.DestroyImmediate(go);
        }
        static void CreateScan(int index)
        {
            string key=ScanKeys[index],path=Scenes+ScanScenes[index]+".unity";
            if(File.Exists(path))return; // Preserve collaborators' authored camera/exit placements.
            var asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>($"{Root}/Scans/{key}/scene_{key}.asset");
            if(!asset)throw new InvalidOperationException("Missing imported scan: "+key);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var scan=new GameObject("Scan - "+key);scan.transform.rotation=Quaternion.Euler(0,0,180);scan.transform.localScale=new Vector3(1,1,-1);
            var gs=scan.AddComponent<GaussianSplatRenderer>();gs.m_Asset=asset;gs.m_SHOrder=0;
            string package="Packages/org.nesnausk.gaussian-splatting/Shaders/";
            gs.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(package+"RenderGaussianSplats.shader");
            gs.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(package+"GaussianComposite.shader");
            gs.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(package+"GaussianDebugRenderPoints.shader");
            gs.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(package+"GaussianDebugRenderBoxes.shader");
            gs.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(package+"SplatUtilities.compute");
            var cam=new GameObject("Scan Camera");cam.tag="MainCamera";cam.transform.rotation=Quaternion.Euler(0,180,0);
            var camera=cam.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.nearClipPlane=.03f;camera.farClipPlane=300;camera.fieldOfView=60;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;cam.AddComponent<AudioListener>();
            var walk=cam.AddComponent<D22ScanWalk>();
            Vector3 min=-asset.boundsMax-Vector3.one*1.5f,max=-asset.boundsMin+Vector3.one*1.5f;
            walk.bounds=new Bounds((min+max)*.5f,max-min);walk.speed=Mathf.Clamp(Mathf.Max(walk.bounds.size.x,walk.bounds.size.z)/50,.45f,1.4f);
            // Scan interior lighting is captured in its color; this light only illuminates the bottle prop.
            var lamp=new GameObject("Prop Fill").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=1;lamp.transform.rotation=Quaternion.Euler(45,20,0);
            if(index>0)
            {
                var exit=new GameObject("Chapter Exit").AddComponent<D22Exit>();exit.transform.position=new Vector3(0,0,-3);exit.radius=1.7f;
                exit.nextScene=index==1?"D22_LiveScan":index==2?"D22_Bootstrap":"END";
                exit.label=index==1?"走进现场 / ENTER LIVE HOUSE":index==2?"听演出 / HEAR THE SHOW":"离场 / LEAVE THE SHOW";
            }
            EditorSceneManager.SaveScene(scene,path);
        }
        static void CreateMenu()
        {
            bool exists=File.Exists(Scenes+"D22_Menu.unity");
            var scene=exists?EditorSceneManager.OpenScene(Scenes+"D22_Menu.unity",OpenSceneMode.Single):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            if(!exists)
            {
                var camera=new GameObject("Menu Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.gameObject.AddComponent<AudioListener>();
            }
            var flow=UnityEngine.Object.FindAnyObjectByType<D22GameFlow>();
            if(!flow)flow=new GameObject("D22 Game").AddComponent<D22GameFlow>();
            flow.storyAsset=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Story/story.json");flow.uiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/UI/Fonts/NotoSansSC.ttf");flow.poster=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/UI/poster.png");flow.cover=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/UI/d22.png");
            flow.music=AssetDatabase.FindAssets("t:AudioClip",new[]{Root+"/Audio"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p,StringComparer.Ordinal).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
            flow.bottlePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/WinePickup.prefab");
            EditorSceneManager.SaveScene(scene,Scenes+"D22_Menu.unity");
        }
        public static void ConfigureBuildScenes()=>EditorBuildSettings.scenes=GameScenes.Where(n=>File.Exists(Scenes+n+".unity")).Select(n=>new EditorBuildSettingsScene(Scenes+n+".unity",true)).ToArray();
        [MenuItem("D22/Game/Open Main Menu")]
        public static void OpenGame()=>EditorSceneManager.OpenScene(Scenes+"D22_Menu.unity",OpenSceneMode.Single);
        [MenuItem("D22/Build/macOS")]
        public static void BuildMac()=>Build(BuildTarget.StandaloneOSX,"builds/macOS/A Gift for D22.app");
        [MenuItem("D22/Build/Windows x64")]
        public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"builds/Windows/A Gift for D22.exe");
        static void Build(BuildTarget target,string relative)
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,target))throw new InvalidOperationException("Install the build module for "+target+" in Unity Hub.");
            ConfigureBuildScenes();string repo=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.."));string path=Path.Combine(repo,relative);Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,path,target,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+report.summary.result);
            Debug.Log("D22_BUILD_SUCCESS "+report.summary.totalSize+" bytes "+path);
        }
    }
}
