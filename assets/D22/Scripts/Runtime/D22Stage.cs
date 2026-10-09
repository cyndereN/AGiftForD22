using System;
using System.Collections.Generic;
using UnityEngine;

namespace D22
{
    // Livehouse stage. Marks are placeholders, same as the hutong, until the three are played.
    public sealed class D22Stage
    {
        public const int NeedDrum = 4, NeedGuitar = 3, NeedBass = 4;
        public const int ShowNeedDrink = 3, ShowNeedGrind = 3, ShowNeedGuitar = 5, ShowNeedBass = 3, ShowNeedDrum = 7;
        public const float BassWalk = 2.4f;
        static readonly Vector3 ScanIntroSpot = new Vector3(-1.1f, 0f, -9f);
        static readonly Vector3 ScanStageSpot = new Vector3(-1.1f, 0f, -20f);
        static readonly Vector2[] Moles =
        {
            new Vector2(.22f, .58f),
            new Vector2(.5f, .4f),
            new Vector2(.78f, .58f)
        };

        readonly D22GameFlow flow;
        readonly List<D22WorldMark> marks = new();
        readonly bool[] swept = new bool[6];
        float strumX = -1;
        D22WorldMark drumMark, guitarMark, bassMark;
        Vector3 introSpot = ScanIntroSpot, stageSpot = ScanStageSpot;
        Vector3 drumSpot = new Vector3(-3.1f, .7f, -15.6f);
        Vector3 guitarSpot = new Vector3(-1.1f, .95f, -16.4f);
        Vector3 bassSpot = new Vector3(1.1f, .7f, -15.6f);
        float introRadius = 8, stageRadius = 7;
        float moleLife, moleGap;
        int mole = -1;
        bool heardDrum, heardGuitar, heardBass, teachDrum, teachGuitar, teachBass;

        public string Game { get; private set; }
        public bool Open => Game != null;
        public bool Spawned { get; private set; }
        public bool IntroPlayed { get; private set; }
        public bool OnStagePlayed { get; private set; }
        public bool Encore { get; private set; }
        public int ShowDrink { get; private set; }
        public int ShowGrind { get; private set; }
        public int ShowGuitar { get; private set; }
        public int ShowBass { get; private set; }
        public int ShowDrum { get; private set; }
        public bool ShowReady => Encore
            && ShowDrink >= ShowNeedDrink
            && ShowGrind >= ShowNeedGrind
            && ShowGuitar >= ShowNeedGuitar
            && ShowBass >= ShowNeedBass
            && ShowDrum >= ShowNeedDrum;
        public bool DoneDrum { get; private set; }
        public bool DoneGuitar { get; private set; }
        public bool DoneBass { get; private set; }
        public bool TrioDone => DoneDrum && DoneGuitar && DoneBass;
        public int DrumHits { get; private set; }
        public int GuitarSweeps { get; private set; }
        public int BassHolds { get; private set; }
        public float Walker { get; private set; }
        public bool BassHeld { get; private set; }
        public int Mole => mole;
        public float MoleUp => mole < 0 ? 0 : Mathf.Clamp01(moleLife / .28f);
        public IReadOnlyList<D22WorldMark> Marks => marks;

        public D22Stage(D22GameFlow host) { flow = host; }

        public void Bind(D22Bootstrap livehouse)
        {
            introSpot = livehouse ? livehouse.introSpot : ScanIntroSpot;
            introRadius = livehouse ? livehouse.introRadius : 8;
            stageSpot = livehouse ? livehouse.stageSpot : ScanStageSpot;
            stageRadius = livehouse ? livehouse.stageRadius : 7;
            drumSpot = livehouse ? livehouse.drumSpot : new Vector3(-3.1f, .7f, -15.6f);
            guitarSpot = livehouse ? livehouse.guitarSpot : new Vector3(-1.1f, .95f, -16.4f);
            bassSpot = livehouse ? livehouse.bassSpot : new Vector3(1.1f, .7f, -15.6f);
        }

        public void Clear()
        {
            Close();
            foreach (var m in marks) if (m) UnityEngine.Object.Destroy(m.gameObject);
            marks.Clear();
            drumMark = guitarMark = bassMark = null;
            Spawned = false;
            IntroPlayed = false;
            OnStagePlayed = false;
            Encore = false;
            ShowDrink = ShowGrind = ShowGuitar = ShowBass = ShowDrum = 0;
            DoneDrum = DoneGuitar = DoneBass = false;
            heardDrum = heardGuitar = heardBass = false;
            teachDrum = teachGuitar = teachBass = false;
            DrumHits = GuitarSweeps = BassHolds = 0;
            Walker = 0;
            mole = -1;
        }

        public void MarkIntro() => IntroPlayed = true;

        public void Spawn()
        {
            if (Spawned) return;
            Spawned = true;
            drumMark = Make(D22MarkKind.Drum, drumSpot, 1.35f);
            guitarMark = Make(D22MarkKind.Guitar, guitarSpot, 1.35f);
            bassMark = Make(D22MarkKind.Bass, bassSpot, 1.35f);
        }

        public bool NearIntro(Camera camera) => camera && Flat(camera.transform.position, introSpot) < introRadius;
        public bool NearStage(Camera camera) => camera && Flat(camera.transform.position, stageSpot) < stageRadius;

        public void MarkOnStage() => OnStagePlayed = true;
        public void BeginEncore() => Encore = true;

        public void NoteDrink() { if (Encore && ShowDrink < ShowNeedDrink) ShowDrink++; }
        public void NoteGrind() { if (Encore && ShowGrind < ShowNeedGrind) ShowGrind++; }

        public string ShowQuota() => $"{ShowDrink}/{ShowNeedDrink}酒 {ShowGrind}/{ShowNeedGrind}磨刀 {ShowGuitar}/{ShowNeedGuitar}吉他 {ShowBass}/{ShowNeedBass}贝斯 {ShowDrum}/{ShowNeedDrum}鼓";

        public string Hint(Camera camera)
        {
            if (!Spawned || camera == null) return null;
            var aim = Aim(camera);
            if (Encore && !ShowReady) return ShowQuota();
            if (aim == D22MarkKind.Drum) return DoneDrum ? "鼓" : "E 鼓";
            if (aim == D22MarkKind.Guitar) return DoneGuitar ? "吉他" : "E 吉他";
            if (aim == D22MarkKind.Bass) return DoneBass ? "贝斯" : "E 贝斯";
            if (TrioDone && !OnStagePlayed) return "上台";
            return null;
        }

        public bool Interact(Camera camera, Action<string, D22Line[], Action> talk, D22Story story)
        {
            if (!Spawned || Open) return false;
            var aim = Aim(camera);
            if (aim == D22MarkKind.Drum)
            {
                OpenTalk("鼓", ref heardDrum, story.drumTalk, "drums", talk);
                return true;
            }
            if (aim == D22MarkKind.Guitar)
            {
                OpenTalk("吉他", ref heardGuitar, story.guitarTalk, "guitar", talk);
                return true;
            }
            if (aim == D22MarkKind.Bass)
            {
                OpenTalk("贝斯", ref heardBass, story.bassTalk, "bass", talk);
                return true;
            }
            return false;
        }

        public bool TryOpen(string id)
        {
            if (Open || (id != "drums" && id != "guitar" && id != "bass")) return false;
            Game = id;
            Walker = 0;
            BassHeld = false;
            strumX = -1;
            mole = -1;
            moleGap = .35f;
            for (int i = 0; i < swept.Length; i++) swept[i] = false;
            return true;
        }

        public void Close()
        {
            Game = null;
            BassHeld = false;
            mole = -1;
        }

        public void Tick()
        {
            if (Game == "drums") TickMoles();
            else if (Game == "bass") TickBass();
        }

        public void Pointer(Vector2 n, bool down, bool drag, bool up)
        {
            if (Game == "drums" && down) Hit(n);
            else if (Game == "guitar" && (down || drag)) Strum(n, down);
            else if (Game == "guitar" && up)
            {
                for (int i = 0; i < swept.Length; i++) swept[i] = false;
                strumX = -1;
            }
            else if (Game == "bass")
            {
                if (up) BassHeld = false;
                else if (down || drag) BassHeld = n.y > .38f && n.y < .62f;
            }
        }

        public Vector2 MolePos(int i) => Moles[Mathf.Clamp(i, 0, Moles.Length - 1)];

        void OpenTalk(string title, ref bool heard, D22Line[] lines, string id, Action<string, D22Line[], Action> talk)
        {
            if (!heard)
            {
                heard = true;
                if (flow.Abilities != null && flow.Abilities.Learn(id))
                {
                    flow.UI.PlayLearnFly(id);
                    if (id == "drums") teachDrum = true;
                    else if (id == "guitar") teachGuitar = true;
                    else if (id == "bass") teachBass = true;
                }
                if (lines != null && lines.Length > 0)
                {
                    talk(title, lines, () => flow.ResumeStage(id));
                    return;
                }
            }
            flow.ResumeStage(id);
        }

        D22MarkKind? Aim(Camera camera)
        {
            if (!Spawned || camera == null) return null;
            var pos = camera.transform.position;
            var fwd = camera.transform.forward;
            D22WorldMark best = null;
            float bestDot = .82f;
            foreach (var mark in marks)
            {
                if (!mark) continue;
                Vector3 to = mark.transform.position - pos;
                if (to.magnitude > mark.radius + 1.2f) continue;
                float dot = Vector3.Dot(fwd, to.normalized);
                if (dot > bestDot) { bestDot = dot; best = mark; }
            }
            return best ? best.kind : (D22MarkKind?)null;
        }

        void TickMoles()
        {
            float dt = Time.unscaledDeltaTime;
            if (mole >= 0)
            {
                moleLife -= dt;
                if (moleLife <= 0) { mole = -1; moleGap = .2f; }
                return;
            }
            moleGap -= dt;
            if (moleGap > 0) return;
            mole = UnityEngine.Random.Range(0, Moles.Length);
            moleLife = 1.15f;
        }

        void Hit(Vector2 n)
        {
            if (mole < 0) return;
            if (Vector2.Distance(n, Moles[mole]) > .12f) return;
            mole = -1;
            moleGap = .18f;
            DrumHits++;
            flow.Abilities?.PlayDrum(UnityEngine.Random.Range(0, 3));
            if (teachDrum) { teachDrum = false; EndLesson(); return; }
            if (!DoneDrum && DrumHits >= NeedDrum) Finish("drums");
            else if (Encore && ShowDrum < ShowNeedDrum) ShowDrum++;
        }

        void Strum(Vector2 n, bool fresh)
        {
            if (fresh)
            {
                for (int i = 0; i < swept.Length; i++) swept[i] = false;
                strumX = n.x;
            }
            float from = strumX < 0 ? n.x : strumX;
            strumX = n.x;
            for (int i = 0; i < 6; i++)
            {
                float x = (i + 1) / 7f;
                if (from < x == n.x < x && Mathf.Abs(n.x - x) > .04f) continue;
                if (swept[i]) continue;
                swept[i] = true;
                flow.Abilities?.PlayGuitar(i);
            }
            for (int i = 0; i < swept.Length; i++) if (!swept[i]) return;
            for (int i = 0; i < swept.Length; i++) swept[i] = false;
            GuitarSweeps++;
            if (teachGuitar) { teachGuitar = false; EndLesson(); return; }
            if (!DoneGuitar && GuitarSweeps >= NeedGuitar) Finish("guitar");
            else if (Encore && ShowGuitar < ShowNeedGuitar) ShowGuitar++;
        }

        void TickBass()
        {
            if (!BassHeld) { Walker = 0; return; }
            Walker += Time.unscaledDeltaTime / BassWalk;
            int fret = Mathf.Clamp(Mathf.FloorToInt(Walker * 4), 0, 3);
            if (Walker < 1f) return;
            Walker = 0;
            BassHolds++;
            flow.Abilities?.PlayBass(fret);
            if (teachBass) { teachBass = false; EndLesson(); return; }
            if (!DoneBass && BassHolds >= NeedBass) Finish("bass");
            else if (Encore && ShowBass < ShowNeedBass) ShowBass++;
        }

        void EndLesson()
        {
            Close();
            if (!flow.Loading) flow.UI.ShowHUD();
        }

        void Finish(string id)
        {
            if (id == "drums") DoneDrum = true;
            else if (id == "guitar") DoneGuitar = true;
            else if (id == "bass") DoneBass = true;
            if (flow.Abilities != null && flow.Abilities.Learn(id))
                flow.UI.PlayLearnFly(id);
            Close();
            if (!flow.Loading) flow.UI.ShowHUD();
        }

        D22WorldMark Make(D22MarkKind kind, Vector3 pos, float radius)
        {
            var go = new GameObject("Mark " + kind);
            go.transform.position = pos;
            var mark = go.AddComponent<D22WorldMark>();
            mark.kind = kind;
            mark.radius = radius;
            marks.Add(mark);
            return mark;
        }

        static float Flat(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0;
            return Vector3.Distance(a, b);
        }
    }
}
