using System.Collections.Generic;
using UnityEngine;

namespace D22
{
    // Four collected sounds, each on an eight-step loop. Level, drive, room
    // and tone are the mix. The room's bed is silent while the desk is open.
    // The same loop can close the night: one lap intact, then one voice drops out of each step.
    public sealed class D22LoftMixer : MonoBehaviour
    {
        public const int Tracks = 4;
        public const int Steps = 8;
        public const int PerformLoops = 8;
        const float StepSeconds = 0.5f;

        public bool Open { get; private set; }
        public int Count => library.Count;
        public int Playhead => playhead < 0 ? 0 : playhead;
        public bool Sequencing => playhead >= 0;

        sealed class Piece
        {
            public string id;
            public string label;
            public AudioClip clip;
        }

        sealed class Lane
        {
            public string id;
            public bool mute;
            public float level = 0.82f;
            public float drive = 0.12f;
            public float room = 0.28f;
            public float tone = 0.75f;
            public readonly bool[] steps = new bool[Steps];
            public AudioSource source;
            public AudioDistortionFilter driveFx;
            public AudioReverbFilter roomFx;
            public AudioLowPassFilter toneFx;
            public Transform cap;
            public bool owed;
            public bool booth;
            public float heardDrive = -1f;
            public float heardRoom = -1f;
            public float heardTone = -1f;
        }

        sealed class Score
        {
            public string id;
            public bool mute;
            public float level = 0.82f;
            public float drive = 0.12f;
            public float room = 0.28f;
            public float tone = 0.75f;
            public readonly bool[] steps = new bool[Steps];
        }

        readonly struct Wanted
        {
            public readonly string id, label;
            public readonly AudioClip clip;
            public Wanted(string id, string label, AudioClip clip)
            {
                this.id = id;
                this.label = label;
                this.clip = clip;
            }
        }

        D22GameFlow flow;
        readonly List<Piece> library = new();
        readonly Dictionary<EntityId, AudioClip> beats = new();
        Lane[] lanes;
        Lane[] song;
        Score[] score;
        GameObject root;
        Material dark, panel, cream;
        float clock;
        int playhead = -1;
        float songClock;
        int songHead = -1;
        int songLoops;
        int listenLeft;
        float songFlash;
        bool finaleBed;
        bool dropHits;
        bool conducting;
        int hole = -1;
        readonly bool[] songHit = new bool[Tracks];

        public bool HasScore
        {
            get
            {
                if (score == null) return false;
                for (int i = 0; i < score.Length; i++)
                {
                    var lane = score[i];
                    if (lane == null || lane.mute || string.IsNullOrEmpty(lane.id)) continue;
                    for (int s = 0; s < Steps; s++) if (lane.steps[s]) return true;
                }
                return false;
            }
        }

        public bool BedOn => finaleBed;
        public bool Conducting => conducting;
        public bool Holes => conducting && dropHits;
        public int SongStep => songHead < 0 ? 0 : songHead;
        public float SongPulse => finaleBed ? 1f - Mathf.Clamp01(songClock / StepSeconds) : 0f;
        public float SongFlash => songFlash;

        public void Bind(D22GameFlow host) => flow = host;

        public void ResetLevels()
        {
            StopBed();
            score = null;
            clock = 0f;
            playhead = -1;
            if (lanes == null) return;
            for (int i = 0; i < Tracks; i++) Clear(i);
        }

        public string SongId(int track)
        {
            if (song == null || track < 0 || track >= Tracks) return null;
            return song[track].id;
        }

        public bool Due(int track)
        {
            if (!Holes || song == null || track < 0 || track >= Tracks || track != hole) return false;
            return OnStep(song[track]) && !songHit[track];
        }

        public bool ChartStep(int step)
        {
            if (song == null || step < 0 || step >= Steps) return false;
            for (int i = 0; i < Tracks; i++)
            {
                var lane = song[i];
                if (lane.mute || string.IsNullOrEmpty(lane.id) || !lane.steps[step]) continue;
                if (Find(lane.id) != null) return true;
            }
            return false;
        }

        public void StartBed()
        {
            if (!HasScore || lanes != null) return;
            MountSong();
            conducting = false;
            dropHits = false;
            listenLeft = 0;
            songLoops = 0;
            songClock = 0f;
            songHead = 0;
            songFlash = 0f;
            StrikeStep();
        }

        public void StartPerformance(int loops)
        {
            if (!HasScore || lanes != null) return;
            MountSong();
            conducting = true;
            songClock = 0f;
            songHead = 0;
            songFlash = 0f;
            int total = Mathf.Max(1, loops);
            if (total == 1)
            {
                listenLeft = 0;
                dropHits = true;
                songLoops = 1;
                StrikeExceptHole();
                return;
            }
            listenLeft = 1;
            dropHits = false;
            songLoops = total - 1;
            StrikeStep();
        }

        public void StopBed()
        {
            ReleaseBed();
            if (song == null) return;
            for (int i = 0; i < Tracks; i++)
            {
                songHit[i] = false;
                song[i].owed = false;
                if (song[i].source == null) continue;
                song[i].source.Stop();
                song[i].source.clip = null;
            }
        }

        void ReleaseBed()
        {
            finaleBed = false;
            conducting = false;
            dropHits = false;
            listenLeft = 0;
            songHead = -1;
            songLoops = 0;
            songClock = 0f;
            hole = -1;
        }

        public bool TickSong(float dt, string[] pressed, int pressedCount)
        {
            if (!finaleBed || song == null) return false;
            for (int i = 0; i < library.Count; i++) EnsureBeat(library[i]);
            for (int i = 0; i < Tracks; i++)
            {
                ApplyLane(song[i]);
                if (i != hole && song[i].owed && OnStep(song[i]) && Hit(song[i])) song[i].owed = false;
            }
            songFlash = Mathf.MoveTowards(songFlash, 0f, dt * 2.4f);
            songClock += dt;
            while (songClock >= StepSeconds)
            {
                songClock -= StepSeconds;
                int next = songHead < 0 ? 0 : (songHead + 1) % Steps;
                if (songHead >= 0 && next == 0)
                {
                    if (listenLeft > 0)
                    {
                        listenLeft--;
                        dropHits = listenLeft == 0;
                    }
                    else if (conducting)
                    {
                        songLoops--;
                        if (songLoops <= 0)
                        {
                            ReleaseBed();
                            return true;
                        }
                    }
                }
                songHead = next;
                if (dropHits) StrikeExceptHole();
                else StrikeStep();
            }
            if (dropHits && pressedCount > 0) Take(pressed, pressedCount);
            return false;
        }

        public string Label(int index) => library[index].label;
        public string IdAt(int index) => library[index].id;

        public string TrackText(int track)
        {
            var piece = PieceOn(track);
            return piece == null ? "空" : piece.label;
        }

        public bool TrackOn(int track) => PieceOn(track) != null;

        public bool Seated(string id)
        {
            if (lanes == null || string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < Tracks; i++) if (lanes[i].id == id) return true;
            return false;
        }

        public bool StepOn(int track, int step) => LaneAt(track) != null && LaneAt(track).steps[step];
        public bool Muted(int track) => LaneAt(track) != null && LaneAt(track).mute;
        public float Level(int track) => LaneAt(track) != null ? LaneAt(track).level : 0f;
        public float Drive(int track) => LaneAt(track) != null ? LaneAt(track).drive : 0f;
        public float Room(int track) => LaneAt(track) != null ? LaneAt(track).room : 0f;
        public float Tone(int track) => LaneAt(track) != null ? LaneAt(track).tone : 0f;

        public void SetLevel(int track, float value) { var lane = LaneAt(track); if (lane != null) lane.level = Mathf.Clamp01(value); }
        public void SetDrive(int track, float value) { var lane = LaneAt(track); if (lane != null) lane.drive = Mathf.Clamp01(value); }
        public void SetRoom(int track, float value) { var lane = LaneAt(track); if (lane != null) lane.room = Mathf.Clamp01(value); }
        public void SetTone(int track, float value) { var lane = LaneAt(track); if (lane != null) lane.tone = Mathf.Clamp01(value); }

        public void ToggleStep(int track, int step)
        {
            var lane = LaneAt(track);
            if (lane == null || string.IsNullOrEmpty(lane.id) || step < 0 || step >= Steps) return;
            lane.steps[step] = !lane.steps[step];
        }

        public void ToggleMute(int track)
        {
            var lane = LaneAt(track);
            if (lane == null || string.IsNullOrEmpty(lane.id)) return;
            lane.mute = !lane.mute;
        }

        public void Place(int track, string id)
        {
            if (lanes == null || track < 0 || track >= Tracks) return;
            var piece = Find(id);
            if (piece == null || piece.clip == null) return;
            if (lanes[track].id == id)
            {
                Clear(track);
                return;
            }
            for (int i = 0; i < Tracks; i++)
                if (i != track && lanes[i].id == id) Clear(i);
            var lane = lanes[track];
            lane.id = id;
            lane.mute = false;
            lane.level = 0.82f;
            lane.drive = 0.12f;
            lane.room = 0.28f;
            lane.tone = 0.75f;
            for (int s = 0; s < Steps; s++) lane.steps[s] = s == 0 || s == 4;
            if (playhead < 0)
            {
                playhead = 0;
                clock = 0f;
                for (int i = 0; i < Tracks; i++)
                    if (lanes[i].steps[0] && !Hit(lanes[i])) lanes[i].owed = true;
            }
            else if (lane.steps[playhead] && !Hit(lane)) lane.owed = true;
        }

        public void Clear(int track)
        {
            var lane = LaneAt(track);
            if (lane == null) return;
            lane.id = null;
            lane.mute = false;
            lane.owed = false;
            for (int s = 0; s < Steps; s++) lane.steps[s] = false;
            if (lane.source && lane.source.isPlaying) lane.source.Stop();
        }

        public bool Near(Camera cam)
        {
            if (!root || Open || cam == null) return false;
            Vector3 aim = root.transform.TransformPoint(new Vector3(0f, 0.9f, -0.12f));
            Vector3 to = aim - cam.transform.position;
            float distance = to.magnitude;
            if (distance < 0.3f || distance > 2.6f) return false;
            float facing = Vector3.Dot(cam.transform.forward, to / distance);
            return facing > (distance < 1.5f ? 0.2f : 0.55f);
        }

        public void Show()
        {
            if (root) return;
            Vector3 floor = new Vector3(-1.15f, 2.66f, 5.7f);
            var probe = new Vector3(floor.x, 4.3f, floor.z);
            if (Physics.Raycast(probe, Vector3.down, out var hit, 2.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.normal.y > 0.65f && hit.point.y > 2.3f && hit.point.y < 3.1f)
                floor = hit.point;

            root = new GameObject("Loft Mixer");
            root.transform.SetPositionAndRotation(floor, Quaternion.identity);
            dark = MakeMat(new Color(0.07f, 0.065f, 0.06f));
            panel = MakeMat(new Color(0.16f, 0.15f, 0.14f));
            cream = MakeMat(new Color(0.86f, 0.78f, 0.62f));

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Desk";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            body.transform.localScale = new Vector3(1.72f, 0.84f, 0.62f);
            body.GetComponent<Renderer>().sharedMaterial = dark;

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Panel";
            Destroy(top.GetComponent<Collider>());
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.86f, -0.02f);
            top.transform.localScale = new Vector3(1.52f, 0.035f, 0.46f);
            top.GetComponent<Renderer>().sharedMaterial = panel;

            AddBox(new Vector3(-0.72f, 0.98f, 0.16f), new Vector3(0.16f, 0.22f, 0.12f), dark);
            AddBox(new Vector3(0.72f, 0.98f, 0.16f), new Vector3(0.16f, 0.22f, 0.12f), dark);
            var lamp = new GameObject("Desk Light").AddComponent<Light>();
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 1.15f, -0.2f);
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.82f, 0.62f);
            lamp.intensity = 1.4f;
            lamp.range = 3.6f;
            lamp.shadows = LightShadows.None;
            EnsureLanes();
            Refresh();
            RestoreScore();
        }

        public void Hide()
        {
            Capture();
            StopBed();
            bool wasOpen = Open;
            Open = false;
            if (wasOpen) flow?.SetMixerBed(true);
            if (lanes != null)
                for (int i = 0; i < Tracks; i++)
                    if (lanes[i].source) lanes[i].source.Stop();
            library.Clear();
            foreach (var beat in beats.Values) if (beat) Destroy(beat);
            beats.Clear();
            lanes = null;
            clock = 0f;
            playhead = -1;
            if (dark) Destroy(dark);
            if (panel) Destroy(panel);
            if (cream) Destroy(cream);
            dark = panel = cream = null;
            if (root) Destroy(root);
            root = null;
        }

        public void OpenPanel()
        {
            if (!root || flow == null || flow.UI == null) return;
            Refresh();
            Open = true;
            flow.SetMixerBed(false);
            D22Look.Unlock();
            flow.UI.ShowMixer();
        }

        public void ClosePanel()
        {
            Open = false;
            flow?.SetMixerBed(true);
            if (flow != null && !flow.Loading && flow.UI != null) flow.UI.ShowHUD();
        }

        void Update()
        {
            if (lanes == null) return;
            for (int i = 0; i < library.Count; i++) EnsureBeat(library[i]);
            bool any = false;
            for (int i = 0; i < Tracks; i++)
            {
                ApplyLane(lanes[i]);
                if (Find(lanes[i].id) != null) any = true;
                else if (lanes[i].owed) lanes[i].owed = false;
            }
            if (!any)
            {
                clock = 0f;
                playhead = -1;
                return;
            }
            for (int i = 0; i < Tracks; i++)
                if (lanes[i].owed && Hit(lanes[i])) lanes[i].owed = false;
            clock += Time.unscaledDeltaTime;
            while (clock >= StepSeconds)
            {
                clock -= StepSeconds;
                playhead = playhead < 0 ? 0 : (playhead + 1) % Steps;
                for (int i = 0; i < Tracks; i++)
                {
                    if (lanes[i].steps[playhead])
                    {
                        if (!Hit(lanes[i])) lanes[i].owed = true;
                    }
                    else
                    {
                        lanes[i].owed = false;
                        if (lanes[i].source && lanes[i].source.isPlaying) lanes[i].source.Stop();
                    }
                }
            }
        }

        void EnsureLanes()
        {
            lanes = new Lane[Tracks];
            var holder = new GameObject("Tracks").transform;
            holder.SetParent(root.transform, false);
            float span = 1.2f;
            for (int i = 0; i < Tracks; i++)
            {
                var lane = new Lane();
                float x = Mathf.Lerp(-span * 0.5f, span * 0.5f, i / (Tracks - 1f));
                var slot = new GameObject("Track " + (i + 1));
                slot.transform.SetParent(holder, false);
                slot.transform.localPosition = new Vector3(x, 0.88f, -0.04f);
                var sourceGo = new GameObject("Voice");
                sourceGo.transform.SetParent(root.transform, false);
                var source = sourceGo.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                source.minDistance = 0.7f;
                source.maxDistance = 3.4f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                lane.source = source;
                lane.driveFx = sourceGo.AddComponent<AudioDistortionFilter>();
                lane.driveFx.enabled = false;
                lane.roomFx = sourceGo.AddComponent<AudioReverbFilter>();
                lane.roomFx.reverbPreset = AudioReverbPreset.User;
                lane.roomFx.enabled = false;
                lane.toneFx = sourceGo.AddComponent<AudioLowPassFilter>();
                lane.toneFx.enabled = false;
                var well = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(well.GetComponent<Collider>());
                well.transform.SetParent(slot.transform, false);
                well.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                well.transform.localScale = new Vector3(0.025f, 0.2f, 0.02f);
                well.GetComponent<Renderer>().sharedMaterial = dark;
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cap.GetComponent<Collider>());
                cap.transform.SetParent(slot.transform, false);
                cap.transform.localScale = new Vector3(0.07f, 0.028f, 0.045f);
                cap.GetComponent<Renderer>().sharedMaterial = cream;
                lane.cap = cap.transform;
                lanes[i] = lane;
            }
        }

        void Refresh()
        {
            if (!root || flow == null) return;
            if (lanes == null) EnsureLanes();
            FillLibrary();
            for (int i = 0; i < Tracks; i++)
                if (!string.IsNullOrEmpty(lanes[i].id) && Find(lanes[i].id) == null) Clear(i);
        }

        void FillLibrary()
        {
            if (flow == null) return;
            var wanted = Collect();
            library.Clear();
            foreach (var item in wanted)
            {
                if (item.clip && item.clip.loadState == AudioDataLoadState.Unloaded) item.clip.LoadAudioData();
                library.Add(new Piece { id = item.id, label = item.label, clip = item.clip });
            }
        }

        void Capture()
        {
            if (lanes == null) return;
            if (score == null)
            {
                score = new Score[Tracks];
                for (int i = 0; i < Tracks; i++) score[i] = new Score();
            }
            for (int i = 0; i < Tracks; i++)
            {
                var lane = lanes[i];
                var kept = score[i];
                kept.id = lane.id;
                kept.mute = lane.mute;
                kept.level = lane.level;
                kept.drive = lane.drive;
                kept.room = lane.room;
                kept.tone = lane.tone;
                for (int s = 0; s < Steps; s++) kept.steps[s] = lane.steps[s];
            }
        }

        void RestoreScore()
        {
            if (score == null || lanes == null) return;
            bool any = false;
            for (int i = 0; i < Tracks; i++)
            {
                var kept = score[i];
                var lane = lanes[i];
                if (kept == null || string.IsNullOrEmpty(kept.id) || Find(kept.id) == null)
                {
                    Clear(i);
                    continue;
                }
                lane.id = kept.id;
                lane.mute = kept.mute;
                lane.level = kept.level;
                lane.drive = lane.drive;
                lane.room = kept.room;
                lane.tone = kept.tone;
                lane.owed = false;
                lane.heardDrive = lane.heardRoom = lane.heardTone = -1f;
                for (int s = 0; s < Steps; s++) lane.steps[s] = kept.steps[s];
                if (!lane.mute && lane.steps[0]) lane.owed = true;
                if (!lane.mute)
                    for (int s = 0; s < Steps; s++) if (lane.steps[s]) any = true;
            }
            playhead = any ? 0 : -1;
            clock = 0f;
        }

        void MountSong()
        {
            FillLibrary();
            EnsureSong();
            for (int i = 0; i < Tracks; i++)
            {
                var lane = song[i];
                var kept = score != null && i < score.Length ? score[i] : null;
                lane.owed = false;
                lane.heardDrive = lane.heardRoom = lane.heardTone = -1f;
                songHit[i] = false;
                if (kept == null || string.IsNullOrEmpty(kept.id) || Find(kept.id) == null)
                {
                    lane.id = null;
                    lane.mute = false;
                    for (int s = 0; s < Steps; s++) lane.steps[s] = false;
                    continue;
                }
                lane.id = kept.id;
                lane.mute = kept.mute;
                lane.level = kept.level;
                lane.drive = kept.drive;
                lane.room = kept.room;
                lane.tone = kept.tone;
                for (int s = 0; s < Steps; s++) lane.steps[s] = kept.steps[s];
            }
            finaleBed = true;
            for (int i = 0; i < library.Count; i++) EnsureBeat(library[i]);
        }

        void EnsureSong()
        {
            if (song != null) return;
            song = new Lane[Tracks];
            for (int i = 0; i < Tracks; i++)
            {
                var lane = new Lane { booth = true };
                var sourceGo = new GameObject("Finale " + (i + 1));
                sourceGo.transform.SetParent(transform, false);
                var source = sourceGo.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                lane.source = source;
                lane.driveFx = sourceGo.AddComponent<AudioDistortionFilter>();
                lane.driveFx.enabled = false;
                lane.roomFx = sourceGo.AddComponent<AudioReverbFilter>();
                lane.roomFx.reverbPreset = AudioReverbPreset.User;
                lane.roomFx.enabled = false;
                lane.toneFx = sourceGo.AddComponent<AudioLowPassFilter>();
                lane.toneFx.enabled = false;
                song[i] = lane;
            }
        }

        bool OnStep(Lane lane)
        {
            if (lane == null || songHead < 0 || songHead >= Steps) return false;
            if (string.IsNullOrEmpty(lane.id) || lane.mute || !lane.steps[songHead]) return false;
            return Find(lane.id) != null;
        }

        void StrikeStep()
        {
            hole = -1;
            for (int i = 0; i < Tracks; i++) PlayOrStop(i, true);
        }

        void StrikeExceptHole()
        {
            hole = PickHole();
            for (int i = 0; i < Tracks; i++) PlayOrStop(i, i != hole);
        }

        int PickHole()
        {
            int count = 0;
            for (int i = 0; i < Tracks; i++) if (OnStep(song[i])) count++;
            if (count == 0) return -1;
            int pick = songHead % count;
            for (int i = 0; i < Tracks; i++)
            {
                if (!OnStep(song[i])) continue;
                if (pick == 0) return i;
                pick--;
            }
            return -1;
        }

        void PlayOrStop(int track, bool play)
        {
            var lane = song[track];
            songHit[track] = false;
            if (!play || !OnStep(lane))
            {
                lane.owed = false;
                if (lane.source && lane.source.isPlaying) lane.source.Stop();
                return;
            }
            if (!Hit(lane)) lane.owed = true;
        }

        void Take(string[] ids, int count)
        {
            if (hole < 0 || songHit[hole] || !OnStep(song[hole])) return;
            string id = song[hole].id;
            for (int n = 0; n < count; n++)
            {
                if (ids[n] != id) continue;
                if (!Hit(song[hole])) return;
                songHit[hole] = true;
                songFlash = 1f;
                return;
            }
        }

        List<Wanted> Collect()
        {
            var list = new List<Wanted>();
            if (flow.HasDrink && flow.beerClip) list.Add(new Wanted("drink", "酒", flow.beerClip));
            AddLearned(list, "cricket", "蛐蛐", flow.cricketClip);
            AddLearned(list, "pigeon", "鸽哨", flow.pigeonClip);
            AddLearned(list, "scissors", "磨刀", flow.grindClip);
            AddLearned(list, "guitar", "吉他", flow.Abilities != null ? flow.Abilities.FieldLoop("guitar") : null);
            AddLearned(list, "bass", "贝斯", flow.Abilities != null ? flow.Abilities.FieldLoop("bass") : null);
            AddLearned(list, "drums", "鼓", flow.Abilities != null ? flow.Abilities.FieldLoop("drums") : null);
            return list;
        }

        void AddLearned(List<Wanted> list, string id, string label, AudioClip clip)
        {
            if (clip && flow.Abilities != null && flow.Abilities.Learned(id)) list.Add(new Wanted(id, label, clip));
        }

        Piece Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < library.Count; i++)
                if (library[i].id == id) return library[i];
            return null;
        }

        Piece PieceOn(int track) => Find(LaneAt(track) != null ? LaneAt(track).id : null);

        Lane LaneAt(int track)
        {
            if (lanes == null || track < 0 || track >= Tracks) return null;
            return lanes[track];
        }

        bool Hit(Lane lane)
        {
            var piece = Find(lane.id);
            if (piece == null || lane.mute || lane.source == null) return false;
            EnsureBeat(piece);
            var beat = piece.clip ? (beats.TryGetValue(piece.clip.GetEntityId(), out var baked) ? baked : null) : null;
            if (!beat) return false;
            lane.owed = false;
            ApplyLane(lane);
            lane.source.Stop();
            lane.source.clip = beat;
            lane.source.loop = false;
            lane.source.time = 0f;
            lane.source.Play();
            return true;
        }

        void EnsureBeat(Piece piece)
        {
            if (piece == null || !piece.clip || beats.ContainsKey(piece.clip.GetEntityId())) return;
            var clip = piece.clip;
            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
                return;
            }
            if (clip.loadState != AudioDataLoadState.Loaded) return;
            beats[clip.GetEntityId()] = Bake(clip);
        }

        AudioClip Bake(AudioClip clip)
        {
            int channels = Mathf.Max(1, clip.channels);
            int rate = clip.frequency > 0 ? clip.frequency : 44100;
            int frames = clip.samples;
            if (frames <= 0) return null;
            var data = new float[frames * channels];
            if (!clip.GetData(data, 0)) return null;
            int take = Mathf.Min(frames, Mathf.CeilToInt(StepSeconds * rate));
            int onset = 0;
            if (frames > take)
            {
                int hop = Mathf.Max(1, rate / 40);
                int hops = frames / hop;
                var energy = new float[hops];
                for (int h = 0; h < hops; h++)
                {
                    int start = h * hop;
                    int count = Mathf.Min(hop, frames - start);
                    double sum = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float sample = 0f;
                        int at = (start + i) * channels;
                        for (int c = 0; c < channels; c++) sample += data[at + c];
                        sample /= channels;
                        sum += sample * sample;
                    }
                    energy[h] = (float)(sum / count);
                }
                int span = Mathf.Max(1, take / hop);
                float best = -1f;
                int bestHop = 0;
                float run = 0f;
                for (int h = 0; h < hops; h++)
                {
                    run += energy[h];
                    if (h >= span) run -= energy[h - span];
                    if (h >= span - 1 && run > best)
                    {
                        best = run;
                        bestHop = h - (span - 1);
                    }
                }
                onset = Mathf.Clamp(bestHop * hop, 0, frames - take);
            }
            var slice = new float[take * channels];
            System.Array.Copy(data, onset * channels, slice, 0, slice.Length);
            int fade = Mathf.Min(take / 3, Mathf.CeilToInt(rate * 0.02f));
            for (int i = 0; i < fade; i++)
            {
                float gain = (fade - i) / (float)fade;
                int at = (take - 1 - i) * channels;
                for (int c = 0; c < channels; c++) slice[at + c] *= gain;
            }
            var beat = AudioClip.Create(clip.name + " beat", take, channels, rate, false);
            beat.SetData(slice, 0);
            return beat;
        }

        void ApplyLane(Lane lane)
        {
            if (lane == null || lane.source == null) return;
            var piece = Find(lane.id);
            bool on = piece != null && piece.clip;
            if (lane.cap) lane.cap.localPosition = new Vector3(0f, on ? Mathf.Lerp(0.02f, 0.2f, lane.level) : 0.02f, 0f);
            bool monitor = lane.booth || Open;
            lane.source.spatialBlend = monitor ? 0f : 1f;
            float master = flow != null ? flow.Volume : 0.35f;
            lane.source.volume = on && !lane.mute ? lane.level * Mathf.Clamp(master * (monitor ? 2.4f : 2.2f), 0f, 1f) : 0f;
            if (lane.driveFx)
            {
                bool use = on && lane.drive > 0.02f;
                if (lane.driveFx.enabled != use || !Mathf.Approximately(lane.heardDrive, lane.drive))
                {
                    lane.heardDrive = lane.drive;
                    lane.driveFx.enabled = use;
                    lane.driveFx.distortionLevel = lane.drive * 0.75f;
                }
            }
            if (lane.toneFx)
            {
                if (lane.toneFx.enabled != on || !Mathf.Approximately(lane.heardTone, lane.tone))
                {
                    lane.heardTone = lane.tone;
                    lane.toneFx.enabled = on;
                    lane.toneFx.cutoffFrequency = Mathf.Lerp(700f, 18000f, lane.tone);
                }
            }
            if (lane.roomFx)
            {
                bool use = on && lane.room > 0.03f;
                if (lane.roomFx.enabled != use || !Mathf.Approximately(lane.heardRoom, lane.room))
                {
                    lane.heardRoom = lane.room;
                    lane.roomFx.enabled = use;
                    lane.roomFx.dryLevel = 0f;
                    lane.roomFx.room = Mathf.Lerp(-2800f, -400f, lane.room);
                    lane.roomFx.decayTime = Mathf.Lerp(0.08f, 0.35f, lane.room);
                    lane.roomFx.reverbLevel = Mathf.Lerp(-3200f, -400f, lane.room);
                    lane.roomFx.reflectionsLevel = Mathf.Lerp(-3000f, -800f, lane.room);
                }
            }
        }

        void AddBox(Vector3 local, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = local;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Material MakeMat(Color color)
        {
            var shader = Shader.Find("D22/Signal");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
