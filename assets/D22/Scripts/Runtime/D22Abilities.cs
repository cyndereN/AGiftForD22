using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace D22
{
    public sealed class D22AbilityInfo
    {
        public string id, title, caption, mark, zh, en;
        public string key;
    }

    public sealed class D22Abilities
    {
        public const int SlotCount = 7;
        public static readonly D22AbilityInfo[] Slots =
        {
            new() { id="drink",    key="1", mark="酒", title="喝酒",     caption="点击倾倒",   zh="情绪放大器，还请您切莫贪杯。", en="An amplifier for feeling. Please don't drink past your limit." },
            new() { id="cricket",  key="2", mark="蛐", title="蛐蛐",     caption="点击唤叫",   zh="请您拨开草丛，里面藏着一只蛐蛐。", en="Part the grass. A cricket is hiding inside." },
            new() { id="pigeon",   key="3", mark="哨", title="鸽哨",     caption="拖动盘旋",   zh="天上飞过一群鸽子，请您捉住其中一只，它的尾巴系着鸽哨。", en="A flock is passing overhead. Catch one. A pigeon whistle is tied to its tail." },
            new() { id="scissors", key="4", mark="磨", title="磨剪子",   caption="来回推磨",   zh="胡同深处传来吆喝：磨剪子嘞，戗菜刀。", en="From deep in the hutong, a cry: sharpen the scissors, sharpen the knife." },
            new() { id="guitar",   key="5", mark="吉", title="吉他",     caption="拨弦 / 扫弦", zh="吉他是弥漫在空气之中的一层情绪。和弦、噪音，一句未曾唱出的心里话。", en="The guitar is a layer of feeling hung in the air. Chords, noise, a sentence of the heart not yet sung." },
            new() { id="bass",     key="6", mark="贝", title="贝斯",     caption="按住拨弦",   zh="贝斯音高最低，也最容易被忽略。与其说听见它，不如说脚下切实感受到了根基。", en="The bass is the lowest, and the easiest to miss. You do not so much hear it as feel the floor under your feet." },
            new() { id="drums",    key="7", mark="鼓", title="鼓",       caption="点击敲击",   zh="鼓掌管时间。一次敲击，整个空间便拥有了当下。", en="The drum keeps the time. One strike, and the whole room has a now." }
        };

        public static readonly D22AbilityInfo Walk = new() { id="walk", mark="走", title="行走", caption="", zh="", en="" };

        readonly HashSet<string> learned = new();
        readonly AudioSource oneshot, loop, bandShot, bandLoop;
        readonly GameObject previewRoot;
        readonly Transform bottlePreview;
        readonly Camera bottleCam;
        readonly AudioClip pourClip, cricketClip, pigeonLoop, scrapeLoop;
        AudioClip knifeClip;
        readonly AudioClip[] guitarClips = new AudioClip[6];
        readonly AudioClip[] bassClips = new AudioClip[4];
        readonly AudioClip[] drumClips = new AudioClip[3];
        AudioClip pourSfx;
        Vector2 pointer, prevPointer;
        bool held;
        int lastString = -1;

        public string OpenId { get; private set; }
        public bool Open => OpenId != null;
        public bool Pouring { get; private set; }
        public float PourT { get; private set; }
        public float BottleTilt { get; private set; }
        public float CooldownUntil { get; private set; }
        public float CricketPulse { get; private set; }
        public float PigeonAngle { get; private set; }
        public float PigeonSpin { get; private set; }
        public float ScissorsX { get; private set; } = .5f;
        public float ScissorsSpark { get; private set; }
        public int LitString { get; private set; } = -1;
        public int LitPad { get; private set; } = -1;
        public bool WalkHot { get; set; }
        public RenderTexture BottleView { get; }
        public bool HasBottlePreview => bottlePreview != null;
        public bool HasWalk => learned.Contains("walk");
        public bool Cooling => Time.unscaledTime < CooldownUntil;
        public float CooldownLeft => Mathf.Max(0, CooldownUntil - Time.unscaledTime);

        public D22Abilities(Transform host, GameObject bottlePrefab, float volume)
        {
            oneshot = host.gameObject.AddComponent<AudioSource>();
            oneshot.playOnAwake = false;
            oneshot.spatialBlend = 0;
            loop = host.gameObject.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            loop.spatialBlend = 0;
            bandShot = host.gameObject.AddComponent<AudioSource>();
            bandShot.playOnAwake = false;
            bandShot.spatialBlend = 0;
            bandLoop = host.gameObject.AddComponent<AudioSource>();
            bandLoop.playOnAwake = false;
            bandLoop.loop = true;
            bandLoop.spatialBlend = 0;
            SetVolume(volume);

            pourClip = MakePour();
            pourSfx = pourClip;
            cricketClip = MakeCricket();
            pigeonLoop = MakeWhistle();
            scrapeLoop = MakeScrape();
            float[] gFreq = { 82.41f, 110f, 146.83f, 196f, 246.94f, 329.63f };
            for (int i = 0; i < 6; i++) guitarClips[i] = Pluck("g" + i, gFreq[i], 1.6f, .992f);
            float[] bFreq = { 41.2f, 55f, 73.42f, 98f };
            for (int i = 0; i < 4; i++) bassClips[i] = Pluck("b" + i, bFreq[i], 2.2f, .996f);
            drumClips[0] = MakeKick();
            drumClips[1] = MakeSnare();
            drumClips[2] = MakeHat();

            BottleView = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "D22 Wine View" };
            previewRoot = new GameObject("D22 Wine Preview");
            UnityEngine.Object.DontDestroyOnLoad(previewRoot);
            previewRoot.transform.position = new Vector3(0, -120, 0);
            int layer = LayerMask.NameToLayer("D22Preview");
            if (layer < 0) layer = 0;
            previewRoot.layer = layer;
            if (bottlePrefab)
            {
                var bottle = UnityEngine.Object.Instantiate(bottlePrefab, previewRoot.transform);
                bottle.name = "Wine Preview";
                bottle.transform.localPosition = Vector3.zero;
                bottle.transform.localScale = Vector3.one;
                bottlePreview = bottle.transform;
                SetLayer(bottle, layer);
                var unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit)
                    foreach (var renderer in bottle.GetComponentsInChildren<Renderer>())
                    {
                        var mats = renderer.materials;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            var src = mats[i];
                            var copy = new Material(unlit);
                            if (src && src.HasProperty("_BaseMap") && src.GetTexture("_BaseMap"))
                                copy.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
                            else if (copy.HasProperty("_BaseColor"))
                                copy.SetColor("_BaseColor", new Color(.45f, .16f, .14f));
                            mats[i] = copy;
                        }
                        renderer.materials = mats;
                    }
            }
            var lamp = new GameObject("Wine Light").AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 10;
            lamp.intensity = 6;
            lamp.color = new Color(1f, .92f, .82f);
            lamp.cullingMask = 1 << layer;
            lamp.transform.SetParent(previewRoot.transform, false);
            lamp.transform.localPosition = new Vector3(.55f, 1.1f, 1.05f);
            lamp.gameObject.layer = layer;
            var camGo = new GameObject("Wine Camera");
            camGo.transform.SetParent(previewRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0, .42f, 1.55f);
            camGo.transform.LookAt(previewRoot.transform.position + Vector3.up * .22f);
            bottleCam = camGo.AddComponent<Camera>();
            bottleCam.clearFlags = CameraClearFlags.SolidColor;
            bottleCam.backgroundColor = new Color(.07f, .065f, .06f, 1);
            bottleCam.fieldOfView = 26;
            bottleCam.nearClipPlane = .02f;
            bottleCam.farClipPlane = 8;
            bottleCam.cullingMask = 1 << layer;
            bottleCam.cameraType = CameraType.Preview;
            bottleCam.depth = -20;
            bottleCam.enabled = false;
            bottleCam.targetTexture = BottleView;
            var camData = bottleCam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;
            camData.renderShadows = false;
            camGo.layer = layer;
            PoseBottle();
        }

        public void SetVolume(float volume)
        {
            oneshot.volume = Mathf.Clamp01(volume) * .85f;
            loop.volume = Mathf.Clamp01(volume) * .7f;
            bandShot.volume = 1f;
            bandLoop.volume = 1f;
        }

        public void SetKnife(AudioClip clip)
        {
            if (clip) knifeClip = clip;
        }

        public void SetBeer(AudioClip beer)
        {
            if (beer) pourSfx = beer;
        }

        public void ResetProgress()
        {
            Close();
            learned.Clear();
            CooldownUntil = 0;
        }

        public bool Learned(string id) => learned.Contains(id);
        public static D22AbilityInfo Info(string id)
        {
            if (id == Walk.id) return Walk;
            for (int i = 0; i < Slots.Length; i++) if (Slots[i].id == id) return Slots[i];
            return null;
        }

        public static void ApplyStory(D22AbilityCopy[] copies)
        {
            if (copies == null) return;
            foreach (var copy in copies)
            {
                var info = Info(copy.id);
                if (info == null) continue;
                if (!string.IsNullOrEmpty(copy.title)) info.title = copy.title;
                if (!string.IsNullOrEmpty(copy.caption)) info.caption = copy.caption;
                if (!string.IsNullOrEmpty(copy.zh)) info.zh = copy.zh;
                if (!string.IsNullOrEmpty(copy.en)) info.en = copy.en;
            }
        }

        public bool Learn(string id)
        {
            if (Info(id) == null || learned.Contains(id)) return false;
            learned.Add(id);
            return true;
        }

        public bool CanOpen(string id)
        {
            if (id == "drink" && Cooling) return false;
            return Info(id) != null && learned.Contains(id);
        }

        public bool Toggle(string id)
        {
            if (OpenId == id) { Close(); return false; }
            if (!CanOpen(id)) return false;
            OpenId = id;
            held = false;
            lastString = -1;
            LitString = -1;
            LitPad = -1;
            if (id == "drink") bottleCam.Render();
            return true;
        }

        public void Close()
        {
            Pouring = false;
            PourT = 0;
            BottleTilt = 0;
            OpenId = null;
            held = false;
            StopLoop();
            PoseBottle();
        }

        public bool StartPour()
        {
            if (OpenId != "drink" || Pouring || Cooling) return false;
            Pouring = true;
            PourT = 0;
            Play(pourSfx ? pourSfx : pourClip);
            return true;
        }

        public void PointerDown(Vector2 n)
        {
            pointer = prevPointer = n;
            held = true;
            if (OpenId == "drink") StartPour();
            else if (OpenId == "cricket") Chirp();
            else if (OpenId == "guitar") Strum(n, n);
            else if (OpenId == "bass") HoldBass(n);
            else if (OpenId == "drums") HitDrum(n);
        }

        public void PointerDrag(Vector2 n)
        {
            prevPointer = pointer;
            pointer = n;
            if (!held) return;
            if (OpenId == "guitar") Strum(prevPointer, pointer);
            else if (OpenId == "bass") HoldBass(n);
            else if (OpenId == "pigeon") SpinPigeon(n - prevPointer);
            else if (OpenId == "scissors") Grind(n);
        }

        public void PointerUp()
        {
            held = false;
            lastString = -1;
            if (OpenId == "bass") StopBand();
            else if (OpenId == "pigeon" || OpenId == "scissors") StopLoop();
        }

        public bool Tick(Action onPoured)
        {
            float dt = Time.unscaledDeltaTime;
            CricketPulse = Mathf.MoveTowards(CricketPulse, 0, dt * 1.8f);
            ScissorsSpark = Mathf.MoveTowards(ScissorsSpark, 0, dt * 2.4f);
            if (LitPad >= 0 && !held) LitPad = -1;
            if (OpenId == "pigeon" && held)
            {
                PigeonSpin = Mathf.MoveTowards(PigeonSpin, 0, dt * 1.6f);
                loop.pitch = Mathf.Lerp(0.7f, 1.8f, Mathf.Clamp01(PigeonSpin));
                loop.volume = oneshot.volume * Mathf.Clamp01(PigeonSpin * 1.4f);
            }
            if (OpenId == "scissors" && held)
                loop.volume = oneshot.volume * Mathf.Clamp01(ScissorsSpark);
            if (Pouring)
            {
                PourT += dt;
                float t = Mathf.Clamp01(PourT / 1.35f);
                float lift = t < .4f ? t / .4f : Mathf.Max(0, 1 - (t - .4f) / .6f);
                BottleTilt = lift * 112;
                PoseBottle();
                if (OpenId == "drink") bottleCam.Render();
                if (t >= 1)
                {
                    Pouring = false;
                    PourT = 0;
                    BottleTilt = 0;
                    PoseBottle();
                    CooldownUntil = Time.unscaledTime + 5;
                    Close();
                    onPoured?.Invoke();
                    return true;
                }
            }
            else if (OpenId == "drink")
            {
                PoseBottle();
                bottleCam.Render();
            }
            return false;
        }

        public void Dispose()
        {
            if (BottleView) BottleView.Release();
            if (previewRoot) UnityEngine.Object.Destroy(previewRoot);
        }

        void Chirp()
        {
            CricketPulse = 1;
            Play(cricketClip, UnityEngine.Random.Range(.92f, 1.08f));
        }

        void SpinPigeon(Vector2 delta)
        {
            float speed = delta.magnitude * 8;
            PigeonSpin = Mathf.Clamp01(PigeonSpin * .6f + speed);
            PigeonAngle = (PigeonAngle + delta.x * 240 + delta.y * 40) % 360;
            EnsureLoop(pigeonLoop, Mathf.Lerp(.7f, 1.8f, PigeonSpin));
        }

        void Grind(Vector2 n)
        {
            ScissorsX = Mathf.Clamp01(n.x);
            ScissorsSpark = Mathf.Clamp01(Mathf.Abs(n.x - prevPointer.x) * 14);
            EnsureLoop(knifeClip ? knifeClip : scrapeLoop, Mathf.Lerp(.85f, 1.2f, ScissorsX));
        }

        void Strum(Vector2 from, Vector2 to)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = (i + 1) / 7f;
                if (!Crossed(from.x, to.x, x) && Mathf.Abs(to.x - x) > .045f) continue;
                if (i == lastString) continue;
                lastString = i;
                LitString = i;
                PlayBand(guitarClips[i]);
            }
        }

        void HoldBass(Vector2 n)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(n.x * 4), 0, 3);
            LitString = i;
            if (bandLoop.clip != bassClips[i] || !bandLoop.isPlaying)
            {
                bandLoop.clip = bassClips[i];
                bandLoop.volume = 1f;
                bandLoop.pitch = 1f;
                bandLoop.Play();
            }
        }

        void HitDrum(Vector2 n)
        {
            int pad = n.x < .33f ? 0 : n.x < .66f ? 1 : 2;
            LitPad = pad;
            PlayDrum(pad);
        }

        public void PlayGuitar(int i)
        {
            if (i >= 0 && i < guitarClips.Length) PlayBand(guitarClips[i]);
        }

        public void PlayBass(int i)
        {
            if (i >= 0 && i < bassClips.Length) PlayBand(bassClips[i], .85f);
        }

        public void PlayDrum(int pad)
        {
            if (pad >= 0 && pad < drumClips.Length) PlayBand(drumClips[pad]);
        }

        public void TapSlot(int slot)
        {
            if (slot < 1 || slot >= Slots.Length) return;
            switch (Slots[slot].id)
            {
                case "cricket": Play(cricketClip); break;
                case "pigeon": Play(pigeonLoop, 1.15f); break;
                case "scissors": Play(knifeClip ? knifeClip : scrapeLoop); break;
                case "guitar": PlayGuitar(UnityEngine.Random.Range(0, guitarClips.Length)); break;
                case "bass": PlayBass(UnityEngine.Random.Range(0, bassClips.Length)); break;
                case "drums": PlayDrum(UnityEngine.Random.Range(0, drumClips.Length)); break;
            }
        }

        void Play(AudioClip clip, float pitch = 1)
        {
            if (!clip) return;
            oneshot.pitch = pitch;
            oneshot.PlayOneShot(clip);
        }

        void PlayBand(AudioClip clip, float pitch = 1)
        {
            if (!clip) return;
            bandShot.pitch = pitch;
            bandShot.volume = 1f;
            bandShot.PlayOneShot(clip);
        }

        void StopBand()
        {
            bandLoop.Stop();
            bandLoop.clip = null;
        }

        void EnsureLoop(AudioClip clip, float pitch)
        {
            if (loop.clip != clip) { loop.clip = clip; loop.Play(); }
            else if (!loop.isPlaying) loop.Play();
            loop.pitch = pitch;
        }

        void StopLoop()
        {
            loop.Stop();
            loop.clip = null;
        }

        void PoseBottle()
        {
            if (bottlePreview) bottlePreview.localRotation = Quaternion.Euler(BottleTilt, 28, 0);
        }

        static bool Crossed(float a, float b, float x) => (a - x) * (b - x) <= 0;

        static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        static AudioClip MakePour()
        {
            return Build("pour", 44100, 44100, i =>
            {
                float t = i / 44100f;
                float noise = (UnityEngine.Random.value * 2 - 1) * Mathf.Lerp(.35f, .08f, t);
                float tone = Mathf.Sin(t * 90 * Mathf.PI * 2) * .12f * (1 - t);
                float env = t < .08f ? t / .08f : Mathf.Pow(1 - t, .45f);
                return (noise + tone) * env;
            });
        }

        static AudioClip MakeCricket()
        {
            const int rate = 44100;
            return Build("cricket", (int)(rate * .55f), rate, i =>
            {
                float t = i / (float)rate;
                int pulse = Mathf.FloorToInt(t / .055f);
                float local = t - pulse * .055f;
                if (pulse > 5 || local > .018f) return 0;
                float env = Mathf.Sin(local / .018f * Mathf.PI);
                return Mathf.Sin(t * 3800 * Mathf.PI * 2) * env * .45f;
            });
        }

        static AudioClip MakeWhistle()
        {
            const int rate = 44100;
            return Build("pigeon", rate, rate, i =>
            {
                float t = i / (float)rate;
                float wobble = 1 + .03f * Mathf.Sin(t * 7 * Mathf.PI * 2);
                return (Mathf.Sin(t * 980 * wobble * Mathf.PI * 2) + .35f * Mathf.Sin(t * 1470 * wobble * Mathf.PI * 2)) * .22f;
            });
        }

        static AudioClip MakeScrape()
        {
            const int rate = 44100;
            return Build("scrape", rate / 2, rate, i =>
            {
                float n = UnityEngine.Random.value * 2 - 1;
                float t = i / (float)rate;
                float metal = Mathf.Sin(t * 2100 * Mathf.PI * 2) * .2f;
                return (n * .55f + metal) * .28f;
            });
        }

        static AudioClip Pluck(string name, float freq, float seconds, float decay)
        {
            int rate = 44100;
            int n = Mathf.Max(2, (int)(rate / freq));
            var buf = new float[n];
            for (int i = 0; i < n; i++) buf[i] = UnityEngine.Random.value * 2 - 1;
            int samples = (int)(seconds * rate);
            var data = new float[samples];
            int idx = 0;
            for (int i = 0; i < samples; i++)
            {
                float v = buf[idx];
                int next = (idx + 1) % n;
                buf[idx] = (v + buf[next]) * .5f * decay;
                data[i] = v * .45f;
                idx = next;
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeKick()
        {
            return Build("kick", 12000, 44100, i =>
            {
                float t = i / 44100f;
                float f = Mathf.Lerp(140, 40, t * 6);
                return Mathf.Sin(t * f * Mathf.PI * 2) * Mathf.Exp(-t * 8) * .7f;
            });
        }

        static AudioClip MakeSnare()
        {
            return Build("snare", 14000, 44100, i =>
            {
                float t = i / 44100f;
                float tone = Mathf.Sin(t * 180 * Mathf.PI * 2) * Mathf.Exp(-t * 12);
                float noise = (UnityEngine.Random.value * 2 - 1) * Mathf.Exp(-t * 16);
                return (tone * .35f + noise * .55f);
            });
        }

        static AudioClip MakeHat()
        {
            return Build("hat", 6000, 44100, i =>
            {
                float t = i / 44100f;
                return (UnityEngine.Random.value * 2 - 1) * Mathf.Exp(-t * 40) * .35f;
            });
        }

        static AudioClip Build(string name, int samples, int rate, Func<int, float> wave)
        {
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = wave(i);
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
