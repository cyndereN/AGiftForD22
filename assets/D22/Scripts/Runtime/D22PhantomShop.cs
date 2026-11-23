using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace D22
{
    // Shadow sight. The grade goes dim, and unrecorded sources stay hidden
    // until their sound is kept. Figures never cross into normal sight.
    public sealed class D22PhantomShop : MonoBehaviour
    {
        public const float RecordSeconds = 3f;

        public bool Active { get; private set; }
        public bool Aiming { get; private set; }
        public float Record01 { get; private set; }

        sealed class Signal
        {
            public string id;
            public GameObject body;
            public Transform beacon;
            public LineRenderer[] rings;
            public float[] phase;
            public AudioSource voice;
            public AudioClip clip;
            public bool recorded;
            public float aimRange = 3.2f;
            public float hearRange = 8f;
        }

        D22GameFlow flow;
        readonly List<Signal> signals = new();
        GameObject figures;
        Volume shadowGrade;
        VolumeProfile shadowProfile;
        string aimId;
        GameObject recorder;
        Material shade;
        Material signal;
        Texture2D ringTex;
        float capturedFlash;
        const int Segments = 40;

        public bool Recorded => IsRecorded("drink");
        public bool HasPending
        {
            get
            {
                foreach (var signal in signals)
                    if (signal.body && !signal.recorded) return true;
                return false;
            }
        }

        public bool IsRecorded(string id)
        {
            foreach (var signal in signals)
                if (signal.id == id) return signal.recorded;
            return false;
        }

        public string PendingHint()
        {
            if (flow == null || !HasPending) return null;
            if (flow.Chapter != "D22_Hutong" && flow.Chapter != "D22_Bootstrap" && flow.Chapter != "D22_LiveScan") return null;
            if (!Active) return "V 暗影";
            return Aiming ? "按住 R，录 3 秒" : "顺着电波找声音";
        }

        public void Bind(D22GameFlow host) => flow = host;

        bool InField => flow != null && (flow.Chapter == "D22_RecordShop" || flow.Chapter == "D22_Hutong" || flow.Chapter == "D22_Bootstrap" || flow.Chapter == "D22_LiveScan");

        public void OnChapter(string scene)
        {
            Active = false;
            Record01 = 0f;
            Aiming = false;
            aimId = null;
            capturedFlash = 0f;
            signals.Clear();
            if (figures) Destroy(figures);
            figures = null;
            ApplySight();
            if (scene == "D22_RecordShop") BuildShopFigures();
            else if (scene == "D22_Hutong") BuildHutongFigures();
            else if (scene == "D22_LiveScan") BuildLivehouseFigures(new Vector3(-3.1f, .7f, -15.6f), new Vector3(-1.1f, .95f, -16.4f), new Vector3(1.1f, .7f, -15.6f), null);
            ShowRecorder(InField);
        }

        public void PlaceLivehouse(D22Bootstrap live)
        {
            if (!live) return;
            if (figures) Destroy(figures);
            figures = null;
            BuildLivehouseFigures(live.drumSpot, live.guitarSpot, live.bassSpot, live.transform);
            ApplySight();
        }

        public void Toggle()
        {
            if (!InField || flow.Loading || D22GameFlow.InputBlocked) return;
            Active = !Active;
            ApplySight();
        }

        public void AttachBottle(GameObject bottle)
        {
            AttachSource("drink", bottle, null, 3.2f, 8f, Vector3.zero, 0.16f);
        }

        public void AttachSource(string id, GameObject body, AudioClip clip, float aimRange, float hearRange, Vector3 fallback, float beaconHeight)
        {
            if (!body) body = MakeProp(id, fallback);
            var source = new Signal
            {
                id = id,
                body = body,
                clip = clip,
                aimRange = aimRange,
                hearRange = hearRange
            };
            var go = new GameObject(id + " Signal");
            go.transform.SetParent(body.transform, false);
            go.transform.localPosition = new Vector3(0f, beaconHeight, 0f);
            source.beacon = go.transform;
            source.rings = new LineRenderer[3];
            source.phase = new float[3];
            var mat = SignalMaterial();
            for (int i = 0; i < source.rings.Length; i++)
            {
                var ring = new GameObject("Ring");
                ring.transform.SetParent(go.transform, false);
                var line = ring.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = Segments;
                line.widthMultiplier = 0.02f;
                line.numCapVertices = 2;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = mat;
                line.textureMode = LineTextureMode.Stretch;
                source.rings[i] = line;
                source.phase[i] = i / 3f;
            }
            source.voice = go.AddComponent<AudioSource>();
            source.voice.playOnAwake = false;
            source.voice.loop = true;
            source.voice.spatialBlend = 1f;
            source.voice.dopplerLevel = 0f;
            source.voice.rolloffMode = AudioRolloffMode.Linear;
            source.voice.minDistance = 0.6f;
            source.voice.maxDistance = hearRange;
            source.voice.volume = 0.9f;
            signals.Add(source);
            ApplySight();
        }

        void Update()
        {
            AnimateRings();
            TickRecord();
            if (capturedFlash > 0f) capturedFlash -= Time.deltaTime;
        }

        void LateUpdate()
        {
            if (!recorder || !recorder.activeSelf || !Camera.main) return;
            Transform cam = Camera.main.transform;
            recorder.transform.SetPositionAndRotation(cam.TransformPoint(0.24f, -0.18f, 0.48f), cam.rotation);
        }

        void OnGUI()
        {
            if (!InField || flow.Loading) return;
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            if (Active && Camera.main)
            {
                foreach (var source in signals)
                    if (source.beacon && !source.recorded) DrawBeaconMarker(Camera.main, source.beacon);
            }
            if (Record01 > 0.001f && Aiming) DrawRec();
            else if (capturedFlash > 0f) DrawCaptured();
            GUI.matrix = matrix;
        }

        void BuildShopFigures()
        {
            var shop = FindAnyObjectByType<D22RecordShop>();
            if (!shop || !shop.shopkeeper) return;
            figures = new GameObject("Phantom Figures");
            figures.transform.SetParent(shop.transform, true);
            Vector3 boss = shop.shopkeeper.position;
            Figure("Boss", boss, 1.72f, false, new Color(0.07f, 0.045f, 0.04f, 0.94f), 200f);
            Figure("Customer", boss + new Vector3(-1.35f, 0f, -1.9f), 1.62f, false, new Color(0.02f, 0.02f, 0.028f, 0.92f), 24f);
            Figure("Customer Sitting", boss + new Vector3(1.25f, -0.15f, -1.15f), 1.55f, true, new Color(0.025f, 0.02f, 0.03f, 0.92f), 150f);
            Figure("Customer Far", boss + new Vector3(0.85f, 0f, -3.05f), 1.58f, false, new Color(0.015f, 0.016f, 0.02f, 0.9f), -30f);
            figures.SetActive(false);
        }

        void BuildHutongFigures()
        {
            var hutong = FindAnyObjectByType<D22HutongLighting>();
            if (!hutong) return;
            figures = new GameObject("Phantom Figures");
            figures.transform.SetParent(hutong.transform, true);
            Vector3 cricket = Grounded(hutong.cricketMark);
            Vector3 stone = Grounded(hutong.grindMark);
            Figure("Resident", cricket + new Vector3(1.2f, 1.55f, 0.4f), 1.68f, false, new Color(0.05f, 0.04f, 0.045f, 0.92f), 40f);
            Figure("Resident Sitting", stone + new Vector3(-0.9f, 1.35f, 0.5f), 1.55f, true, new Color(0.03f, 0.025f, 0.03f, 0.92f), -20f);
            Figure("Resident Far", Vector3.Lerp(cricket, stone, 0.45f) + new Vector3(0.4f, 1.6f, -1.6f), 1.62f, false, new Color(0.02f, 0.02f, 0.028f, 0.9f), 110f);
            figures.SetActive(false);
        }

        void BuildLivehouseFigures(Vector3 drum, Vector3 guitar, Vector3 bass, Transform parent)
        {
            figures = new GameObject("Phantom Figures");
            if (parent) figures.transform.SetParent(parent, true);
            Figure("Player", new Vector3(drum.x - 1.5f, 1.65f, drum.z + 1.4f), 1.72f, false, new Color(0.06f, 0.04f, 0.045f, 0.94f), 160f);
            Figure("Player Sitting", new Vector3(guitar.x + 0.2f, 1.4f, guitar.z + 1.8f), 1.58f, true, new Color(0.03f, 0.025f, 0.03f, 0.92f), 10f);
            Figure("Player Far", new Vector3(bass.x + 1.3f, 1.62f, bass.z + 1.1f), 1.66f, false, new Color(0.02f, 0.02f, 0.03f, 0.9f), -30f);
            figures.SetActive(false);
        }

        GameObject MakeProp(string id, Vector3 pos)
        {
            var root = new GameObject(id);
            root.transform.position = pos;
            if (id == "drums")
            {
                Prop(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0.15f), new Vector3(0.7f, 0.28f, 0.7f), Quaternion.identity, new Color(0.12f, 0.07f, 0.05f, 1f));
                Prop(root.transform, PrimitiveType.Cylinder, new Vector3(-0.28f, 0.55f, -0.05f), new Vector3(0.32f, 0.16f, 0.32f), Quaternion.identity, new Color(0.16f, 0.09f, 0.06f, 1f));
                Prop(root.transform, PrimitiveType.Cylinder, new Vector3(0.3f, 0.58f, -0.02f), new Vector3(0.28f, 0.14f, 0.28f), Quaternion.identity, new Color(0.18f, 0.1f, 0.07f, 1f));
                Prop(root.transform, PrimitiveType.Cylinder, new Vector3(0.42f, 0.82f, 0f), new Vector3(0.34f, 0.02f, 0.34f), Quaternion.identity, new Color(0.55f, 0.5f, 0.42f, 1f));
            }
            else if (id == "guitar" || id == "bass")
            {
                bool bass = id == "bass";
                var wood = bass ? new Color(0.1f, 0.08f, 0.07f, 1f) : new Color(0.42f, 0.24f, 0.12f, 1f);
                Prop(root.transform, PrimitiveType.Cube, new Vector3(0f, bass ? 0.55f : 0.62f, 0f), bass ? new Vector3(0.22f, 0.4f, 0.07f) : new Vector3(0.32f, 0.42f, 0.07f), Quaternion.identity, wood);
                Prop(root.transform, PrimitiveType.Cube, new Vector3(0f, bass ? 1.15f : 1.12f, 0f), bass ? new Vector3(0.05f, 0.7f, 0.04f) : new Vector3(0.055f, 0.55f, 0.04f), Quaternion.identity, new Color(0.2f, 0.14f, 0.09f, 1f));
            }
            else
            {
                Prop(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), Vector3.one * 0.28f, Quaternion.identity, new Color(0.28f, 0.24f, 0.2f, 1f));
            }
            return root;
        }

        void Prop(Transform parent, PrimitiveType shape, Vector3 localPos, Vector3 scale, Quaternion rotation, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>();
            if (collider) Destroy(collider);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = ShadeMaterial(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Vector3 Grounded(Vector3 mark) => new Vector3(mark.x, 0f, mark.z);

        void Figure(string name, Vector3 head, float height, bool sitting, Color color, float yaw)
        {
            float bodyHeight = sitting ? height * 0.58f : height;
            var root = new GameObject(name);
            root.transform.SetParent(figures.transform, true);
            root.transform.SetPositionAndRotation(head - Vector3.up * (bodyHeight - 0.18f), Quaternion.Euler(0f, yaw, 0f));
            var mat = ShadeMaterial(color);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, bodyHeight * 0.42f, 0f);
            body.transform.localScale = new Vector3(0.34f, bodyHeight * 0.36f, 0.28f);
            Strip(body, mat);
            var skull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skull.name = "Head";
            skull.transform.SetParent(root.transform, false);
            skull.transform.localPosition = new Vector3(0f, bodyHeight * 0.86f, 0.02f);
            skull.transform.localScale = Vector3.one * 0.22f;
            Strip(skull, mat);
            if (!sitting) return;
            var stool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stool.name = "Stool";
            stool.transform.SetParent(root.transform, false);
            stool.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            stool.transform.localScale = new Vector3(0.42f, 0.08f, 0.42f);
            Strip(stool, mat);
        }

        static void Strip(GameObject go, Material mat)
        {
            var collider = go.GetComponent<Collider>();
            if (collider) DestroyImmediate(collider);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void AnimateRings()
        {
            if (!Active) return;
            foreach (var source in signals)
            {
                if (source.recorded || source.rings == null) continue;
                for (int i = 0; i < source.rings.Length; i++)
                {
                    var line = source.rings[i];
                    if (!line) continue;
                    source.phase[i] = Mathf.Repeat(source.phase[i] + Time.deltaTime * 0.33f, 1f);
                    float u = source.phase[i];
                    float radius = Mathf.Lerp(0.06f, 1.45f, u);
                    float alpha = (1f - u) * 0.9f;
                    var color = new Color(0.72f, 0.93f, 1f, alpha);
                    line.startColor = color;
                    line.endColor = color;
                    for (int p = 0; p < Segments; p++)
                    {
                        float a = p / (float)Segments * Mathf.PI * 2f;
                        line.SetPosition(p, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                    }
                }
            }
        }

        void TickRecord()
        {
            Aiming = false;
            if (!Active || D22GameFlow.InputBlocked || !Camera.main)
            {
                Record01 = 0f;
                aimId = null;
                if (D22GameFlow.InputBlocked)
                {
                    foreach (var source in signals)
                        if (source.voice && source.voice.isPlaying) source.voice.Stop();
                }
                return;
            }
            Transform cam = Camera.main.transform;
            Signal best = null;
            float bestDist = float.MaxValue;
            foreach (var source in signals)
            {
                if (source.recorded || !source.beacon) continue;
                Vector3 to = source.beacon.position - cam.position;
                float dist = to.magnitude;
                if (dist < 0.2f || dist > source.aimRange) continue;
                if (Vector3.Dot(cam.forward, to / dist) <= 0.78f) continue;
                if (dist >= bestDist) continue;
                best = source;
                bestDist = dist;
            }
            if (best == null || best.id != aimId)
            {
                Record01 = 0f;
                aimId = best != null ? best.id : null;
            }
            Aiming = best != null;
            if (!Aiming) return;
            bool holding = Keyboard.current != null && Keyboard.current.rKey.isPressed;
            Record01 = holding ? Mathf.Min(1f, Record01 + Time.deltaTime / RecordSeconds) : 0f;
            if (Record01 < 1f) return;
            best.recorded = true;
            Record01 = 1f;
            capturedFlash = 1.6f;
            aimId = null;
            ApplySight();
            flow?.GrantRecorded(best.id);
        }

        void ShowRecorder(bool on)
        {
            if (on && !recorder) recorder = BuildRecorder();
            if (recorder) recorder.SetActive(on);
        }

        GameObject BuildRecorder()
        {
            var root = new GameObject("Field Recorder");
            var mat = ShadeMaterial(new Color(0.12f, 0.12f, 0.13f, 1f));
            var red = ShadeMaterial(new Color(0.75f, 0.12f, 0.1f, 1f));
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            body.transform.localScale = new Vector3(0.045f, 0.09f, 0.045f);
            Strip(body, mat);
            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.transform.SetParent(root.transform, false);
            band.transform.localPosition = new Vector3(0f, 0.01f, 0.02f);
            band.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            band.transform.localScale = new Vector3(0.05f, 0.012f, 0.05f);
            Strip(band, red);
            var mic = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mic.transform.SetParent(root.transform, false);
            mic.transform.localPosition = new Vector3(0f, 0.02f, 0.1f);
            mic.transform.localScale = Vector3.one * 0.045f;
            Strip(mic, mat);
            root.transform.SetParent(transform, false);
            root.SetActive(false);
            return root;
        }

        void DrawBeaconMarker(Camera cam, Transform beacon)
        {
            Vector3 sp = cam.WorldToScreenPoint(beacon.position);
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var gui = new Vector2(sp.x, Screen.height - sp.y);
            var delta = gui - center;
            if (sp.z < 0.05f) delta = -delta;
            bool onScreen = sp.z > 0.05f
                && gui.x > 90f && gui.y > 90f
                && gui.x < Screen.width - 90f && gui.y < Screen.height - 90f;
            if (onScreen) return;
            Vector2 n = delta.sqrMagnitude < 0.001f ? Vector2.up : delta.normalized;
            float a = Screen.width * 0.5f - 88f;
            float b = Screen.height * 0.5f - 88f;
            float hit = Mathf.Sqrt((n.x * n.x) / (a * a) + (n.y * n.y) / (b * b));
            Vector2 at = center + n / Mathf.Max(0.0001f, hit);
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            var tex = RingTexture();
            Color old = GUI.color;
            for (int i = 0; i < 3; i++)
            {
                float u = Mathf.Repeat(Time.unscaledTime * 0.45f + i / 3f, 1f);
                float size = Mathf.Lerp(16f, 78f, u);
                GUI.color = new Color(0.75f, 0.94f, 1f, (1f - u) * 0.85f * pulse);
                GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), tex, ScaleMode.StretchToFill, true);
            }
            GUI.color = new Color(0.9f, 0.97f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(at.x - 4f, at.y - 4f, 8f, 8f), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawRec()
        {
            float seconds = Record01 * RecordSeconds;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            float x = Screen.width * 0.5f - 70f;
            float y = Screen.height * 0.5f + 28f;
            GUI.color = new Color(0.9f, 0.15f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(x, y + 6f, 14f, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 22f, y, 180f, 28f), "REC  " + seconds.ToString("0.0") + " / 3", style);
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(x, y + 34f, 160f, 3f), Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.2f, 0.15f, 0.95f);
            GUI.DrawTexture(new Rect(x, y + 34f, 160f * Record01, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void DrawCaptured()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.95f, 1f, Mathf.Clamp01(capturedFlash)) }
            };
            GUI.Label(new Rect(0f, Screen.height * 0.5f + 24f, Screen.width, 32f), "已录下", style);
        }

        Material ShadeMaterial(Color color)
        {
            if (!shade) shade = NewMaterial();
            var mat = new Material(shade);
            mat.SetColor("_Color", color);
            return mat;
        }

        Material SignalMaterial()
        {
            if (!signal)
            {
                signal = NewMaterial();
                signal.SetColor("_Color", Color.white);
            }
            return signal;
        }

        static Material NewMaterial()
        {
            var shader = Shader.Find("D22/Signal");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(shader);
        }

        Texture2D RingTexture()
        {
            if (ringTex) return ringTex;
            const int n = 64;
            ringTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            ringTex.wrapMode = TextureWrapMode.Clamp;
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.72f) * 8f);
                    ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            ringTex.Apply();
            return ringTex;
        }

        void ApplySight()
        {
            EnsureGrade();
            bool here = Active && InField;
            shadowGrade.weight = here ? 1f : 0f;
            if (figures) figures.SetActive(here);
            foreach (var source in signals)
            {
                bool shown = source.body && (here || source.recorded);
                bool waves = here && !source.recorded;
                if (source.body) source.body.SetActive(shown);
                if (source.beacon) source.beacon.gameObject.SetActive(waves);
                SyncSourceVoice(source, waves);
            }
            flow?.SetShadowMusic(here);
        }

        void SyncSourceVoice(Signal source, bool waves)
        {
            if (source.voice == null || flow == null) return;
            AudioClip clip = source.clip ? source.clip : source.id == "drink" ? flow.beerClip : null;
            bool play = waves && clip && !D22GameFlow.InputBlocked;
            if (!play)
            {
                if (source.voice.isPlaying) source.voice.Stop();
                return;
            }
            if (source.voice.clip != clip) source.voice.clip = clip;
            source.voice.maxDistance = source.hearRange;
            if (!source.voice.isPlaying) source.voice.Play();
        }

        void EnsureGrade()
        {
            if (shadowGrade) return;
            var go = new GameObject("Shadow Grade");
            go.transform.SetParent(transform, false);
            shadowGrade = go.AddComponent<Volume>();
            shadowGrade.isGlobal = true;
            shadowGrade.priority = 80;
            shadowProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            shadowGrade.sharedProfile = shadowProfile;
            var color = shadowProfile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-1.05f);
            color.contrast.Override(-10f);
            color.saturation.Override(-40f);
            color.colorFilter.Override(new Color(0.74f, 0.78f, 0.84f));
            var vignette = shadowProfile.Add<Vignette>(true);
            vignette.intensity.Override(0.42f);
            vignette.smoothness.Override(0.48f);
            vignette.color.Override(new Color(0.02f, 0.025f, 0.04f));
        }

        void OnDestroy()
        {
            if (ringTex) Destroy(ringTex);
            if (shadowProfile) Destroy(shadowProfile);
        }
    }
}
