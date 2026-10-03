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
        public static bool InputBlocked => Instance && (Instance.blocked || Instance.AbilityOpen || Instance.HuntOpen || Instance.StageOpen);
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
        public D22Stage Stage { get; private set; }
        public D22Conductor Conductor { get; private set; }
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
        public bool StageOpen => Stage != null && Stage.Open;
        bool blocked = true, bossMet, aimingBottle, doorHeard, hutongAsked;
        float cooldown;
        GameObject bottle;
        AudioSource audioSource;
        AudioClip[] playlist;
        AudioClip finaleClip;
        int playlistIndex = -1;
        bool onFinale, heardPlaylist, playlistHeld;
        AudioLowPassFilter lowPass;
        AudioDistortionFilter distortion;
        AudioReverbFilter reverb;
        D22Story story;
        VolumeProfile moodProfile;
        readonly float[] spectrum = new float[64];

        void Awake()
        {
            if (Instance) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            story = JsonUtility.FromJson<D22Story>(storyAsset.text);
            D22Abilities.ApplyStory(story.abilities);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.volume = .35f;
            BuildPlaylist();
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
            Stage = new D22Stage(this);
            Conductor = new D22Conductor();
            BindSfx();
        }

        public void BindSfx()
        {
            Abilities?.SetBeer(beerClip);
            Abilities?.SetKnife(grindClip);
            Hunt?.SetClips(cricketClip, pigeonClip, grindClip);
        }

        void Start()
        {
            UI.ShowMenu();
            EnsurePlaylist();
        }

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
        }

        void GrantSkipped(params string[] ids)
        {
            if (Abilities == null || UI == null) return;
            foreach (var id in ids)
                if (Abilities.Learn(id)) UI.PlayLearnFly(id);
        }

        public void LoadSpace(string scene)
        {
            if (!Loading) StartCoroutine(LoadRoutine(scene));
        }

        public void SkipChapter()
        {
            if (Loading || UI == null || !UI.OffersLevelSkip) return;
            Conductor?.End();
            CloseAbility();
            CloseHunt();
            CloseStage();
            D22Look.Unlock();
            switch (Chapter)
            {
                case "D22_RecordShop":
                    HasDrink = true;
                    GrantSkipped("drink");
                    LoadSpace("D22_Hutong");
                    break;
                case "D22_Hutong":
                    GrantSkipped("cricket", "pigeon", "scissors");
                    LoadSpace("D22_LiveScan");
                    break;
                case "D22_LiveScan":
                    GrantSkipped("guitar", "bass", "drums");
                    LoadSpace("D22_Performance");
                    break;
                case "D22_Bootstrap":
                    LoadSpace("D22_Performance");
                    break;
                case "D22_Performance":
                    blocked = true;
                    UI.ShowEnding("名字没有一起关掉，还在人嘴里走。", () => LoadSpace("D22_Menu"));
                    break;
                default:
                    LoadSpace("D22_RecordShop");
                    break;
            }
        }

        IEnumerator LoadRoutine(string scene)
        {
            Loading = true;
            blocked = true;
            Conductor?.End();
            CloseAbility();
            CloseHunt();
            Hunt?.ClearMarks();
            Stage?.Clear();
            D22Look.Unlock();
            UI.ShowLoading();
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            Chapter = scene;
            bottle = null;
            aimingBottle = false;
            if (scene == "D22_Bootstrap")
            {
                yield return new WaitUntil(() => Camera.main != null);
                var exit = new GameObject("Stage Exit").AddComponent<D22Exit>();
                exit.transform.position = new Vector3(0, 1.8f, 4);
                exit.radius = 2;
                exit.nextScene = "D22_Performance";
                exit.label = "演出开始 / HEAR THE SHOW";
            }
            yield return null;
            if (scene == "D22_Performance") PlayFinale();
            else if (scene == "D22_Hutong") HoldPlaylist();
            else EnsurePlaylist();
            if (scene == "D22_Menu")
            {
                UI.ShowMenu();
            }
            else
            {
                SetupMood();
                blocked = false;
                UI.ShowHUD();
                if (scene == "D22_RecordShop") { bossMet = false; Dialogue(story.recordShop, Resume); }
                else if (scene == "D22_Hutong") { doorHeard = false; hutongAsked = false; Dialogue(story.hutong, Resume); }
                else if (scene == "D22_Performance") PlayChorus();
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

        public void ResumeStage(string id)
        {
            blocked = false;
            D22Look.Unlock();
            if (Stage != null && Stage.TryOpen(id)) UI.ShowMinigame();
            else UI.ShowHUD();
        }

        public void CloseStage()
        {
            if (Stage == null || !Stage.Open) return;
            Stage.Close();
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
            if (id == "cricket" || id == "pigeon" || id == "scissors") return ToggleHuntAbility(id);
            if (id == "guitar" || id == "bass" || id == "drums") return ToggleStageAbility(id);
            if (HuntOpen || AbilityOpen || StageOpen) return false;
            if (id == "drink" && !HasDrink) return false;
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

        bool ToggleStageAbility(string id)
        {
            if (!Abilities.Learned(id)) return false;
            if (StageOpen)
            {
                if (Stage != null && Stage.Game == id) CloseStage();
                return false;
            }
            if (AbilityOpen) CloseAbility();
            if (HuntOpen) CloseHunt();
            ResumeStage(id);
            return StageOpen;
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
            if (Chapter == "D22_LiveScan") Stage?.NoteDrink();
            if (Chapter == "D22_RecordShop")
            {
                blocked = true;
                D22Look.Unlock();
                UI.ShowChoices(story.choice, i => { Mood = i; LoadSpace("D22_Hutong"); });
            }
            else if (Chapter != "D22_Hutong" && Chapter != "D22_LiveScan" && Chapter != "D22_Performance" && Sips >= 5)
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
            TickPlaylist();
            if (Abilities != null)
                Abilities.WalkHot = !blocked && k != null && (k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed);
            if (!Loading && UI != null && UI.OffersLevelSkip && k != null && k.rightBracketKey.wasPressedThisFrame)
            { SkipChapter(); return; }
            if (Conductor != null && Conductor.Running)
            {
                int slot = ReadPerformKey(k);
                if (Conductor.Tick(Time.unscaledDeltaTime, slot, out int played) )
                {
                    if (played >= 0) Abilities?.TapSlot(played);
                    FinishSong();
                }
                else if (played >= 0) Abilities?.TapSlot(played);
                return;
            }
            if (UI != null && UI.InCredits) return;
            if (k != null && k.escapeKey.wasPressedThisFrame)
            {
                if (StageOpen && !blocked) { CloseStage(); return; }
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
            Stage?.Tick();
            if (!Loading && Chapter == "D22_LiveScan" && Stage != null && Stage.ShowReady)
            {
                CloseStage();
                CloseAbility();
                CloseHunt();
                LoadSpace("D22_Performance");
                return;
            }
            if (blocked || Loading || AbilityOpen || HuntOpen || StageOpen || !Camera.main) return;
            var camera = Camera.main;
            if (Chapter == "D22_RecordShop")
            {
                if (!bossMet && camera.transform.position.z < -5)
                { bossMet = true; Dialogue(story.boss, () => { SpawnBottle(); Resume(); }); return; }
                aimingBottle = bottle && Vector3.Distance(camera.transform.position, bottle.transform.position) < 2 && Vector3.Dot(camera.transform.forward, (bottle.transform.position - camera.transform.position).normalized) > .86f;
                if (aimingBottle && k != null && k.eKey.wasPressedThisFrame) { TryInteract(); return; }
            }
            var exit = FindAnyObjectByType<D22Exit>();
            bool atExit = Chapter != "D22_LiveScan" && exit && Vector3.Distance(camera.transform.position, exit.transform.position) < exit.radius;
            if (Chapter == "D22_LiveScan" && Stage != null)
            {
                if (!Stage.IntroPlayed && Stage.NearIntro(camera)) { MeetBand(); return; }
                if (Stage.TrioDone && !Stage.OnStagePlayed && Stage.NearStage(camera)) { StepOnStage(); return; }
            }
            if (Chapter == "D22_Hutong" && !doorHeard && atExit)
            { MeetDoor(); return; }
            if (Chapter == "D22_Hutong" && atExit && Hunt != null && Hunt.Unlocked && Hunt.QuotaDone(Sips) && !hutongAsked)
            { AskHutong(); return; }
            string huntHint = Hunt?.AimHint(camera);
            string stageHint = Chapter == "D22_LiveScan" ? Stage?.Hint(camera) : null;
            string hint = aimingBottle ? "E 拿酒瓶"
                : Chapter == "D22_Hutong" && atExit && Hunt != null && Hunt.Unlocked ? Hunt.Quota(Sips)
                : !string.IsNullOrEmpty(stageHint) ? stageHint
                : Chapter == "D22_LiveScan" && Stage != null && !Stage.IntroPlayed ? "往舞台走"
                : !string.IsNullOrEmpty(huntHint) ? huntHint
                : Chapter == "D22_Hutong" && atExit ? (doorHeard ? "已满" : "门")
                : atExit ? "E " + (string.IsNullOrEmpty(exit.label) ? "进去" : ShortLabel(exit.label))
                : Chapter == "D22_RecordShop"
                    ? (!bossMet ? "往里走走" : !HasDrink ? "回头拿酒瓶" : "1 喝酒")
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
            if (bottle && Vector3.Distance(camera.transform.position, bottle.transform.position) < 2 && Vector3.Dot(camera.transform.forward, (bottle.transform.position - camera.transform.position).normalized) > .86f)
            {
                Destroy(bottle);
                HasDrink = true;
                if (Abilities.Learn("drink")) UI.PlayLearnFly("drink");
                Dialogue(story.wine, Resume);
                return;
            }
            if (Hunt != null && Hunt.Spawned && Hunt.Interact(camera, (lines, after) => Dialogue(lines, after), story)) return;
            if (Chapter == "D22_LiveScan" && Stage != null && Stage.Interact(camera, (title, lines, after) => Dialogue(lines, after, title), story)) return;
            if (Chapter == "D22_LiveScan") return;
            var exit = FindAnyObjectByType<D22Exit>();
            if (!exit || Vector3.Distance(camera.transform.position, exit.transform.position) >= exit.radius) return;
            if (Chapter == "D22_Hutong")
            {
                if (Hunt != null && Hunt.Unlocked && Hunt.QuotaDone(Sips)) AskHutong();
                else MeetDoor();
                return;
            }
            if (exit.nextScene == "END") { blocked = true; UI.ShowEnding("名字没有一起关掉，还在人嘴里走。", () => LoadSpace("D22_Menu")); }
            else LoadSpace(exit.nextScene);
        }

        void MeetDoor()
        {
            if (doorHeard || story.door == null || story.door.Length == 0) return;
            doorHeard = true;
            Dialogue(story.door, () => { Hunt?.AfterDoor(); Resume(); });
        }

        void MeetBand()
        {
            if (Stage == null || Stage.IntroPlayed || story.band == null || story.band.Length == 0) return;
            Stage.MarkIntro();
            Dialogue(story.band, () => { Stage.Spawn(); Resume(); }, "三大件");
        }

        void StepOnStage()
        {
            if (Stage == null || Stage.OnStagePlayed || story.stage == null) return;
            Stage.MarkOnStage();
            Dialogue(story.stage, () => { Stage.BeginEncore(); Resume(); }, "舞台");
        }

        void PlayChorus()
        {
            if (story.chorus == null || story.chorusAsk == null) { BeginSong(); return; }
            Dialogue(story.chorus, () =>
            {
                UI.ShowChoices(story.chorusAsk, i =>
                {
                    Mood = i;
                    var follow = i == 0 ? story.remembered : i == 1 ? story.finishSong : story.unsure;
                    Dialogue(follow, BeginSong);
                });
            });
        }

        void BeginSong()
        {
            blocked = true;
            CloseAbility();
            CloseHunt();
            CloseStage();
            D22Look.Unlock();
            Conductor.Begin();
            UI.ShowConduct();
        }

        void FinishSong()
        {
            if (story.aftersong != null && story.aftersong.Length > 0) Dialogue(story.aftersong, RollCredits, "演唱完");
            else RollCredits();
        }

        void RollCredits()
        {
            blocked = true;
            D22Look.Unlock();
            UI.ShowCredits(story.credits, () => LoadSpace("D22_Menu"));
        }

        static int ReadPerformKey(Keyboard k)
        {
            if (k == null) return -1;
            if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) return 1;
            if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) return 2;
            if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) return 3;
            if (k.digit5Key.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame) return 4;
            if (k.digit6Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame) return 5;
            if (k.digit7Key.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame) return 6;
            return -1;
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
                Dialogue(story.livehouse, () => LoadSpace("D22_LiveScan"));
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
            bottle = Instantiate(bottlePrefab, new Vector3(-.36f, -.68f, -1.72f), Quaternion.Euler(0, 160, 0));
            bottle.name = "Wine Pickup";
        }

        void BuildPlaylist()
        {
            var songs = new System.Collections.Generic.List<AudioClip>();
            if (music != null)
            {
                foreach (var clip in music)
                {
                    if (!clip) continue;
                    if (IsFinale(clip)) finaleClip = clip;
                    else songs.Add(clip);
                }
            }
            playlist = songs.ToArray();
        }

        static bool IsFinale(AudioClip clip)
        {
            string name = clip.name;
            return name.IndexOf("Zhong Nan Hai", StringComparison.OrdinalIgnoreCase) >= 0 || name.Contains("中南海");
        }

        void HoldPlaylist()
        {
            heardPlaylist = false;
            playlistHeld = true;
            audioSource.Pause();
        }

        void EnsurePlaylist()
        {
            if (playlist == null || playlist.Length == 0) return;
            if (playlistHeld)
            {
                playlistHeld = false;
                if (audioSource.clip != null && audioSource.clip != finaleClip)
                {
                    onFinale = false;
                    audioSource.loop = false;
                    audioSource.UnPause();
                    if (!audioSource.isPlaying) audioSource.Play();
                    return;
                }
            }
            if (onFinale)
            {
                onFinale = false;
                heardPlaylist = false;
                playlistIndex = (playlistIndex + 1) % playlist.Length;
                PlayCurrent();
                return;
            }
            if (audioSource.isPlaying) return;
            if (playlistIndex < 0) playlistIndex = 0;
            PlayCurrent();
        }

        void PlayFinale()
        {
            if (!finaleClip)
            {
                EnsurePlaylist();
                return;
            }
            heardPlaylist = false;
            onFinale = true;
            audioSource.loop = true;
            if (audioSource.clip == finaleClip && audioSource.isPlaying) return;
            audioSource.clip = finaleClip;
            audioSource.Play();
        }

        void PlayCurrent()
        {
            if (playlist == null || playlist.Length == 0) return;
            playlistIndex = Mathf.Clamp(playlistIndex, 0, playlist.Length - 1);
            onFinale = false;
            audioSource.loop = false;
            audioSource.clip = playlist[playlistIndex];
            audioSource.Play();
        }

        void TickPlaylist()
        {
            if (onFinale || playlistHeld || playlist == null || playlist.Length == 0) return;
            if (audioSource.isPlaying) heardPlaylist = true;
            else if (heardPlaylist)
            {
                heardPlaylist = false;
                playlistIndex = (playlistIndex + 1) % playlist.Length;
                PlayCurrent();
            }
        }

        void SetupMood()
        {
            if (moodProfile) Destroy(moodProfile);
            var volume = new GameObject("Story Mood").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 20;
            volume.weight = .6f;
            moodProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = moodProfile;
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
