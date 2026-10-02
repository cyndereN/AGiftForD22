using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace D22
{
    public sealed class D22GameFlow : MonoBehaviour
    {
        public static D22GameFlow Instance { get; private set; }
        public static bool InputBlocked => Instance && Instance.blocked;
        public TextAsset storyAsset;
        public Font uiFont;
        public Texture2D poster, cover;
        public AudioClip[] music;
        public GameObject bottlePrefab;
        public D22GameUI UI { get; private set; }
        public string Chapter { get; private set; } = "D22_Menu";
        public bool HasDrink { get; private set; }
        public int Sips { get; private set; }
        public int Mood { get; private set; } = 1;
        public bool ReducedMotion { get; set; } = true;
        public float Volume { get => audioSource.volume; set => audioSource.volume=value; }
        public bool Loading { get; private set; }
        bool blocked=true, bossMet, aimingBottle;
        float cooldown, pour;
        GameObject bottle;
        AudioSource audioSource;
        AudioLowPassFilter lowPass;
        AudioDistortionFilter distortion;
        AudioReverbFilter reverb;
        D22Story story;
        VolumeProfile moodProfile;
        readonly float[] spectrum = new float[64];
        void Awake()
        {
            if(Instance){Destroy(gameObject);return;}
            Instance=this; DontDestroyOnLoad(gameObject);
            story=JsonUtility.FromJson<D22Story>(storyAsset.text);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.loop=true;audioSource.volume=.35f;
            lowPass=gameObject.AddComponent<AudioLowPassFilter>();lowPass.cutoffFrequency=22000;
            distortion=gameObject.AddComponent<AudioDistortionFilter>();distortion.distortionLevel=0;
            reverb=gameObject.AddComponent<AudioReverbFilter>();reverb.reverbPreset=AudioReverbPreset.Off;
            UI=gameObject.AddComponent<D22GameUI>();UI.Initialize(this,uiFont,poster,cover);
        }
        void Start()=>UI.ShowMenu();
        void OnDestroy(){if(Instance==this)Instance=null;if(moodProfile)Destroy(moodProfile);}
        public void Begin()
        {
            HasDrink=false;Sips=0;bossMet=false;Mood=1;cooldown=0;
            Dialogue(story.intro,()=>UI.ShowPuzzle(()=>LoadSpace("D22_RecordShop")),"五道口 · D-22");
            PlayTrack(0);
        }
        public void LoadSpace(string scene)
        {
            if(!Loading)StartCoroutine(LoadRoutine(scene));
        }
        IEnumerator LoadRoutine(string scene)
        {
            Loading=true;blocked=true;UI.ShowLoading();
            yield return SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
            Chapter=scene;bottle=null;aimingBottle=false;pour=0;
            if(scene=="D22_Bootstrap")
            {
                yield return new WaitUntil(()=>Camera.main!=null);
                var exit=new GameObject("Stage Exit").AddComponent<D22Exit>();
                exit.transform.position=new Vector3(0,1.8f,4);exit.radius=2;
                exit.nextScene="D22_Performance";exit.label="演出开始 / HEAR THE SHOW";
            }
            yield return null;
            if(scene=="D22_Menu") {audioSource.Stop();UI.ShowMenu();}
            else
            {
                SetupMood();PlayTrack(scene=="D22_Performance"?3:scene=="D22_RecordShop"?0:2);
                blocked=false;UI.ShowHUD();
                if(scene=="D22_RecordShop") {bossMet=false;Dialogue(story.recordShop,Resume,"唱片店 / RECORD SHOP");}
                if(scene=="D22_Hutong")Dialogue(story.hutong,Resume,"胡同 / HUTONG");
            }
            Loading=false;
        }
        void Resume(){blocked=false;UI.ShowHUD();}
        public void Dialogue(D22Line[] lines,Action after,string title="D-22")
        {blocked=true;UI.ShowDialogue(title,lines,after);}
        public void Pause(){if(Loading)return;blocked=true;UI.ShowPause(Resume);}
        public void RequestDrink()
        {
            if(!HasDrink || Time.time<cooldown || blocked)return;
            blocked=true;UI.ShowDrink(()=>StartCoroutine(Pour()),Resume);
        }
        IEnumerator Pour()
        {
            UI.DisableDrink();pour=0;
            while(pour<1.35f){pour+=Time.unscaledDeltaTime;UI.SetPour(pour/1.35f);yield return null;}
            cooldown=Time.time+3;Sips++;
            if(Chapter=="D22_RecordShop")UI.ShowChoices(story.choice,i=>{Mood=i;LoadSpace("D22_Hutong");});
            else if(Sips>=5)UI.ShowEnding("声音还在。先歇一会儿。",()=>LoadSpace("D22_Menu"));
            else Resume();
        }
        void Update()
        {
            var k=Keyboard.current;
            if(k!=null && k.escapeKey.wasPressedThisFrame && !blocked)Pause();
            if(blocked || Loading || !Camera.main)return;
            var camera=Camera.main;
            if(Chapter=="D22_RecordShop")
            {
                if(!bossMet && camera.transform.position.z < -5)
                {bossMet=true;Dialogue(story.boss,()=>{SpawnBottle();Resume();},"店主 / THE SHOPKEEPER");return;}
                aimingBottle=bottle && Vector3.Distance(camera.transform.position,bottle.transform.position)<2 && Vector3.Dot(camera.transform.forward,(bottle.transform.position-camera.transform.position).normalized)>.86f;
                if(aimingBottle && k!=null && k.eKey.wasPressedThisFrame){TryInteract();return;}
            }
            var exit=FindAnyObjectByType<D22Exit>();
            bool atExit=exit && Vector3.Distance(camera.transform.position,exit.transform.position)<exit.radius;
            string hint=aimingBottle?"E · 拿起酒瓶 / TAKE THE BOTTLE":atExit?"E · "+exit.label:Chapter=="D22_RecordShop"?(!bossMet?"往唱片店里走走 / Explore the shop":!HasDrink?"回头拿上酒瓶 / Find the bottle behind you":"1 · 喝酒 / DRINK"):"WASD 移动 · 按住鼠标右键转向 · Esc 菜单";
            UI.UpdateHUD(hint,HasDrink,Sips,Mathf.Max(0,cooldown-Time.time));
            if(atExit && !aimingBottle && k!=null && k.eKey.wasPressedThisFrame)TryInteract();
            if(k!=null && k.digit1Key.wasPressedThisFrame)RequestDrink();
            audioSource.GetSpectrumData(spectrum,0,FFTWindow.BlackmanHarris);
            float pulse=Mathf.Clamp01(spectrum[2]*30);
            camera.fieldOfView=ReducedMotion?60:Mood==0?62+Mathf.Sin(Time.time*1.2f)*4+pulse:Mood==2?52:60;
            if(moodProfile && moodProfile.TryGet<Bloom>(out var bloom))bloom.intensity.value=Mood==0?.25f+(ReducedMotion?0:pulse*.2f):0;
        }
        public void TryInteract()
        {
            if(blocked||Loading||!Camera.main)return;
            var camera=Camera.main.transform;
            if(bottle && Vector3.Distance(camera.position,bottle.transform.position)<2 && Vector3.Dot(camera.forward,(bottle.transform.position-camera.position).normalized)>.86f)
            {Destroy(bottle);HasDrink=true;Dialogue(story.wine,Resume,"酒 / THE DRINK");return;}
            var exit=FindAnyObjectByType<D22Exit>();
            if(!exit||Vector3.Distance(camera.position,exit.transform.position)>=exit.radius)return;
            if(exit.nextScene=="END"){blocked=true;UI.ShowEnding("名字没有一起关掉，还在人嘴里走。",()=>LoadSpace("D22_Menu"));}
            else LoadSpace(exit.nextScene);
        }
        void SpawnBottle()
        {
            if(HasDrink)return;
            bottle=Instantiate(bottlePrefab,new Vector3(-.36f,-.68f,-1.72f),Quaternion.Euler(0,160,0));
            bottle.name="Wine Pickup";
        }
        void PlayTrack(int index)
        {if(music==null||music.Length==0)return;var clip=music[Mathf.Clamp(index,0,music.Length-1)];if(audioSource.clip==clip&&audioSource.isPlaying)return;audioSource.clip=clip;audioSource.Play();}
        void SetupMood()
        {
            if(moodProfile)Destroy(moodProfile);
            var volume=new GameObject("Story Mood").AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;volume.weight=.6f;
            moodProfile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=moodProfile;
            var color=moodProfile.Add<ColorAdjustments>(true);color.postExposure.value=Mood==2?-.35f:0;color.saturation.value=Mood==1?-8:Mood==2?-25:5;
            color.colorFilter.value=Mood==0?new Color(1,.91f,.84f):Mood==2?new Color(.8f,.9f,1):Color.white;
            var vignette=moodProfile.Add<Vignette>(true);vignette.intensity.value=Mood==2?.36f:.2f;
            moodProfile.Add<Bloom>(true).intensity.value=Mood==0?.25f:0;
            lowPass.cutoffFrequency=Mood==2?1600:22000;distortion.distortionLevel=Mood==0?.25f:0;reverb.reverbPreset=Mood==2?AudioReverbPreset.Room:AudioReverbPreset.Off;
        }
    }
}
