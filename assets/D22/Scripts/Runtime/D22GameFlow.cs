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
        bool IsLivehouse => Chapter == D22Bootstrap.SceneName || Chapter == "D22_LiveScan";
        public bool HasDrink { get; private set; }
        public int Sips { get; private set; }
        public bool ReducedMotion { get; set; } = true;
        public float Volume
        {
            get => musicLevel;
            set { musicLevel = Mathf.Clamp01(value); ApplyMusicLevel(); Abilities?.SetVolume(musicLevel); }
        }
        public bool Loading { get; private set; }
        public bool AbilityOpen => Abilities != null && Abilities.Open;
        public bool HuntOpen => Hunt != null && Hunt.Open;
        public bool StageOpen => Stage != null && Stage.Open;
        bool blocked = true, bossMet, aimingBottle, doorHeard, hutongAsked, loftHeard;
        float cooldown;
        D22RecordShop recordShop;
        Vector3 shopEntry;
        bool walked;
        GameObject bottle;
        AudioSource audioSource;
        AudioClip[] playlist;
        AudioClip finaleClip;
        int playlistIndex = -1;
        bool onFinale, heardPlaylist, playlistHeld;
        float musicLevel = .35f;
        float bedGain = 1f;
        AudioSource hum;
        AudioLowPassFilter lowPass;
        AudioDistortionFilter distortion;
        AudioReverbFilter reverb;
        D22Story story;
        Volume moodVolume;
        VolumeProfile moodProfile;
        bool presenting;

        void Awake()
        {
            if (Instance) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            story = JsonUtility.FromJson<D22Story>(storyAsset.text);
            D22Abilities.ApplyStory(story.abilities);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.volume = musicLevel;
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
            loftHeard = false;
            D22Feel.Clear();
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
                    LoadSpace(D22Bootstrap.SceneName);
                    break;
                case D22Bootstrap.SceneName:
                case "D22_LiveScan":
                    GrantSkipped("guitar", "bass", "drums");
                    LoadSpace("D22_Performance");
                    break;
                case "D22_Performance":
                    blocked = true;
                    UI.ShowEnding("这个名字，至今仍被大家口口相传。", () => LoadSpace("D22_Menu"));
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
            recordShop = FindAnyObjectByType<D22RecordShop>();
            shopEntry = Camera.main ? Camera.main.transform.position : Vector3.zero;
            walked = false;
            if (scene == "D22_Bootstrap")
            {
                var bootstrap = FindAnyObjectByType<D22Bootstrap>();
                yield return new WaitUntil(() => bootstrap && bootstrap.Ready && Camera.main != null);
                Stage?.Bind(bootstrap);
            }
            else Stage?.Bind(null);
            yield return null;
            if (scene == "D22_Performance") PlayFinale();
            else if (scene == "D22_Hutong") HoldPlaylist();
            else EnsurePlaylist();
            if (scene == "D22_Menu")
            {
                D22Feel.Clear();
                QuietBed();
                UI.ShowMenu();
            }
            else
            {
                SetupMood();
                blocked = false;
                UI.ShowHUD();
                if (scene == "D22_RecordShop") { bossMet = false; Dialogue(story.recordShop, Resume); }
                else if (scene == "D22_Hutong")
                {
                    doorHeard = false;
                    hutongAsked = false;
                    var hutongLighting = FindAnyObjectByType<D22HutongLighting>();
                    if (hutongLighting) hutongLighting.Apply(D22Feel.Dusk);
                    Dialogue(story.hutong, Resume);
                }
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
            if (IsLivehouse) Stage?.NoteDrink();
            if (Chapter == "D22_RecordShop")
            {
                blocked = true;
                D22Look.Unlock();
                UI.ShowChoices(story.choice, i => { D22Feel.SetDrink(i); SetupMood(); LoadSpace("D22_Hutong"); });
            }
            else if (Chapter != "D22_Hutong" && !IsLivehouse && Chapter != "D22_Performance" && Sips >= 5)
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
                Abilities.SetVolume(Volume);
                Abilities.Tick(FinishPour);
            }
            Hunt?.Tick(Camera.main, Volume);
            Stage?.Tick();
            if (!Loading && IsLivehouse && Stage != null && Stage.ShowReady)
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
                walked |= Vector3.Distance(camera.transform.position, shopEntry) > 1.5f;
                if (!recordShop && !bossMet && camera.transform.position.z < -5)
                { bossMet = true; Dialogue(story.boss, () => { SpawnBottle(); Resume(); }); return; }
                if (recordShop && !bossMet && recordShop.CanTalk(camera.transform) && k != null && k.eKey.wasPressedThisFrame)
                { TryInteract(); return; }
                aimingBottle = bottle && Vector3.Distance(camera.transform.position, bottle.transform.position) < 2 && Vector3.Dot(camera.transform.forward, (bottle.transform.position - camera.transform.position).normalized) > .86f;
                if (aimingBottle && k != null && k.eKey.wasPressedThisFrame) { TryInteract(); return; }
            }
            var exit = NearestExit(camera.transform.position);
            bool atExit = !IsLivehouse && exit;
            if (IsLivehouse && Stage != null)
            {
                if (!Stage.IntroPlayed && Stage.NearIntro(camera)) { MeetBand(); return; }
                if (Stage.TrioDone && !Stage.OnStagePlayed && Stage.NearStage(camera)) { StepOnStage(); return; }
                if (!loftHeard && InLoft(camera.transform.position)) { MeetLoft(); return; }
            }
            if (Chapter == "D22_Hutong" && !doorHeard && atExit)
            { MeetDoor(); return; }
            if (Chapter == "D22_Hutong" && atExit && Hunt != null && Hunt.Unlocked && Hunt.QuotaDone(Sips) && !hutongAsked)
            { AskHutong(); return; }
            string huntHint = Hunt?.AimHint(camera);
            string stageHint = IsLivehouse ? Stage?.Hint(camera) : null;
            string shopHint = recordShop
                ? !walked ? "往柜台走走" : !bossMet ? (recordShop.CanTalk(camera.transform) ? "E 和店主说话" : "去柜台找店主")
                : !HasDrink ? "柜台上有瓶酒" : Sips == 0 ? "1 喝酒" : "从侧门去胡同"
                : !bossMet ? "往里走走" : !HasDrink ? "回头拿酒瓶" : "1 喝酒";
            string hint = aimingBottle ? "E 拿酒瓶"
                : !string.IsNullOrEmpty(stageHint) ? stageHint
                : IsLivehouse && Stage != null && !Stage.IntroPlayed ? "往舞台走"
                : !string.IsNullOrEmpty(huntHint) ? huntHint
                : Chapter == "D22_Hutong" && Hunt != null && Hunt.Unlocked && !Hunt.QuotaDone(Sips) ? Hunt.Quota(Sips)
                : Chapter == "D22_Hutong" && atExit ? (doorHeard ? "已满" : "门")
                : atExit ? (recordShop && Sips == 0 ? "先在柜台拿酒" : "E " + (string.IsNullOrEmpty(exit.label) ? "进去" : ShortLabel(exit.label)))
                : Chapter == "D22_RecordShop" ? shopHint
                    : "";
            UI.UpdateHUD(hint, HasDrink, Sips, Abilities != null ? Abilities.CooldownLeft : Mathf.Max(0, cooldown - Time.unscaledTime));
            if (k != null && k.eKey.wasPressedThisFrame) TryInteract();
        }

        void LateUpdate()
        {
            if (Loading || !Camera.main) return;
            bool moving = !blocked && !AbilityOpen && !HuntOpen && !StageOpen && (Conductor == null || !Conductor.Running);
            if (!presenting && !moving) return;
            D22Feel.Apply(Camera.main, ReducedMotion);
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
            if (blocked || Loading || AbilityOpen || HuntOpen || StageOpen || !Camera.main) return;
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
            if (IsLivehouse && Stage != null && Stage.Interact(camera, (title, lines, after) => Dialogue(lines, after, title), story)) return;
            if (IsLivehouse) return;
            var exit = NearestExit(camera.transform.position);
            if (!exit || recordShop && Sips == 0) return;
            if (Chapter == "D22_Hutong")
            {
                if (Hunt != null && Hunt.Unlocked && Hunt.QuotaDone(Sips)) AskHutong();
                else MeetDoor();
                return;
            }
            if (exit.nextScene == "END") { blocked = true; UI.ShowEnding("这个名字，至今仍被大家口口相传。", () => LoadSpace("D22_Menu")); }
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

        void MeetBand()
        {
            if (Stage == null || Stage.IntroPlayed || story.band == null || story.band.Length == 0) return;
            Stage.MarkIntro();
            Dialogue(story.band, () => { Stage.Spawn(); Resume(); }, "三大件");
        }

        void MeetLoft()
        {
            if (loftHeard || story.loft == null || story.loft.Length == 0) return;
            loftHeard = true;
            Dialogue(story.loft, Resume, "二楼");
        }

        static bool InLoft(Vector3 p)
        {
            if (Instance != null && Instance.Chapter == "D22_LiveScan")
                return p.y > 2.5f && p.x > -5f && p.x < 5f && p.z < -5f && p.z > -24f;
            return p.y > 3.05f && p.y < 5.2f && p.x > -3.5f && p.x < 3.5f && p.z > 4f && p.z < 8.6f;
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
                D22Feel.SetLane(i);
                SetupMood();
                StartCoroutine(PresentLane());
            });
        }

        IEnumerator PresentLane()
        {
            presenting = true;
            UI.ShowHUD();
            if (D22Feel.Active == D22Feel.Kind.Made) yield return Glance();
            else
            {
                float t = 0f;
                while (t < 2.2f && !Loading)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }
            presenting = false;
            if (Loading) yield break;
            Dialogue(story.livehouse, () => LoadSpace(D22Bootstrap.SceneName));
        }

        IEnumerator Glance()
        {
            var camera = Camera.main;
            if (!camera) yield break;
            var targets = new System.Collections.Generic.List<Vector3>();
            var exit = FindAnyObjectByType<D22Exit>();
            if (exit) targets.Add(exit.transform.position + Vector3.up * 1.3f);
            var flock = FindAnyObjectByType<D22PigeonFlock>();
            var hutong = FindAnyObjectByType<D22HutongLighting>();
            if (flock) targets.Add(flock.Center);
            else if (hutong) targets.Add(hutong.pigeonMark);
            if (hutong) targets.Add(hutong.grindMark + Vector3.up * 1.2f);
            foreach (var target in targets)
            {
                if (Loading || !camera) yield break;
                Vector3 dir = target - camera.transform.position;
                if (dir.sqrMagnitude < .04f) continue;
                var from = D22Feel.Pose(camera.transform);
                var to = Quaternion.LookRotation(dir);
                float t = 0f;
                const float slice = 2.5f;
                while (t < slice && !Loading)
                {
                    t += Time.deltaTime;
                    camera.transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / slice));
                    D22Feel.NotePose(camera.transform);
                    yield return null;
                }
            }
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
            if (!moodVolume)
            {
                var go = new GameObject("Story Mood");
                go.transform.SetParent(transform, false);
                moodVolume = go.AddComponent<Volume>();
                moodVolume.isGlobal = true;
                moodVolume.priority = 50;
            }
            if (moodProfile) Destroy(moodProfile);
            var look = D22Feel.Current;
            moodVolume.weight = D22Feel.Active == D22Feel.Kind.None ? 0f : 1f;
            moodProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            moodVolume.sharedProfile = moodProfile;
            var color = moodProfile.Add<ColorAdjustments>(true);
            color.postExposure.Override(look.exposure);
            color.contrast.Override(look.contrast);
            color.saturation.Override(look.saturation);
            color.colorFilter.Override(look.filter);
            var vignette = moodProfile.Add<Vignette>(true);
            vignette.intensity.Override(look.vignette);
            vignette.smoothness.Override(look.vignetteSoft);
            var bloom = moodProfile.Add<Bloom>(true);
            bloom.intensity.Override(look.bloom);
            bloom.threshold.Override(look.bloomAt);
            var chroma = moodProfile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(look.chroma);
            if (look.mix == 0)
            {
                OpenBed(1.45f, .72f, 20000f, AudioReverbPreset.Off, 0f);
                Hunt?.SetBed(.4f);
            }
            else if (look.mix == 1)
            {
                OpenBed(.32f, 0f, 22000f, AudioReverbPreset.Off, 0f);
                Hunt?.SetBed(1.7f);
            }
            else if (look.mix == 2)
            {
                OpenBed(.5f, .18f, 480f, AudioReverbPreset.Cave, .34f);
                Hunt?.SetBed(.12f);
            }
            else QuietBed();
        }

        void OpenBed(float gain, float distort, float cutoff, AudioReverbPreset room, float humLevel)
        {
            bedGain = gain;
            ApplyMusicLevel();
            distortion.distortionLevel = distort;
            lowPass.cutoffFrequency = cutoff;
            reverb.reverbPreset = room;
            playlistHeld = false;
            if (audioSource.clip == null || audioSource.clip == finaleClip)
            {
                if (playlist != null && playlist.Length > 0)
                {
                    if (playlistIndex < 0) playlistIndex = 0;
                    onFinale = false;
                    audioSource.loop = false;
                    audioSource.clip = playlist[playlistIndex];
                }
            }
            if (audioSource.clip != null && audioSource.clip != finaleClip && !audioSource.isPlaying)
            {
                audioSource.UnPause();
                if (!audioSource.isPlaying) audioSource.Play();
            }
            if (humLevel > .01f)
            {
                EnsureHum();
                hum.volume = humLevel;
                if (!hum.isPlaying) hum.Play();
            }
            else if (hum && hum.isPlaying) hum.Stop();
        }

        void QuietBed()
        {
            bedGain = 1f;
            ApplyMusicLevel();
            if (distortion) distortion.distortionLevel = 0f;
            if (lowPass) lowPass.cutoffFrequency = 22000f;
            if (reverb) reverb.reverbPreset = AudioReverbPreset.Off;
            if (hum && hum.isPlaying) hum.Stop();
            Hunt?.SetBed(1f);
            if (moodVolume) moodVolume.weight = D22Feel.Active == D22Feel.Kind.None ? 0f : moodVolume.weight;
        }

        void ApplyMusicLevel()
        {
            if (audioSource) audioSource.volume = musicLevel * bedGain;
        }

        void EnsureHum()
        {
            if (hum) return;
            hum = gameObject.AddComponent<AudioSource>();
            hum.playOnAwake = false;
            hum.loop = true;
            hum.spatialBlend = 0;
            int rate = 22050;
            int n = rate;
            var clip = AudioClip.Create("mains-hum", n, 1, rate, false);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(t * 50f * Mathf.PI * 2f) * .55f
                    + Mathf.Sin(t * 100f * Mathf.PI * 2f) * .18f
                    + Mathf.Sin(t * 150f * Mathf.PI * 2f) * .08f;
            }
            clip.SetData(data, 0);
            hum.clip = clip;
        }
    }
}
