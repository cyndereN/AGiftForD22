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
        public static bool InputBlocked => Instance && (Instance.blocked || Instance.AbilityOpen || Instance.HuntOpen);
        public static bool UiBlocksLook => Instance && Instance.UI && Instance.UI.BlocksLook;
        public TextAsset storyAsset;
        public Font uiFont;
        public Texture2D poster, cover;
        public AudioClip[] music;
        public AudioClip beerClip, cricketClip, pigeonClip, grindClip;
        public GameObject bottlePrefab;
        public D22GameUI UI { get; private set; }
        public D22Abilities Abilities { get; private set; }
        public D22Hunt Hunt { get; private set; }
        public string Chapter { get; private set; } = "D22_Menu";
        public bool HasDrink { get; private set; }
        public int Sips { get; private set; }
        public int Mood { get; private set; } = 1;
        public bool ReducedMotion { get; set; } = true;
        public float Volume
        {
            get => audioSource.volume;
            set { audioSource.volume = value; Abilities?.SetVolume(value); }
        }
        public bool Loading { get; private set; }
        public bool AbilityOpen => Abilities != null && Abilities.Open;
        public bool HuntOpen => Hunt != null && Hunt.Open;
        bool blocked = true, bossMet, aimingBottle, doorHeard, hutongAsked;
        float cooldown;
        D22RecordShop recordShop;
        Vector3 shopEntry;
        bool walked;
        GameObject bottle;
        AudioSource audioSource;
        AudioLowPassFilter lowPass;
        AudioDistortionFilter distortion;
        AudioReverbFilter reverb;
        D22Story story;
        VolumeProfile moodProfile;
        Volume moodVolume;
        readonly float[] spectrum = new float[64];

        void Awake()
        {
            if (Instance) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            story = JsonUtility.FromJson<D22Story>(storyAsset.text);
            D22Abilities.ApplyStory(story.abilities);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.volume = .35f;
            lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 22000;
            distortion = gameObject.AddComponent<AudioDistortionFilter>();
            distortion.distortionLevel = 0;
            reverb = gameObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.Off;
            UI = gameObject.AddComponent<D22GameUI>();
            UI.Initialize(this, uiFont, poster, cover);
            Abilities = new D22Abilities(transform, bottlePrefab, audioSource.volume);
            Hunt = new D22Hunt(this);
            BindSfx();
        }

        public void BindSfx()
        {
            Abilities?.SetBeer(beerClip);
            Hunt?.SetClips(cricketClip, pigeonClip, grindClip);
        }

        void Start() => UI.ShowMenu();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (moodProfile) Destroy(moodProfile);
            Abilities?.Dispose();
        }

        public void Begin()
        {
            HasDrink = false;
            Sips = 0;
            bossMet = false;
            doorHeard = false;
            hutongAsked = false;
            Mood = 1;
            cooldown = 0;
            Hunt?.Reset();
            Abilities.ResetProgress();
            Dialogue(story.intro, () => UI.ShowPuzzle(() => LoadSpace("D22_RecordShop")));
            PlayTrack(0);
        }

        public void LoadSpace(string scene)
        {
            if (!Loading) StartCoroutine(LoadRoutine(scene));
        }

        IEnumerator LoadRoutine(string scene)
        {
            Loading = true;
            blocked = true;
            CloseAbility();
            CloseHunt();
            Hunt?.ClearMarks();
            D22Look.Unlock();
            UI.ShowLoading();
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            Chapter = scene;
            bottle = null;
            aimingBottle = false;
            recordShop = FindAnyObjectByType<D22RecordShop>();
            shopEntry = Camera.main ? Camera.main.transform.position : Vector3.zero;
            walked = false;
            if (scene == "D22_Bootstrap")
                yield return new WaitUntil(() => Camera.main != null);
            yield return null;
            if (scene == "D22_Menu")
            {
                audioSource.Stop();
                UI.ShowMenu();
            }
            else
            {
                SetupMood();
                PlayTrack(scene == "D22_Performance" ? 3 : scene == "D22_RecordShop" ? 0 : 2);
                blocked = false;
                UI.ShowHUD();
                if (scene == "D22_RecordShop") { bossMet = false; Dialogue(story.recordShop, Resume, "唱片店 / RECORD SHOP"); }
                else if (scene == "D22_Hutong")
                {
                    doorHeard = false;
                    hutongAsked = false;
                    var hutongLighting = FindAnyObjectByType<D22HutongLighting>();
                    if (hutongLighting) hutongLighting.Apply(Mood == 2);
                    Dialogue(story.hutong, Resume, "胡同 / HUTONG");
                }
            }
            Loading = false;
        }

        void Resume()
        {
            blocked = false;
            UI.ShowHUD();
        }

        public void Dialogue(D22Line[] lines, Action after, string title = "D-22")
        {
            CloseAbility();
            CloseHunt();
            D22Look.Unlock();
            blocked = true;
            UI.ShowDialogue(title, lines, after);
        }

        public void ResumeHunt(string id)
        {
            blocked = false;
            D22Look.Unlock();
            if (Hunt != null && Hunt.TryOpen(id)) UI.ShowMinigame();
            else UI.ShowHUD();
        }

        public void CloseHunt()
        {
            if (Hunt == null || !Hunt.Open) return;
            Hunt.Close();
            if (!blocked && !Loading) UI.ShowHUD();
        }

        public void Pause()
        {
            if (Loading) return;
            CloseAbility();
            CloseHunt();
            D22Look.Unlock();
            blocked = true;
            UI.ShowPause(Resume);
        }

        public void NoteWalk()
        {
            if (Abilities.Learn("walk")) UI.PlayLearnFly("walk");
        }

        public void RequestDrink() => ToggleAbility("drink");

        public bool ToggleAbility(string id)
        {
            if (blocked || Loading || Abilities == null) return false;
            if (HuntOpen || AbilityOpen) return false;
            if (id == "drink" && !HasDrink) return false;
            if (id == "cricket" || id == "pigeon" || id == "scissors") return ToggleHuntAbility(id);
            if (!Abilities.CanOpen(id)) return false;
            bool opened = Abilities.Toggle(id);
            D22Look.Unlock();
            if (opened) UI.ShowAbility();
            else UI.ShowHUD();
            return opened;
        }

        bool ToggleHuntAbility(string id)
        {
            if (!Abilities.Learned(id)) return false;
            string huntId = D22Hunt.HuntId(id);
            if (HuntOpen)
            {
                if (Hunt != null && Hunt.Game == huntId) CloseHunt();
                return false;
            }
            if (AbilityOpen) CloseAbility();
            ResumeHunt(huntId);
            return HuntOpen;
        }

        public void CloseAbility()
        {
            if (Abilities == null || !Abilities.Open) return;
            Abilities.Close();
            if (!blocked && !Loading) UI.ShowHUD();
        }

        void FinishPour()
        {
            cooldown = Time.unscaledTime + 3;
            Sips++;
            if (Chapter == "D22_RecordShop")
            {
                blocked = true;
                D22Look.Unlock();
                UI.ShowChoices(story.choice, i => { Mood = i; SetupMood(); LoadSpace("D22_Hutong"); });
            }
            else if (Chapter != "D22_Hutong" && Sips >= 5)
            {
                blocked = true;
                D22Look.Unlock();
                UI.ShowEnding("声音还在。先歇一会儿。", () => LoadSpace("D22_Menu"));
            }
            else Resume();
        }

        void Update()
        {
            var k = Keyboard.current;
            if (Abilities != null)
                Abilities.WalkHot = !blocked && k != null && (k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed);
            if (k != null && k.escapeKey.wasPressedThisFrame)
            {
                if (HuntOpen && !blocked) { CloseHunt(); return; }
                if (AbilityOpen && !blocked) { CloseAbility(); return; }
                if (!blocked) Pause();
            }
            if (!blocked && !Loading && !HuntOpen && !AbilityOpen && k != null) HandleAbilityKeys(k);
            if (Abilities != null)
            {
                Abilities.SetVolume(audioSource.volume);
                Abilities.Tick(FinishPour);
            }
            Hunt?.Tick(Camera.main, audioSource.volume);
            if (blocked || Loading || AbilityOpen || HuntOpen || !Camera.main) return;
            var camera = Camera.main;
            if (Chapter == "D22_RecordShop")
            {
                walked |= Vector3.Distance(camera.transform.position, shopEntry) > 1.5f;
                if (!recordShop && !bossMet && camera.transform.position.z < -5)
                { bossMet = true; Dialogue(story.boss, () => { SpawnBottle(); Resume(); }); return; }
                if (recordShop && !bossMet && recordShop.CanTalk(camera.transform) && k != null && k.eKey.wasPressedThisFrame)
                { TryInteract(); return; }
                aimingBottle = bottle && Vector3.Distance(camera.transform.position, bottle.transform.position) < 2 && Vector3.Dot(camera.transform.forward, (bottle.transform.position - camera.transform.position).normalized) > .86f;
                if (aimingBottle && k != null && k.eKey.wasPressedThisFrame) { TryInteract(); return; }
            }
            var exit = NearestExit(camera.transform.position);
            bool atExit = exit;
            if (Chapter == "D22_Hutong" && !doorHeard && atExit)
            { MeetDoor(); return; }
            if (Chapter == "D22_Hutong" && atExit && Hunt != null && Hunt.Unlocked && Hunt.QuotaDone(Sips) && !hutongAsked)
            { AskHutong(); return; }
            string huntHint = Hunt?.AimHint(camera);
            string shopHint = recordShop
                ? !walked ? "往柜台走走" : !bossMet ? (recordShop.CanTalk(camera.transform) ? "E 和店主说话" : "去柜台找店主")
                : !HasDrink ? "柜台上有瓶酒" : Sips == 0 ? "1 喝酒" : "从侧门去胡同"
                : !bossMet ? "往里走走" : !HasDrink ? "回头拿酒瓶" : "1 喝酒";
            string hint = aimingBottle ? "E 拿酒瓶"
                : Chapter == "D22_Hutong" && atExit && Hunt != null && Hunt.Unlocked ? Hunt.Quota(Sips)
                : !string.IsNullOrEmpty(huntHint) ? huntHint
                : Chapter == "D22_Hutong" && atExit ? (doorHeard ? "已满" : "门")
                : atExit ? (recordShop && Sips == 0 ? "先在柜台拿酒" : "E " + (string.IsNullOrEmpty(exit.label) ? "进去" : ShortLabel(exit.label)))
                : Chapter == "D22_RecordShop" ? shopHint
                    : "";
            UI.UpdateHUD(hint, HasDrink, Sips, Abilities != null ? Abilities.CooldownLeft : Mathf.Max(0, cooldown - Time.unscaledTime));
            if (k != null && k.eKey.wasPressedThisFrame) TryInteract();
            audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
            float pulse = Mathf.Clamp01(spectrum[2] * 30);
            camera.fieldOfView = ReducedMotion ? 60 : Mood == 0 ? 62 + Mathf.Sin(Time.time * 1.2f) * 4 + pulse : Mood == 2 ? 52 : 60;
            if (moodProfile && moodProfile.TryGet<Bloom>(out var bloom)) bloom.intensity.value = Mood == 0 ? .25f + (ReducedMotion ? 0 : pulse * .2f) : 0;
        }

        void HandleAbilityKeys(Keyboard k)
        {
            if (k.digit1Key.wasPressedThisFrame) ToggleAbility("drink");
            else if (k.digit2Key.wasPressedThisFrame) ToggleAbility("cricket");
            else if (k.digit3Key.wasPressedThisFrame) ToggleAbility("pigeon");
            else if (k.digit4Key.wasPressedThisFrame) ToggleAbility("scissors");
            else if (k.digit5Key.wasPressedThisFrame) ToggleAbility("guitar");
            else if (k.digit6Key.wasPressedThisFrame) ToggleAbility("bass");
            else if (k.digit7Key.wasPressedThisFrame) ToggleAbility("drums");
        }

        public void TryInteract()
        {
            if (blocked || Loading || AbilityOpen || HuntOpen || !Camera.main) return;
            var camera = Camera.main;
            if (recordShop && !bossMet && recordShop.CanTalk(camera.transform))
            {
                bossMet = true;
                Dialogue(story.boss, () => { SpawnBottle(); Resume(); }, "店主 / THE SHOPKEEPER");
                return;
            }
            if (bottle && Vector3.Distance(camera.transform.position, bottle.transform.position) < 2 && Vector3.Dot(camera.transform.forward, (bottle.transform.position - camera.transform.position).normalized) > .86f)
            {
                Destroy(bottle);
                HasDrink = true;
                if (Abilities.Learn("drink")) UI.PlayLearnFly("drink");
                Dialogue(story.wine, Resume);
                return;
            }
            if (Hunt != null && Hunt.Spawned && Hunt.Interact(camera, (lines, after) => Dialogue(lines, after), story)) return;
            var exit = NearestExit(camera.transform.position);
            if (!exit || recordShop && Sips == 0) return;
            if (Chapter == "D22_Hutong")
            {
                if (Hunt != null && Hunt.Unlocked && Hunt.QuotaDone(Sips)) AskHutong();
                else MeetDoor();
                return;
            }
            if (exit.nextScene == "END") { blocked = true; UI.ShowEnding("名字没有一起关掉，还在人嘴里走。", () => LoadSpace("D22_Menu")); }
            else LoadSpace(exit.nextScene);
        }

        D22Exit NearestExit(Vector3 position)
        {
            D22Exit nearest = null;
            float distance = float.MaxValue;
            foreach (var exit in FindObjectsByType<D22Exit>(FindObjectsSortMode.None))
            {
                float candidate = Vector3.Distance(position, exit.transform.position);
                if (candidate < exit.radius && candidate < distance) { nearest = exit; distance = candidate; }
            }
            return nearest;
        }

        void MeetDoor()
        {
            if (doorHeard || story.door == null || story.door.Length == 0) return;
            doorHeard = true;
            Dialogue(story.door, () => { Hunt?.AfterDoor(); Resume(); });
        }

        void AskHutong()
        {
            if (hutongAsked || story.hutongAsk == null) return;
            hutongAsked = true;
            CloseAbility();
            CloseHunt();
            D22Look.Unlock();
            blocked = true;
            UI.ShowChoices(story.hutongAsk, i =>
            {
                Mood = i;
                Dialogue(story.livehouse, () => LoadSpace("D22_Bootstrap"));
            });
        }

        static string ShortLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return "进去";
            int slash = label.IndexOf(" / ", StringComparison.Ordinal);
            return slash > 0 ? label.Substring(0, slash) : label;
        }

        void SpawnBottle()
        {
            if (HasDrink) return;
            var spawn = recordShop ? recordShop.bottleSpawn : null;
            var pickupPrefab = recordShop && recordShop.bottlePrefab ? recordShop.bottlePrefab : bottlePrefab;
            bottle = Instantiate(pickupPrefab, spawn ? spawn.position : new Vector3(-.36f, -.68f, -1.72f), spawn ? spawn.rotation : Quaternion.Euler(0, 160, 0));
            if (spawn)
            {
                // FBX origins vary: align the actual bottle bottom to the authored tabletop.
                var renderers = bottle.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    bottle.transform.position += new Vector3(spawn.position.x - bounds.center.x, spawn.position.y - bounds.min.y, spawn.position.z - bounds.center.z);
                }
            }
            bottle.name = "Wine Pickup";
        }

        void PlayTrack(int index)
        {
            if (music == null || music.Length == 0) return;
            var clip = music[Mathf.Clamp(index, 0, music.Length - 1)];
            if (audioSource.clip == clip && audioSource.isPlaying) return;
            audioSource.clip = clip;
            audioSource.Play();
        }

        void SetupMood()
        {
            if (moodProfile) Destroy(moodProfile);
            if (!moodVolume) moodVolume = new GameObject("Story Mood").AddComponent<Volume>();
            moodVolume.isGlobal = true;
            moodVolume.priority = 20;
            moodVolume.weight = .6f;
            moodProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            moodVolume.sharedProfile = moodProfile;
            var color = moodProfile.Add<ColorAdjustments>(true);
            color.postExposure.value = Mood == 2 ? -.35f : 0;
            color.saturation.value = Mood == 1 ? -8 : Mood == 2 ? -25 : 5;
            color.colorFilter.value = Mood == 0 ? new Color(1, .91f, .84f) : Mood == 2 ? new Color(.8f, .9f, 1) : Color.white;
            var vignette = moodProfile.Add<Vignette>(true);
            vignette.intensity.value = Mood == 2 ? .36f : .2f;
            moodProfile.Add<Bloom>(true).intensity.value = Mood == 0 ? .25f : 0;
            lowPass.cutoffFrequency = Mood == 2 ? 1600 : 22000;
            distortion.distortionLevel = Mood == 0 ? .25f : 0;
            reverb.reverbPreset = Mood == 2 ? AudioReverbPreset.Room : AudioReverbPreset.Off;
        }
    }
}
